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
    public SkillExecutor executor;

    [Header("Dice Rig (IMPORTANT)")]
    public DiceSlotRig diceRig;

    [Header("Optional: lock planning UI before roll / after Continue")]
    public CanvasGroup skillBarGroup;
    public CanvasGroup slotsPanelGroup;

    public enum Phase { Planning, AwaitTarget, Executing, EnemyTurn }
    public Phase phase = Phase.Planning;

    [Header("Debug")]
    public bool logPhase = true;
    public KeyCode toggleLogKey = KeyCode.F10;

    // Planning is allowed ONLY after dice rolled (and not rolling)
    public bool IsPlanning =>
        phase == Phase.Planning &&
        (diceRig == null || (diceRig.HasRolledThisTurn && !diceRig.IsRolling));

    // UI slot drops register here (slotIndex: 1..3)
    private readonly ActionSlotDrop[] _drops = new ActionSlotDrop[3];

    // Plan board (3-slot grouping + reserved focus + runtime evaluation)
    private readonly SkillPlanBoard _board = new SkillPlanBoard();

    private int _cursor = 0;

    void Start()
    {
        _board.Reset();

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
        // Press Space to roll ONCE during Planning (before placing skills).
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

    // ---------------------------
    // Registration (ActionSlotDrop should call this in Awake/Start)
    // ---------------------------
    public void RegisterDrop(ActionSlotDrop drop)
    {
        if (!drop) return;
        int i = drop.slotIndex - 1;
        if (i < 0 || i > 2) return;

        _drops[i] = drop;
        RefreshAllPreviews();
    }

    // ---------------------------
    // Equip APIs (called by drag-drop / click)
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

        // block multi-slot placement if slots are not active
        if (!AreSlotsActiveInRange(start0, span))
            return false;

        // also respect DiceSlotRig rule (covers edge cases)
        if (diceRig != null && !diceRig.CanFitAtDrop(start0, span))
            return false;

        // transaction: try apply & recalc. If cost is impossible, rollback.
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

    // Click-to-equip: ONLY fills empty placement (no replace when full)
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
    // Continue / Target flow
    // ---------------------------
    public void OnContinue()
    {
        if (!IsPlanning) return;

        LockPlanningUI(true);
        SetPhase(Phase.AwaitTarget);
        _cursor = 0;
        SkipEmptyOrNonAnchorForward();
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
            Debug.LogError($"[TurnManager] AnchorRuntime NULL at cursor={_cursor}, skill={(sk ? sk.name : "NULL")}. " +
                           $"Did SkillRuntimeEvaluator return null or did board not recalc?", this);

            // ĐỪNG cho mất lượt oan
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

        if (rt != null && executor != null)
        {
            // focus was reserved at equip time => skipCost = true
            // consume NGAY khi bắt đầu thực hiện action (icon biến mất ngay)
            _board.ConsumeGroupAtAnchor_NoRefund(_cursor);
            RefreshAllPreviews();
            UpdateAllIconsDim();

            // rồi mới chạy animation / projectile / damage
            yield return executor.ExecuteSkill(rt, player, clicked, dieSum, skipCost: true);
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

        if (enemy != null && enemy.status != null)
        {
            int dot = enemy.status.TickStartOfTurnDamage();
            if (dot > 0) enemy.TakeDamage(dot, bypassGuard: true);

            if (enemy.status.frozen)
            {
                enemy.status.frozen = false;
                yield break;
            }
        }

        if (player != null && !player.IsDead)
        {
            player.TakeDamage(4, bypassGuard: false);
            player.guardPool = 0; // guard doesn't carry
        }

        yield return null;
    }

    private void BeginNewPlayerTurn()
    {
        SetPhase(Phase.Planning);

        // planning board should be empty now, but safe reset
        _board.Reset();
        RefreshAllPreviews();
        UpdateAllIconsDim();

        if (diceRig != null)
            diceRig.BeginNewTurn();

        RefreshPlanningInteractivity();
        LockPlanningUI(false);
    }

    // ---------------------------
    // Internals: slot checks / cursor
    // ---------------------------
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

    // ---------------------------
    // UI Preview positioning (center icon for 2/3 slots)
    // ---------------------------
    private void RefreshAllPreviews()
    {
        // clear all previews and reset their icon position to their slot center
        for (int i = 0; i < 3; i++)
        {
            var d = GetDrop(i);
            if (d == null || d.iconPreview == null) continue;

            d.ClearPreview();
            d.iconPreview.rectTransform.position = ((RectTransform)d.transform).position;
        }

        // draw ONLY at anchors, place at center of occupied slots
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

        // sp == 3 => center is slot2 (index 1)
        return GetDrop(1).transform.position;
    }

    private ActionSlotDrop GetDrop(int i) => (i >= 0 && i < 3) ? _drops[i] : null;

    private void UpdateAllIconsDim()
    {
        var all = FindObjectsOfType<DraggableSkillIcon>(true);
        foreach (var ic in all)
        {
            if (ic == null) continue;
            ic.SetInUse(IsSkillEquipped(ic.skill));
        }
    }

    // ---------------------------
    // Planning UI lock
    // ---------------------------
    private void OnDiceRolled()
    {
        // If you allow any reroll / dice edit later, this keeps reserved cost in sync.
        _board.RecalculateRuntimesAndRebalance(player, diceRig);
        RefreshAllPreviews();
        RefreshPlanningInteractivity();
    }

    private void RefreshPlanningInteractivity()
    {
        if (phase != Phase.Planning) return;

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

    // ---------------------------
    // Target gating
    // ---------------------------
    private bool IsValidTargetForPendingSkill(CombatActor clicked)
    {
        var rt = _board.GetAnchorRuntime(_cursor);
        if (rt == null || clicked == null || player == null) return false;

        if (rt.target == TargetRule.Self) return clicked == player;
        if (rt.target == TargetRule.Enemy) return clicked != player;
        return false;
    }

    // ---------------------------
    // Input
    // ---------------------------
    private static bool SpacePressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        return kb != null && kb.spaceKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Space);
#endif
    }

    // ---------------------------
    // Debug logging
    // ---------------------------
    private void SetPhase(Phase newPhase)
    {
        if (phase == newPhase) return;

        phase = newPhase;

        if (logPhase)
            Debug.Log($"[TurnManager] Phase => {phase}", this);
    }
}
