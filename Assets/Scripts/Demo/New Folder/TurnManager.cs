using System.Collections;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class TurnManager : MonoBehaviour
{
    [Header("Actors")]
    public CombatActor player;
    public CombatActor enemy;              // demo: single enemy
    public BattlePartyManager2D party;     // preferred: multi enemy/ally roster
    public SkillExecutor executor;

    [Header("Dice Rig (IMPORTANT)")]
    public DiceSlotRig diceRig;

    [Header("Optional: lock planning UI before roll / after Continue")]
    public CanvasGroup skillBarGroup;
    public CanvasGroup slotsPanelGroup;

    [Tooltip("If TRUE, disables planning UI until dice have finished rolling.\n" +
             "If your Roll button lives inside these CanvasGroups, keep this FALSE or move the Roll button out.")]
    public bool lockPlanningUIUntilRolled = false;

    public enum Phase { Planning, AwaitTarget, Executing, EnemyTurn }
    public Phase phase = Phase.Planning;

    [Header("Debug")]
    public bool logPhase = true;
    public KeyCode toggleLogKey = KeyCode.F10;

    // Planning is allowed ONLY after dice rolled (and not rolling)
    public bool IsPlanning =>
        phase == Phase.Planning &&
        (diceRig == null || (diceRig.HasRolledThisTurn && !diceRig.IsRolling));

    private readonly ActionSlotDrop[] _drops = new ActionSlotDrop[3];
    private readonly SkillPlanBoard _board = new SkillPlanBoard();
    private int _cursor = 0;

    void Start()
    {
        _board.Reset();

        if (party != null)
        {
            party.EnsureSpawned();
            if (party.Player != null) player = party.Player;
        }

        if (diceRig != null)
        {
            diceRig.onAllDiceRolled += OnDiceRolled;
            diceRig.BeginNewTurn();
        }

        RefreshPlanningInteractivity();
        RefreshAllPreviews();
        UpdateAllIconsDim();
    }

    void OnDestroy()
    {
        if (diceRig != null)
            diceRig.onAllDiceRolled -= OnDiceRolled;
    }

    void Update()
    {
        // Press Space to roll ONCE during Planning
        if (phase == Phase.Planning && diceRig != null)
        {
            if (!diceRig.HasRolledThisTurn && !diceRig.IsRolling && SpacePressedThisFrame())
            {
                diceRig.RollOnce();
                RefreshPlanningInteractivity();
            }
        }

        if (Input.GetKeyDown(toggleLogKey))
        {
            logPhase = !logPhase;
            Debug.Log($"[TurnManager] logPhase = {logPhase}", this);
        }
    }

    // Optional UI button hook
    public void OnRollPressed()
    {
        if (phase != Phase.Planning) return;
        if (diceRig == null) return;
        if (diceRig.HasRolledThisTurn || diceRig.IsRolling) return;

        diceRig.RollOnce();
        RefreshPlanningInteractivity();
    }

    // Registration (ActionSlotDrop should call this)
    public void RegisterDrop(ActionSlotDrop drop)
    {
        if (!drop) return;
        int i = drop.slotIndex - 1;
        if (i < 0 || i > 2) return;

        _drops[i] = drop;
        RefreshAllPreviews();
    }

    // ---------------------------
    // Equip APIs (Legacy SkillSO)
    // ---------------------------
    public bool TryAssignSkillToSlot(int slotIndex1Based, SkillSO skill)
    {
        if (!IsPlanning) return false;
        if (player == null || skill == null) return false;

        int drop0 = slotIndex1Based - 1;
        if (drop0 < 0 || drop0 > 2) return false;

        int span = Mathf.Clamp(skill.slotsRequired, 1, 3);

        if (!_board.ResolvePlacementForDrop(drop0, span, out int start0, out int anchor0))
            return false;

        if (!AreSlotsActiveInRange(start0, span))
            return false;

        if (diceRig != null && !diceRig.CanFitAtDrop(start0, span))
            return false;

        var snap = _board.Capture(player);

        _board.ClearGroupsInRange(start0, span, player);
        _board.PlaceGroup(start0, anchor0, span, skill);

        if (!_board.RecalculateRuntimesAndRebalance(player, diceRig))
        {
            _board.Restore(snap, player);
            RefreshAllPreviews();
            UpdateAllIconsDim();
            return false;
        }

        RefreshAllPreviews();
        UpdateAllIconsDim();
        return true;
    }

    public bool TryAutoAssignFromClick(SkillSO skill)
    {
        if (!IsPlanning) return false;
        if (player == null || skill == null) return false;

        int span = Mathf.Clamp(skill.slotsRequired, 1, 3);

        if (!_board.TryFindEmptyPlacement(span, IsSlotActive0, out int start0, out int anchor0))
            return false;

        if (!AreSlotsActiveInRange(start0, span))
            return false;

        if (diceRig != null && !diceRig.CanFitAtDrop(start0, span))
            return false;

        var snap = _board.Capture(player);

        _board.PlaceGroup(start0, anchor0, span, skill);

        if (!_board.RecalculateRuntimesAndRebalance(player, diceRig))
        {
            _board.Restore(snap, player);
            RefreshAllPreviews();
            UpdateAllIconsDim();
            return false;
        }

        RefreshAllPreviews();
        UpdateAllIconsDim();
        return true;
    }

    public void ClearSlot(int slotIndex1Based)
    {
        if (!IsPlanning) return;

        int i0 = slotIndex1Based - 1;
        if (i0 < 0 || i0 > 2) return;

        _board.ClearGroupAtSlot0(i0, player);

        RefreshAllPreviews();
        UpdateAllIconsDim();
    }

    public bool IsSkillEquipped(SkillSO skill) => _board.IsSkillEquipped(skill);

    // ---------------------------
    // NEW Equip APIs (Damage/BuffDebuff)
    // ---------------------------
    public bool TryAssignSkillToSlot(int slotIndex1Based, SkillDamageSO skill)
        => TryAssignActiveSkillToSlot(slotIndex1Based, skill);

    public bool TryAssignSkillToSlot(int slotIndex1Based, SkillBuffDebuffSO skill)
        => TryAssignActiveSkillToSlot(slotIndex1Based, skill);

    private bool TryAssignActiveSkillToSlot(int slotIndex1Based, ScriptableObject activeSkill)
    {
        if (!IsPlanning) return false;
        if (player == null || activeSkill == null) return false;
        if (activeSkill is SkillPassiveSO) return false;

        int drop0 = slotIndex1Based - 1;
        if (drop0 < 0 || drop0 > 2) return false;

        int span = 1;
        if (activeSkill is SkillDamageSO dmg) span = Mathf.Clamp(dmg.slotsRequired, 1, 3);
        else if (activeSkill is SkillBuffDebuffSO bd) span = Mathf.Clamp(bd.slotsRequired, 1, 3);
        else if (activeSkill is SkillSO legacy) span = Mathf.Clamp(legacy.slotsRequired, 1, 3);
        else return false;

        if (!_board.ResolvePlacementForDrop(drop0, span, out int start0, out int anchor0))
            return false;

        if (!AreSlotsActiveInRange(start0, span))
            return false;

        if (diceRig != null && !diceRig.CanFitAtDrop(start0, span))
            return false;

        var snap = _board.Capture(player);

        _board.ClearGroupsInRange(start0, span, player);
        _board.PlaceGroup(start0, anchor0, span, activeSkill);

        if (!_board.RecalculateRuntimesAndRebalance(player, diceRig))
        {
            _board.Restore(snap, player);
            RefreshAllPreviews();
            UpdateAllIconsDim();
            return false;
        }

        RefreshAllPreviews();
        UpdateAllIconsDim();
        return true;
    }

    public bool TryAutoAssignFromClick(SkillDamageSO skill) => TryAutoAssignActiveFromClick(skill);
    public bool TryAutoAssignFromClick(SkillBuffDebuffSO skill) => TryAutoAssignActiveFromClick(skill);

    private bool TryAutoAssignActiveFromClick(ScriptableObject activeSkill)
    {
        if (!IsPlanning) return false;
        if (player == null || activeSkill == null) return false;
        if (activeSkill is SkillPassiveSO) return false;

        int span = 1;
        if (activeSkill is SkillDamageSO dmg) span = Mathf.Clamp(dmg.slotsRequired, 1, 3);
        else if (activeSkill is SkillBuffDebuffSO bd) span = Mathf.Clamp(bd.slotsRequired, 1, 3);
        else if (activeSkill is SkillSO legacy) span = Mathf.Clamp(legacy.slotsRequired, 1, 3);
        else return false;

        if (!_board.TryFindEmptyPlacement(span, IsSlotActive0, out int start0, out int anchor0))
            return false;

        if (!AreSlotsActiveInRange(start0, span))
            return false;

        if (diceRig != null && !diceRig.CanFitAtDrop(start0, span))
            return false;

        var snap = _board.Capture(player);

        _board.PlaceGroup(start0, anchor0, span, activeSkill);

        if (!_board.RecalculateRuntimesAndRebalance(player, diceRig))
        {
            _board.Restore(snap, player);
            RefreshAllPreviews();
            UpdateAllIconsDim();
            return false;
        }

        RefreshAllPreviews();
        UpdateAllIconsDim();
        return true;
    }

    public bool IsSkillEquipped(SkillDamageSO skill) => _board.IsSkillEquipped(skill);
    public bool IsSkillEquipped(SkillBuffDebuffSO skill) => _board.IsSkillEquipped(skill);

    // ---------------------------
    // Continue / Target flow (giữ như batch 3 của bạn)
    // ---------------------------
    public void OnContinue()
    {
        if (!IsPlanning) return;

        if (lockPlanningUIUntilRolled)
            LockPlanningUI(true);

        _cursor = 0;
        SkipEmptyOrNonAnchorForward();

        if (_cursor >= 3)
        {
            StartCoroutine(EnemyTurnThenBeginNewPlayerTurn());
            return;
        }

        SetPhase(Phase.AwaitTarget);
    }

    private IEnumerator EnemyTurnThenBeginNewPlayerTurn()
    {
        yield return EnemyTurnRoutine();
        BeginNewPlayerTurn();
    }

    public void OnTargetClicked(CombatActor clicked)
    {
        if (phase != Phase.AwaitTarget) return;
        if (!IsValidTargetForPendingSkill(clicked)) return;

        StartCoroutine(ExecuteCurrent(clicked));
    }

    private IEnumerator ExecuteCurrent(CombatActor clicked)
    {
        SetPhase(Phase.Executing);

        var rt = _board.GetAnchorRuntime(_cursor);
        if (rt == null)
        {
            var sk = _board.GetCellSkill(_cursor);
            Debug.LogError($"[TurnManager] AnchorRuntime NULL at cursor={_cursor}, skill={(sk ? sk.name : "NULL")}.", this);
            SetPhase(Phase.AwaitTarget);
            yield break;
        }

        if (executor == null)
        {
            Debug.LogError("[TurnManager] Missing SkillExecutor reference!", this);
            SetPhase(Phase.AwaitTarget);
            yield break;
        }

        int dieSum = _board.GetDieSumForAnchor(_cursor, diceRig);

        _board.ConsumeGroupAtAnchor_NoRefund(_cursor);
        RefreshAllPreviews();
        UpdateAllIconsDim();

        var aoeTargets = ResolveAoeTargets(rt);

        yield return executor.ExecuteSkill(rt, player, clicked, dieSum, skipCost: true, aoeTargets: aoeTargets);

        if (rt.kind == SkillKind.Guard)
        {
            for (int i = _cursor + 1; i < 3; i++)
            {
                if (_board.IsAnchorSlot(i))
                    _board.ClearGroupAtAnchor(i, player);
            }

            RefreshAllPreviews();
            UpdateAllIconsDim();

            yield return EnemyTurnRoutine();
            BeginNewPlayerTurn();
            yield break;
        }

        _cursor++;
        SkipEmptyOrNonAnchorForward();

        if (_cursor >= 3)
        {
            yield return EnemyTurnRoutine();
            BeginNewPlayerTurn();
            yield break;
        }

        SetPhase(Phase.AwaitTarget);
    }

    private IEnumerator EnemyTurnRoutine()
    {
        SetPhase(Phase.EnemyTurn);

        var enemies = ResolveAliveEnemiesSnapshot();

        for (int i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i];
            if (e == null || e.IsDead) continue;

            bool skipTurn = false;
            if (e.status != null)
            {
                int dot = e.status.OnTurnStarted(consumeFreezeToSkipTurn: true, out skipTurn);
                if (dot > 0) e.TakeDamage(dot, bypassGuard: true);
            }

            if (e.IsDead) continue;
            if (skipTurn) continue;

            if (player != null && !player.IsDead)
            {
                player.TakeDamage(4, bypassGuard: false);
            }

            if (player != null && player.IsDead)
                break;
        }

        if (player != null) player.guardPool = 0;

        yield return null;
    }

    private void BeginNewPlayerTurn()
    {
        SetPhase(Phase.Planning);

        if (player != null && player.status != null)
        {
            bool _unusedSkip;
            int dot = player.status.OnTurnStarted(consumeFreezeToSkipTurn: false, out _unusedSkip);
            if (dot > 0) player.TakeDamage(dot, bypassGuard: true);
        }

        if (player != null && !player.IsDead)
            player.GainFocus(1);

        _board.Reset();
        RefreshAllPreviews();
        UpdateAllIconsDim();

        if (diceRig != null)
            diceRig.BeginNewTurn();

        RefreshPlanningInteractivity();
        LockPlanningUI(false);
    }

    // -------- helpers --------
    private System.Collections.Generic.IReadOnlyList<CombatActor> ResolveAoeTargets(SkillRuntime rt)
    {
        if (rt == null) return null;
        if (!rt.hitAllEnemies && !rt.hitAllAllies) return null;

        if (rt.hitAllEnemies)
            return ResolveAliveEnemiesSnapshot();

        return null;
    }

    private System.Collections.Generic.List<CombatActor> ResolveAliveEnemiesSnapshot()
    {
        if (party != null)
            return party.GetAliveEnemies(frontOnly: false);

        var list = new System.Collections.Generic.List<CombatActor>(1);
        if (enemy != null && !enemy.IsDead) list.Add(enemy);
        return list;
    }

    private bool IsSlotActive0(int i0) => diceRig == null || diceRig.IsSlotActive(i0);

    private bool AreSlotsActiveInRange(int start0, int span)
    {
        for (int j = start0; j < start0 + span; j++)
            if (!IsSlotActive0(j)) return false;
        return true;
    }

    private void SkipEmptyOrNonAnchorForward()
    {
        while (_cursor < 3)
        {
            if (!_board.IsAnchorSlot(_cursor)) { _cursor++; continue; }
            break;
        }
    }

    private void RefreshAllPreviews()
    {
        for (int i = 0; i < 3; i++)
        {
            var d = GetDrop(i);
            if (d == null || d.iconPreview == null) continue;

            d.ClearPreview();
            d.iconPreview.rectTransform.position = ((RectTransform)d.transform).position;
        }

        for (int a = 0; a < 3; a++)
        {
            if (!_board.IsAnchorSlot(a)) continue;

            var d = GetDrop(a);
            if (d == null || d.iconPreview == null) continue;

            d.SetPreview(_board.GetCellSkill(a));
            d.iconPreview.rectTransform.position = GetGroupCenterWorldPos(a);
        }
    }

    private Vector3 GetGroupCenterWorldPos(int anchor0)
    {
        int sp = _board.GetAnchorSpan(anchor0);
        if (sp <= 1) return GetDrop(anchor0).transform.position;

        if (sp == 2)
        {
            Vector3 pA = GetDrop(anchor0).transform.position;
            Vector3 pB = GetDrop(anchor0 + 1).transform.position;
            return (pA + pB) * 0.5f;
        }

        return GetDrop(1).transform.position;
    }

    private ActionSlotDrop GetDrop(int i) => (i >= 0 && i < 3) ? _drops[i] : null;

    private void UpdateAllIconsDim()
    {
        // Legacy dimming still works.
        var all = FindObjectsOfType<DraggableSkillIcon>(true);
        foreach (var ic in all)
        {
            if (ic == null) continue;
            ic.SetInUse(IsSkillEquipped(ic.skill));
        }
    }

    private void OnDiceRolled()
    {
        _board.RecalculateRuntimesAndRebalance(player, diceRig);
        RefreshAllPreviews();
        RefreshPlanningInteractivity();
    }

    private void RefreshPlanningInteractivity()
    {
        if (phase != Phase.Planning) return;

        if (!lockPlanningUIUntilRolled)
        {
            LockPlanningUI(false); // preserves old roll button layouts
            return;
        }

        bool locked = (diceRig != null) && (!diceRig.HasRolledThisTurn || diceRig.IsRolling);
        LockPlanningUI(locked);
    }

    private void LockPlanningUI(bool locked)
    {
        if (skillBarGroup)
        {
            skillBarGroup.interactable = !locked;
            skillBarGroup.blocksRaycasts = !locked;
        }
        if (slotsPanelGroup)
        {
            slotsPanelGroup.interactable = !locked;
            slotsPanelGroup.blocksRaycasts = !locked;
        }
    }

    private bool IsValidTargetForPendingSkill(CombatActor clicked)
    {
        var rt = _board.GetAnchorRuntime(_cursor);
        if (rt == null || clicked == null || player == null) return false;

        if (rt.target == TargetRule.Self) return clicked == player;

        if (rt.target == TargetRule.Enemy)
        {
            if (clicked == player) return false;

            if (rt.kind == SkillKind.Attack && rt.range == RangeType.Melee && !rt.hitAllEnemies)
            {
                bool anyFrontAlive = false;

                if (party != null)
                {
                    var fronts = party.GetAliveEnemies(frontOnly: true);
                    anyFrontAlive = fronts != null && fronts.Count > 0;
                }
                else
                {
                    anyFrontAlive = (enemy != null && !enemy.IsDead && enemy.row == CombatActor.RowTag.Front);
                }

                if (anyFrontAlive && clicked.row != CombatActor.RowTag.Front)
                    return false;
            }

            return true;
        }

        return false;
    }

    private static bool SpacePressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        return kb != null && kb.spaceKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Space);
#endif
    }

    private void SetPhase(Phase newPhase)
    {
        if (phase == newPhase) return;

        phase = newPhase;

        if (logPhase)
            Debug.Log($"[TurnManager] Phase => {phase}", this);
    }
}
