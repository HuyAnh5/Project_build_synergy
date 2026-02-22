using System.Collections;
using System.Collections.Generic;
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

    [Header("Enemy Turn")]
    public float delayBetweenEnemyAttacks = 0.2f;

    [Header("Debug")]
    public bool logPhase = true;
    public KeyCode toggleLogKey = KeyCode.F10;

    // Planning is allowed ONLY after dice rolled (and not rolling)
    public bool IsPlanning =>
        phase == Phase.Planning &&
        (diceRig == null || (diceRig.HasRolledThisTurn && !diceRig.IsRolling));

    private readonly ActionSlotDrop[] _drops = new ActionSlotDrop[3];
    // --- Slot Collapse support ---
    private readonly bool[] _baseSlotActive = new bool[3];
    private int _slotCollapseKeepIndex = -1;

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

            // ✅ hook dice debuff layer
            diceRig.onComputeAllDiceDelta += ComputeAllDiceDelta;

            diceRig.BeginNewTurn();
            // capture baseline slot actives (what "normal" means for this rig)
            for (int i = 0; i < 3; i++)
                _baseSlotActive[i] = diceRig.slots[i].active;
            if (diceRig != null && diceRig.slots != null)
            {
                for (int i = 0; i < 3 && i < diceRig.slots.Length; i++)
                    _baseSlotActive[i] = diceRig.slots[i].active;
            }

            // apply any slot debuff immediately (if combat starts with it)
            ApplyPlayerSlotDebuffs();
        }

        if (logPhase)
        {
            Debug.Log($"[TM] Start refs: player={(player ? player.name : "NULL")} enemy={(enemy ? enemy.name : "NULL")} party={(party ? party.name : "NULL")} diceRig={(diceRig ? diceRig.name : "NULL")} executor={(executor ? executor.name : "NULL")}", this);
            Debug.Log($"[TM] player.status={(player && player.status ? player.status.name : "NULL")}", this);
        }

        RefreshPlanningInteractivity();
        RefreshAllPreviews();
        UpdateAllIconsDim();
    }


    void OnDestroy()
    {
        if (diceRig != null)
        {
            diceRig.onAllDiceRolled -= OnDiceRolled;
            diceRig.onComputeAllDiceDelta -= ComputeAllDiceDelta;
        }
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

        // ✅ giữ state board/diceRig nhất quán
        _board.RecalculateRuntimesAndRebalance(player, diceRig);

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

        if (logPhase)
        {
            var asset = (_cursor < 3) ? _board.GetCellSkillAsset(_cursor) : null;
            var rt = (_cursor < 3) ? _board.GetAnchorRuntime(_cursor) : null;

            Debug.Log($"[TM] Continue -> cursor={_cursor} asset={(asset ? asset.name : "NULL")} type={(asset ? asset.GetType().Name : "NULL")} rt.kind={(rt.kind)} rt.target={(rt.target)} span={_board.GetAnchorSpan(_cursor)} start0={_board.GetStartForAnchor(_cursor)}", this);
        }

        if (_cursor >= 3)
        {
            EndPlayerTurn_TickStatusesAndPassives();
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
        if (logPhase)
            Debug.Log($"[TM] OnTargetClicked phase={phase} cursor={_cursor} clicked={(clicked ? clicked.name : "NULL")}", this);

        if (phase != Phase.AwaitTarget) return;

        if (!TryValidateTargetForPendingSkill(clicked, out string reason))
        {
            if (logPhase)
            {
                var asset = _board.GetCellSkillAsset(_cursor);
                Debug.LogWarning($"[TM] Target INVALID: {reason} | asset={(asset ? asset.name : "NULL")} type={(asset ? asset.GetType().Name : "NULL")}", this);
            }
            return;
        }

        StartCoroutine(ExecuteCurrent(clicked));
    }


    private IEnumerator ExecuteCurrent(CombatActor clicked)
    {
        SetPhase(Phase.Executing);

        var asset = _board.GetCellSkillAsset(_cursor);
        var rt = _board.GetAnchorRuntime(_cursor);

        if (rt == null)
        {
            Debug.LogError($"[TM] AnchorRuntime NULL at cursor={_cursor}, asset={(asset ? asset.name : "NULL")}.", this);
            SetPhase(Phase.AwaitTarget);
            yield break;
        }

        if (executor == null)
        {
            Debug.LogError("[TM] Missing SkillExecutor reference!", this);
            SetPhase(Phase.AwaitTarget);
            yield break;
        }

        // Dice debug: raw vs resolved
        int start0 = _board.GetStartForAnchor(_cursor);
        int span = _board.GetAnchorSpan(_cursor);
        int rawSum = _board.GetDieSumForAnchor(_cursor, diceRig);
        int resolvedSum = ComputeResolvedDieSum(start0, span);
        int maxFace = ComputeMaxFace(start0, span);

        if (logPhase)
        {
            Debug.Log($"[TM] Execute cursor={_cursor} asset={(asset ? asset.name : "NULL")} type={(asset ? asset.GetType().Name : "NULL")} rt.kind={rt.kind} rt.target={rt.target} span={span} start0={start0} rawSum={rawSum} resolvedSum={resolvedSum} maxFace={maxFace} playerFocus={(player ? player.focus : -999)}", this);
        }

        _board.ConsumeGroupAtAnchor_NoRefund(_cursor);
        RefreshAllPreviews();
        UpdateAllIconsDim();

        // ✅ ROUTE: BuffDebuff phải gọi overload SkillBuffDebuffSO, runtime Utility không làm gì
        if (asset is SkillBuffDebuffSO buffSkill)
        {
            var aoeTargets = (buffSkill.target == SkillTargetRule.AllEnemies || buffSkill.target == SkillTargetRule.AllUnits)
                ? ResolveAliveEnemiesSnapshot()
                : null;

            if (logPhase)
                Debug.Log($"[TM] Branch=BuffDebuff -> {buffSkill.name} targetRule={buffSkill.target} delay={buffSkill.applyDelayTurns} effects={(buffSkill.effects != null ? buffSkill.effects.Count : 0)} applyAilment={buffSkill.applyAilment}", this);

            yield return executor.ExecuteSkill(buffSkill, player, clicked, resolvedSum, maxFace, skipCost: true, aoeTargets: aoeTargets);
        }
        else
        {
            var aoeTargets = ResolveAoeTargets(rt);

            if (logPhase)
                Debug.Log($"[TM] Branch=Runtime (Attack/Guard/Legacy) -> rt.kind={rt.kind}", this);

            yield return executor.ExecuteSkill(rt, player, clicked, resolvedSum, skipCost: true, aoeTargets: aoeTargets);
        }

        if (rt.kind == SkillKind.Guard)
        {
            for (int i = _cursor + 1; i < 3; i++)
            {
                if (_board.IsAnchorSlot(i))
                    _board.ClearGroupAtAnchor(i, player);
            }

            RefreshAllPreviews();
            UpdateAllIconsDim();

            if (logPhase) Debug.Log("[TM] Guard ends player turn -> EnemyTurn", this);

            EndPlayerTurn_TickStatusesAndPassives();
            yield return EnemyTurnRoutine();
            BeginNewPlayerTurn();
            yield break;
        }

        _cursor++;
        SkipEmptyOrNonAnchorForward();

        if (_cursor >= 3)
        {
            if (logPhase) Debug.Log("[TM] Player actions done -> EnemyTurn", this);

            EndPlayerTurn_TickStatusesAndPassives();
            yield return EnemyTurnRoutine();
            BeginNewPlayerTurn();
            yield break;
        }

        SetPhase(Phase.AwaitTarget);
    }

    private IEnumerator EnemyTurnRoutine()
    {
        SetPhase(Phase.EnemyTurn);

        // ✅ Delay trước enemy đầu tiên (để không đánh ngay lập tức khi vừa bấm Continue)
        if (delayBetweenEnemyAttacks > 0f)
            yield return new WaitForSeconds(delayBetweenEnemyAttacks);

        var enemies = ResolveAliveEnemiesSnapshot();

        for (int i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i];
            if (e == null || e.IsDead) continue;

            bool skipTurn = false;

            if (e.status != null)
            {
                int dot = e.status.OnTurnStarted(consumeFreezeToSkipTurn: true, out skipTurn);
                if (logPhase) Debug.Log(
                    $"[TM] EnemyTurnStart {e.name}: dot={dot} skip={skipTurn} bleed={e.status.bleedTurns} burnStacks={e.status.burnStacks} burnTurns={e.status.burnTurns} frozen={e.status.frozen}",
                    this
                );

                if (dot > 0) e.TakeDamage(dot, bypassGuard: true);
            }

            if (e.IsDead) continue;

            if (skipTurn)
            {
                if (e.status != null) e.status.OnOwnerTurnEnded();

                // ✅ Nhịp đều: skip cũng delay như một lượt “hành động”
                if (delayBetweenEnemyAttacks > 0f)
                    yield return new WaitForSeconds(delayBetweenEnemyAttacks);

                continue;
            }

            if (player != null && !player.IsDead)
            {
                var brain = e.GetComponent<EnemyBrainController>();

                // nếu có brain+definition thì cast skill thật
                if (brain != null && brain.definition != null && brain.definition.moves != null && brain.definition.moves.Count > 0)
                {
                    // nếu chưa có intent thì decide ngay (tạm thời; STS chuẩn là decide cuối lượt player)
                    if (!brain.CurrentIntent.hasIntent)
                        brain.DecideNextIntent(player);

                    if (brain.CurrentIntent.hasIntent)
                    {
                        var move = brain.definition.moves[brain.CurrentIntent.moveIndex];

                        // chọn skill để cast: ưu tiên BuffDebuff nếu có, không thì Damage
                        if (move.buffDebuffSkill != null)
                        {
                            // resolve target cho buff/debuff
                            CombatActor clicked = null;
                            IReadOnlyList<CombatActor> aoe = null;

                            switch (move.buffDebuffSkill.target)
                            {
                                case SkillTargetRule.Self:
                                    clicked = e;
                                    break;

                                case SkillTargetRule.SingleEnemy:
                                    clicked = player;
                                    break;

                                case SkillTargetRule.SingleAlly:
                                    // ✅ heal ally yếu nhất nếu move tag Heal
                                    if ((move.tags & EnemyDefinitionSO.EnemyMoveTag.Heal) != 0)
                                        clicked = brain.PickMostInjuredEnemyAlly(party, includeSelf: true);
                                    else
                                        clicked = e; // fallback
                                    break;

                                case SkillTargetRule.AllEnemies:
                                    aoe = party != null ? party.GetAliveAllies(includePlayer: true) : null;
                                    clicked = (aoe != null && aoe.Count > 0) ? aoe[0] : player;
                                    break;

                                case SkillTargetRule.AllAllies:
                                    aoe = party != null ? party.GetAliveEnemies(frontOnly: false) : null;
                                    clicked = e;
                                    break;

                                case SkillTargetRule.AllUnits:
                                    if (party != null)
                                    {
                                        var tmp = new List<CombatActor>();
                                        tmp.AddRange(party.GetAliveAllies(includePlayer: true));
                                        tmp.AddRange(party.GetAliveEnemies(frontOnly: false));
                                        aoe = tmp;
                                        clicked = (aoe.Count > 0) ? aoe[0] : player;
                                    }
                                    else clicked = player;
                                    break;
                            }

                            // nếu SingleAlly heal mà không ai thiếu máu => fallback attack nhẹ (hoặc skip)
                            if (move.buffDebuffSkill.target == SkillTargetRule.SingleAlly && clicked == null)
                            {
                                // fallback: đánh player bằng damageSkill nếu có
                                if (move.damageSkill != null)
                                    yield return executor.ExecuteSkill(move.damageSkill, e, player, dieValue: 3, skipCost: true);
                            }
                            else
                            {
                                yield return executor.ExecuteSkill(
                                    move.buffDebuffSkill, e, clicked,
                                    rolledValue: 3, maxFaceValue: 6,
                                    skipCost: true,
                                    aoeTargets: aoe
                                );
                            }
                        }
                        else if (move.damageSkill != null)
                        {
                            // resolve target cho damage
                            CombatActor target = null;
                            switch (move.damageSkill.target)
                            {
                                case SkillTargetRule.Self: target = e; break;
                                case SkillTargetRule.SingleEnemy: target = player; break;
                                case SkillTargetRule.SingleAlly: target = e; break; // damage hiếm khi dùng
                                default: target = player; break;
                            }

                            if (target != null)
                                yield return executor.ExecuteSkill(move.damageSkill, e, target, dieValue: 3, skipCost: true);
                        }

                        // consume intent + tick cooldown turn (tối thiểu để cooldown hoạt động)
                        brain.ConsumeCurrentIntent();
                        brain.AdvanceTurnTick();

                        if (delayBetweenEnemyAttacks > 0f)
                            yield return new WaitForSeconds(delayBetweenEnemyAttacks);

                        if (e.status != null) e.status.OnOwnerTurnEnded();
                        continue;
                    }
                }

                // fallback cuối cùng nếu chưa có brain/move/skill
                player.TakeDamage(4, bypassGuard: false);
                if (delayBetweenEnemyAttacks > 0f)
                    yield return new WaitForSeconds(delayBetweenEnemyAttacks);
            }

            if (e.status != null) e.status.OnOwnerTurnEnded();

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
            if (logPhase) Debug.Log($"[TM] PlayerTurnStart dot={dot} focusBefore={player.focus} diceDelta={player.status.GetAllDiceDelta()} ailment={(player.status != null && player.status.HasAilment(out var at, out _) ? at.ToString() : "None")}", this);

            if (dot > 0) player.TakeDamage(dot, bypassGuard: true);
        }

        // NEW: Passive turn-start bonuses (non-breaking)
        if (player != null && !player.IsDead)
        {
            var ps = player.GetComponent<PassiveSystem>();
            if (ps != null)
            {
                int bonus = ps.GetFocusBonusOnTurnStart();
                if (bonus != 0)
                {
                    player.GainFocus(bonus);
                    if (logPhase) Debug.Log($"[TM] Passive FocusBonusOnTurnStart +{bonus} -> focus={player.focus}/{player.maxFocus}", this);
                }
            }
            else
            {
                if (logPhase) Debug.Log("[TM] PassiveSystem not found on player (passives won't apply).", this);
            }

            // existing baseline focus gain
            player.GainFocus(1);
        }

        _board.Reset();
        RefreshAllPreviews();
        UpdateAllIconsDim();

        if (diceRig != null)
        {
            // ✅ restore baseline then apply slot-collapse (if any) BEFORE BeginNewTurn
            RestoreBaselineSlots();
            ApplySlotCollapseToRig();

            diceRig.BeginNewTurn(); // will ApplyActiveStates again (safe)
        }

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
        // clear all
        for (int i = 0; i < 3; i++)
        {
            var d = GetDrop(i);
            if (d == null || d.iconPreview == null) continue;

            d.ClearPreview();
            d.iconPreview.rectTransform.position = ((RectTransform)d.transform).position;
        }

        // draw anchors
        for (int a = 0; a < 3; a++)
        {
            if (!_board.IsAnchorSlot(a)) continue;

            var d = GetDrop(a);
            if (d == null || d.iconPreview == null) continue;

            // ✅ IMPORTANT: lấy asset thật (SkillSO / SkillDamageSO / SkillBuffDebuffSO)
            var asset = _board.GetCellSkillAsset(a);

            d.SetPreview(asset);
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
        var all = FindObjectsOfType<DraggableSkillIcon>(true);
        foreach (var ic in all)
        {
            if (ic == null) continue;

            var asset = ic.GetSkillAsset(); // ✅ SkillSO / SkillDamageSO / SkillBuffDebuffSO / Passive
            bool inUse = (asset != null) && _board.IsSkillEquipped(asset);

            ic.SetInUse(inUse);
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

        return TryValidateTargetForPendingSkill(clicked, out _);

        // ✅ V2 targeting (new system, incl. SkillBuffDebuffSO)
        if (rt.useV2Targeting)
        {
            switch (rt.targetRuleV2)
            {
                case SkillTargetRule.Self:
                    return clicked == player;

                case SkillTargetRule.SingleAlly:
                case SkillTargetRule.AllAllies:
                    // prototype: only player ally exists
                    return clicked == player;

                case SkillTargetRule.SingleEnemy:
                case SkillTargetRule.AllEnemies:
                    if (clicked == player) return false;
                    break;

                case SkillTargetRule.AllUnits:
                    // allow clicking anything to confirm
                    break;

                default:
                    break;
            }
        }
        else
        {
            // Legacy targeting
            if (rt.target == TargetRule.Self) return clicked == player;
            if (rt.target == TargetRule.Enemy && clicked == player) return false;
        }

        // ✅ melee restriction: only for Attack melee single-target
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

            if (anyFrontAlive && clicked != player && clicked.row != CombatActor.RowTag.Front)
                return false;
        }

        return true;
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

    private int ComputeAllDiceDelta(CombatActor owner)
    {
        if (owner == null || owner.status == null) return 0;
        return owner.status.GetAllDiceDelta();
    }

    private void EndPlayerTurn_TickStatusesAndPassives()
    {
        if (player == null || player.IsDead) return;

        // --- PASSIVES: end-of-player-turn hooks ---
        var ps = player.GetComponent<PassiveSystem>();
        if (ps != null)
        {
            int flat = ps.GetGuardFlatAtTurnEnd();
            if (flat != 0)
            {
                float mult = 1f + Mathf.Max(-0.99f, ps.GetGuardGainPercent());
                int scaled = Mathf.CeilToInt(flat * mult);
                if (scaled != 0)
                {
                    player.AddGuard(scaled);
                    if (logPhase) Debug.Log($"[TM] Passive GuardFlatAtTurnEnd +{flat} (x{mult:0.##} => +{scaled}) -> guard={player.guardPool}", this);
                }
            }
        }

        // statuses tick at end of owner turn
        if (player.status != null)
            player.status.OnOwnerTurnEnded();
    }

    private void ApplyPlayerSlotDebuffs()
    {
        if (diceRig == null) return;

        // 1) restore baseline (để khi debuff hết thì slot tự bật lại)
        for (int i = 0; i < 3; i++)
            diceRig.slots[i].active = _baseSlotActive[i];

        // 2) apply SlotCollapse (chỉ 1 slot usable)
        bool collapse = (player != null && player.status != null && player.status.HasSlotCollapse());
        if (collapse)
        {
            // giữ slot active đầu tiên trong baseline (thường là slot 0)
            int keep = -1;
            for (int i = 0; i < 3; i++)
            {
                if (_baseSlotActive[i]) { keep = i; break; }
            }
            if (keep < 0) keep = 0;

            for (int i = 0; i < 3; i++)
                if (i != keep) diceRig.slots[i].active = false;
        }

        diceRig.ApplyActiveStates();

        if (logPhase)
            Debug.Log($"[TM] SlotCollapse={(collapse ? "ON" : "off")} activeSlots={diceRig.ActiveSlotCount()}", this);
    }

    private int GetResolvedDieSumForAnchor(int anchor0)
    {
        if (diceRig == null || player == null) return 0;

        int sp = Mathf.Clamp(_board.GetAnchorSpan(anchor0), 0, 3);
        if (sp <= 0) return 0;

        int start0 = Mathf.Clamp(_board.GetStartForAnchor(anchor0), 0, 2);

        int sum = 0;
        for (int i = start0; i < start0 + sp; i++)
            sum += diceRig.GetResolvedDieValue(i, player);

        return sum;
    }

    private int GetMaxFaceValueForAnchor(int anchor0)
    {
        if (diceRig == null) return 6;

        int sp = Mathf.Clamp(_board.GetAnchorSpan(anchor0), 0, 3);
        if (sp <= 0) return 6;

        int start0 = Mathf.Clamp(_board.GetStartForAnchor(anchor0), 0, 2);

        int max = 1;
        for (int i = start0; i < start0 + sp; i++)
            max = Mathf.Max(max, diceRig.GetMaxFaceValue(i));

        return Mathf.Max(1, max);
    }

    // Optional: passive focus bonus (reflection, no hard dependency)
    private int GetPassiveFocusBonusOnTurnStart_Reflection(CombatActor actor)
    {
        if (actor == null) return 0;

        Component ps = actor.GetComponent("PassiveSystem");
        if (ps == null) return 0;

        try
        {
            var t = ps.GetType();
            var m = t.GetMethod("GetFocusBonusOnTurnStart");
            if (m != null)
            {
                object v = m.Invoke(ps, null);
                if (v is int i) return i;
            }
        }
        catch { }
        return 0;
    }


    private bool TryValidateTargetForPendingSkill(CombatActor clicked, out string reason)
    {
        reason = "";
        var rt = _board.GetAnchorRuntime(_cursor);
        if (rt == null) { reason = "rt == null"; return false; }
        if (clicked == null) { reason = "clicked == null (không raycast trúng actor?)"; return false; }
        if (player == null) { reason = "player == null"; return false; }

        // Self
        if (rt.target == TargetRule.Self)
        {
            if (clicked != player) { reason = "rt.target=Self nhưng clicked != player"; return false; }
            return true;
        }

        // Enemy
        if (rt.target == TargetRule.Enemy)
        {
            if (clicked == player) { reason = "rt.target=Enemy nhưng clicked == player"; return false; }

            // melee front-only only for Attack (Utility buff/debuff không vào đây)
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
                {
                    reason = "melee front-only: clicked không phải Front";
                    return false;
                }
            }

            return true;
        }

        reason = $"Unhandled rt.target={rt.target}";
        return false;
    }

    private int ComputeResolvedDieSum(int start0, int span)
    {
        if (diceRig == null || player == null) return 0;
        span = Mathf.Clamp(span, 1, 3);
        start0 = Mathf.Clamp(start0, 0, 2);

        int sum = 0;
        for (int i = start0; i < start0 + span; i++)
            sum += diceRig.GetResolvedDieValue(i, player);

        return sum;
    }

    private int ComputeMaxFace(int start0, int span)
    {
        if (diceRig == null) return 6;
        span = Mathf.Clamp(span, 1, 3);
        start0 = Mathf.Clamp(start0, 0, 2);

        int max = 1;
        for (int i = start0; i < start0 + span; i++)
            max = Mathf.Max(max, diceRig.GetMaxFaceValue(i));

        return Mathf.Max(1, max);
    }




    private void RestoreBaselineSlots()
    {
        if (diceRig == null || diceRig.slots == null) return;

        for (int i = 0; i < 3 && i < diceRig.slots.Length; i++)
            diceRig.slots[i].active = _baseSlotActive[i];

        _slotCollapseKeepIndex = -1;
    }

    private void ApplySlotCollapseToRig()
    {
        if (diceRig == null || diceRig.slots == null) return;
        if (player == null || player.status == null) return;
        if (!player.status.HasSlotCollapse()) return;

        // collect currently active slots (baseline)
        int[] actives = new int[3];
        int activeCount = 0;

        for (int i = 0; i < 3 && i < diceRig.slots.Length; i++)
        {
            if (diceRig.slots[i].active)
                actives[activeCount++] = i;
        }

        // baseline only has 0/1 slot -> nothing to collapse
        if (activeCount <= 1)
        {
            diceRig.ApplyActiveStates();
            return;
        }

        // pick ONE slot to keep (random once per player turn)
        _slotCollapseKeepIndex = actives[UnityEngine.Random.Range(0, activeCount)];

        for (int k = 0; k < activeCount; k++)
        {
            int idx = actives[k];
            if (idx != _slotCollapseKeepIndex)
                diceRig.slots[idx].active = false;
        }

        diceRig.ApplyActiveStates();

        if (logPhase)
            Debug.Log($"[TM] SlotCollapse ON -> keep slot {_slotCollapseKeepIndex}", this);
    }
}
