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
    private readonly HashSet<DiceSpinnerGeneric> _spentDiceThisTurn = new HashSet<DiceSpinnerGeneric>();
    private bool _victoryResolvedThisCombat;

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
            DiceCombatEnchantRuntimeUtility.BeginCombat(diceRig);
        }

        _victoryResolvedThisCombat = false;

        if (logPhase)
        {
            Debug.Log($"[TM] Start refs: player={(player ? player.name : "NULL")} enemy={(enemy ? enemy.name : "NULL")} party={(party ? party.name : "NULL")} diceRig={(diceRig ? diceRig.name : "NULL")} executor={(executor ? executor.name : "NULL")}", this);
            Debug.Log($"[TM] player.status={(player && player.status ? player.status.name : "NULL")}", this);
        }

        RefreshPlanningInteractivity();
        RefreshAllPreviews();
        UpdateAllIconsDim();
        UpdateAllDiceDim();
        BeginNewPlayerTurn();
        EnsureAllEnemyIntentsNow();
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

        // IMPORTANT:
        // A combat slot pair (e.g. IconSlot1) can move to another visual lane after dice reorder.
        // Remove stale references first so the same drop is never registered in multiple lanes.
        for (int k = 0; k < _drops.Length; k++)
        {
            if (_drops[k] == drop)
                _drops[k] = null;
        }

        _drops[i] = drop;
        RefreshAllPreviews();
    }

    public void ClearSlot(int slotIndex1Based)
        => TurnManagerPlanningUtility.ClearSlot(IsPlanning, player, diceRig, _board, slotIndex1Based, RefreshAllViews, UpdateAllIconsDim);

    // ---------------------------
    // Equip APIs (Damage/BuffDebuff)
    // ---------------------------
    public bool TryAssignSkillToSlot(int slotIndex1Based, SkillDamageSO skill)
        => TryAssignActiveSkillToSlot(slotIndex1Based, skill);

    public bool TryAssignSkillToSlot(int slotIndex1Based, SkillBuffDebuffSO skill)
        => TryAssignActiveSkillToSlot(slotIndex1Based, skill);

    private bool TryAssignActiveSkillToSlot(int slotIndex1Based, ScriptableObject activeSkill)
        => TurnManagerPlanningUtility.TryAssignSkillToSlot(IsPlanning, player, diceRig, _board, slotIndex1Based, activeSkill, IsSlotAssignable0, AreSlotsActiveInRange, RefreshAllViews, UpdateAllIconsDim, clearExistingGroups: true);

    public bool TryAutoAssignFromClick(SkillDamageSO skill) => TryAutoAssignActiveFromClick(skill);
    public bool TryAutoAssignFromClick(SkillBuffDebuffSO skill) => TryAutoAssignActiveFromClick(skill);

    private bool TryAutoAssignActiveFromClick(ScriptableObject activeSkill)
        => TurnManagerPlanningUtility.TryAutoAssignFromClick(IsPlanning, player, diceRig, _board, activeSkill, IsSlotActive0, IsSlotAssignable0, AreSlotsActiveInRange, RefreshAllViews, UpdateAllIconsDim);

    public bool IsSkillEquipped(SkillDamageSO skill) => _board.IsSkillEquipped(skill);
    public bool IsSkillEquipped(SkillBuffDebuffSO skill) => _board.IsSkillEquipped(skill);

    public bool CanPrototypeCastSkillNow(ScriptableObject activeSkill)
    {
        if (player == null || activeSkill == null)
            return false;
        if (activeSkill is SkillPassiveSO)
            return false;
        if (_board.IsSkillEquipped(activeSkill))
            return false;

        return TryResolvePrototypeCastPlacement(activeSkill, out _, out _, commit: false);
    }

    public bool TryCastDraggedSkillToTarget(ScriptableObject activeSkill, CombatActor clicked)
    {
        if (!IsPlanning || player == null || activeSkill == null || clicked == null)
            return false;
        if (activeSkill is SkillPassiveSO)
            return false;
        if (_board.IsSkillEquipped(activeSkill))
            return false;

        int span = GetSkillSpan(activeSkill);
        if (span <= 0)
            return false;

        if (!TryResolvePrototypeCastPlacement(activeSkill, out _, out int anchor0, commit: true))
        {
            RefreshAllViews();
            return false;
        }

        _cursor = anchor0;
        if (!TryValidateTargetForPendingSkill(clicked, out _))
        {
            _board.ClearGroupAtAnchor(anchor0, player);
            _board.RecalculateRuntimesAndRebalance(player, diceRig);
            RefreshAllViews();
            return false;
        }

        RefreshAllViews();
        StartCoroutine(ExecuteCurrent(clicked));
        return true;
    }

    public bool TryCastDraggedSkillToSelf(ScriptableObject activeSkill)
    {
        if (player == null)
            return false;
        return TryCastDraggedSkillToTarget(activeSkill, player);
    }

    public bool CommitDiceLaneReorder(int[] permutation)
    {
        if (!IsPlanning) return false;

        var snap = _board.Capture(player);
        if (!_board.TryApplyLanePermutation(permutation, player, diceRig))
        {
            _board.Restore(snap, player);
            RefreshAllViews();
            return false;
        }

        RefreshAllViews();
        return true;
    }

    public bool TryGetPlannedGroupAtLane(int lane1Based, out int anchor0, out int start0, out int span)
    {
        anchor0 = -1;
        start0 = -1;
        span = 0;

        int lane0 = lane1Based - 1;
        if (lane0 < 0 || lane0 > 2)
            return false;

        anchor0 = _board.GetCellAnchor(lane0);
        if (anchor0 < 0 || !_board.IsAnchorSlot(anchor0))
        {
            anchor0 = -1;
            return false;
        }

        start0 = _board.GetStartForAnchor(anchor0);
        span = _board.GetAnchorSpan(anchor0);
        return span > 0;
    }

    public void RefreshPlanningAfterDiceValueReorder()
    {
        _board.RecalculateRuntimesAndRebalance(player, diceRig);
        RefreshAllViews();
    }

    // ---------------------------
    // Continue / Target flow (giữ như batch 3 của bạn)
    // ---------------------------
    public void OnContinue()
    {
        if (!IsPlanning) return;
        StartCoroutine(ContinueRoutine());
    }

    private IEnumerator ContinueRoutine()
    {
        if (TryHandleCombatVictory())
            yield break;

        EndPlayerTurn_TickStatusesAndPassives();
        yield return EnemyTurnThenBeginNewPlayerTurn();
    }


    private IEnumerator EnemyTurnThenBeginNewPlayerTurn()
    {
        yield return EnemyTurnRoutine();
        if (TryHandleCombatVictory())
            yield break;
        BeginNewPlayerTurn();
    }

    public void OnTargetClicked(CombatActor clicked)
    {
        if (logPhase)
            Debug.Log($"[TM] OnTargetClicked phase={phase} cursor={_cursor} clicked={(clicked ? clicked.name : "NULL")}", this);

        if (phase == Phase.Planning)
        {
            _cursor = FindNextExecutableAnchor();
            if (_cursor < 0)
                return;
        }
        else if (phase != Phase.AwaitTarget)
        {
            return;
        }

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
            SetPhase(Phase.Planning);
            yield break;
        }

        if (executor == null)
        {
            Debug.LogError("[TM] Missing SkillExecutor reference!", this);
            SetPhase(Phase.Planning);
            yield break;
        }

        // Dice debug: raw vs resolved
        int start0 = _board.GetStartForAnchor(_cursor);
        int span = _board.GetAnchorSpan(_cursor);
        int rawSum = _board.GetDieSumForAnchor(_cursor, diceRig);
        ElementType dieElement = GetResolvedDiceElement(rt, asset);
        int resolvedSum = ComputeResolvedDieSum(start0, span, dieElement);
        int maxFace = ComputeMaxFace(start0, span);

        if (logPhase)
        {
            Debug.Log($"[TM] Execute cursor={_cursor} asset={(asset ? asset.name : "NULL")} type={(asset ? asset.GetType().Name : "NULL")} rt.kind={rt.kind} rt.target={rt.target} span={span} start0={start0} rawSum={rawSum} resolvedSum={resolvedSum} maxFace={maxFace} playerFocus={(player ? player.focus : -999)}", this);
        }

        MarkDiceSpentInRange(start0, span);
        _board.ConsumeGroupAtAnchor_NoRefund(_cursor);
        RefreshAllViews();

        // ✅ ROUTE: BuffDebuff phải gọi overload SkillBuffDebuffSO, runtime Utility không làm gì
        if (asset is SkillBuffDebuffSO buffSkill)
        {
            var aoeTargets = SkillTargetRuleUtility.IsMultiTarget(buffSkill.target)
                ? TurnManagerCombatUtility.ResolveTargets(buffSkill.target, player, clicked, party, enemy)
                : null;

            if (logPhase)
                Debug.Log($"[TM] Branch=BuffDebuff -> {buffSkill.name} targetRule={buffSkill.target} delay={buffSkill.applyDelayTurns} effects={(buffSkill.effects != null ? buffSkill.effects.Count : 0)} applyAilment={buffSkill.applyAilment}", this);

            yield return executor.ExecuteSkill(buffSkill, player, clicked, resolvedSum, maxFace, skipCost: true, aoeTargets: aoeTargets);
        }
        else
        {
            var aoeTargets = TurnManagerCombatUtility.ResolveAoeTargets(rt, player, clicked, party, enemy);

            if (logPhase)
                Debug.Log($"[TM] Branch=Runtime (Attack/Guard/Legacy) -> rt.kind={rt.kind}", this);

            yield return executor.ExecuteSkill(rt, player, clicked, resolvedSum, skipCost: true, aoeTargets: aoeTargets);

            if (IsBasicStrikeRuntime(rt))
            {
                PassiveSystem ps = player != null ? player.GetComponent<PassiveSystem>() : null;
                if (ps != null)
                    ps.TryHandleBasicStrikeUse(diceRig, start0);
            }
        }

        if (TryHandleCombatVictory())
            yield break;

        _cursor = FindNextExecutableAnchor();
        SetPhase(Phase.Planning);
        RefreshAllViews();
        RefreshPlanningInteractivity();
    }

    private IEnumerator EnemyTurnRoutine()
    {
        SetPhase(Phase.EnemyTurn);
        SkillCombatState playerSkillState = player != null ? player.GetComponent<SkillCombatState>() : null;
        if (player != null && playerSkillState != null)
            playerSkillState.BeginEnemyTurn(player.hp);

        // ✅ Intent fade out when enemy turn starts (STS feel)
        TurnManagerViewUtility.FadeEnemyIntents(TurnManagerCombatUtility.ResolveAliveEnemiesSnapshot(party, enemy), 0.25f);

        // ✅ Delay trước enemy đầu tiên (để không đánh ngay lập tức khi vừa bấm Continue)
        if (delayBetweenEnemyAttacks > 0f)
            yield return new WaitForSeconds(delayBetweenEnemyAttacks);

        var enemies = TurnManagerCombatUtility.ResolveAliveEnemiesSnapshot(party, enemy);

        for (int i = 0; i < enemies.Count; i++)
        {
            var e = enemies[i];
            if (e == null || e.IsDead) continue;

            bool skipTurn = false;

            if (e.status != null)
            {
                int dot = e.status.OnTurnStarted(consumeFreezeToSkipTurn: true, out skipTurn);
                if (logPhase) Debug.Log(
                    $"[TM] EnemyTurnStart {e.name}: dot={dot} skip={skipTurn} bleed={e.status.bleedStacks} burnStacks={e.status.burnStacks} burnBatches={e.status.GetBurnBatches().Count} frozen={e.status.frozen}",
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

                                case SkillTargetRule.RowEnemies:
                                    aoe = TurnManagerCombatUtility.ResolveTargets(move.buffDebuffSkill.target, e, player, party, enemy);
                                    clicked = player;
                                    break;

                                case SkillTargetRule.AllEnemies:
                                    aoe = TurnManagerCombatUtility.ResolveTargets(move.buffDebuffSkill.target, e, player, party, enemy);
                                    clicked = (aoe != null && aoe.Count > 0) ? aoe[0] : player;
                                    break;

                                case SkillTargetRule.RowAllies:
                                    clicked = e;
                                    aoe = TurnManagerCombatUtility.ResolveTargets(move.buffDebuffSkill.target, e, clicked, party, enemy);
                                    break;

                                case SkillTargetRule.AllAllies:
                                    aoe = TurnManagerCombatUtility.ResolveTargets(move.buffDebuffSkill.target, e, e, party, enemy);
                                    clicked = e;
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
                            IReadOnlyList<CombatActor> aoe = null;
                            switch (move.damageSkill.target)
                            {
                                case SkillTargetRule.Self: target = e; break;
                                case SkillTargetRule.SingleEnemy: target = player; break;
                                case SkillTargetRule.SingleAlly: target = e; break; // damage hiếm khi dùng
                                case SkillTargetRule.RowEnemies:
                                case SkillTargetRule.AllEnemies:
                                    target = player;
                                    aoe = TurnManagerCombatUtility.ResolveTargets(move.damageSkill.target, e, target, party, enemy);
                                    break;
                                case SkillTargetRule.RowAllies:
                                case SkillTargetRule.AllAllies:
                                    target = e;
                                    aoe = TurnManagerCombatUtility.ResolveTargets(move.damageSkill.target, e, target, party, enemy);
                                    break;
                                default: target = player; break;
                            }

                            if (target != null)
                                yield return executor.ExecuteSkill(move.damageSkill, e, target, dieValue: 3, skipCost: true, aoeTargets: aoe);
                        }

                        // consume intent + tick cooldown turn (tối thiểu để cooldown hoạt động)
                        brain.ConsumeCurrentIntent();
                        brain.AdvanceTurnTick();
                        brain.DecideNextIntent(player);

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

        if (player != null)
        {
            PassiveSystem ps = player.GetComponent<PassiveSystem>();
            if (ps == null || !ps.ShouldRetainGuardAtEndOfTurn())
                player.guardPool = 0;
            if (playerSkillState != null)
                playerSkillState.EndEnemyTurn(player.hp);
        }
        TurnManagerCombatUtility.ClearAllStagger();

        yield return null;
    }



    private void BeginNewPlayerTurn()
    {
        SetPhase(Phase.Planning);
        SkillCombatState playerSkillState = player != null ? player.GetComponent<SkillCombatState>() : null;
        if (playerSkillState != null)
            playerSkillState.BeginPlayerTurn();
        TurnManagerLifecycleUtility.BeginPlayerTurnStatusesAndFocus(player, logPhase, this);

        _spentDiceThisTurn.Clear();
        _board.Reset();

        if (diceRig != null)
        {
            RestoreBaselineSlots();
            ApplySlotCollapseToRig();
            diceRig.BeginNewTurn();
        }

        RefreshAllViews();
        RefreshPlanningInteractivity();
        LockPlanningUI(false);

        // ✅ Ensure enemy has intent for THIS upcoming enemy turn (STS style)
        TurnManagerViewUtility.EnsureEnemyIntentsNow(TurnManagerCombatUtility.ResolveAliveEnemiesSnapshot(party, enemy), player);
    }




    // -------- helpers --------
    private bool IsSlotActive0(int i0) => diceRig == null || diceRig.IsSlotActive(i0);

    private bool IsSlotAssignable0(int i0)
    {
        if (!IsSlotActive0(i0))
            return false;
        if (diceRig == null || diceRig.slots == null || i0 < 0 || i0 >= diceRig.slots.Length || diceRig.slots[i0] == null)
            return true;

        DiceSpinnerGeneric die = diceRig.slots[i0].dice;
        return die == null || !_spentDiceThisTurn.Contains(die);
    }

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
        => TurnManagerViewUtility.RefreshAllPreviews(_drops, _board);

    private void UpdateAllIconsDim()
        => TurnManagerViewUtility.UpdateAllIconsDim(_board, this);

    private void UpdateAllDiceDim()
        => TurnManagerViewUtility.UpdateAllDiceDim(this);

    private void RefreshAllViews()
    {
        RefreshAllPreviews();
        UpdateAllIconsDim();
        UpdateAllDiceDim();
    }


    private void OnDiceRolled()
    {
        DiceCombatEnchantRuntimeUtility.ResolveOnRollFaceEnchants(diceRig, player, party, enemy);

        PassiveSystem ps = player != null ? player.GetComponent<PassiveSystem>() : null;
        if (ps != null)
            ps.OnDiceRolled(player, diceRig);

        _board.RecalculateRuntimesAndRebalance(player, diceRig);
        RefreshAllViews();
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
        return TurnManagerTargetingUtility.IsValidTargetForPendingSkill(rt, clicked, player, party, enemy);
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
        => TurnManagerLifecycleUtility.EndPlayerTurnTickStatusesAndPassives(player, logPhase, this);

    private void ApplyPlayerSlotDebuffs()
        => TurnManagerLifecycleUtility.ApplyPlayerSlotDebuffs(diceRig, player, _baseSlotActive, ref _slotCollapseKeepIndex, logPhase, this);

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

    private bool TryValidateTargetForPendingSkill(CombatActor clicked, out string reason)
    {
        var rt = _board.GetAnchorRuntime(_cursor);
        return TurnManagerTargetingUtility.TryValidateTargetForPendingSkill(rt, clicked, player, party, enemy, out reason);
    }

    private int FindNextExecutableAnchor()
    {
        for (int i = 0; i < 3; i++)
        {
            if (_board.IsAnchorSlot(i))
                return i;
        }

        return -1;
    }

    private ElementType GetResolvedDiceElement(SkillRuntime rt, ScriptableObject asset)
    {
        if (rt != null && rt.kind == SkillKind.Attack)
            return rt.element;

        return TurnManagerCombatUtility.GetResolvedDiceElement(rt, asset);
    }

    private int ComputeResolvedDieSum(int start0, int span, ElementType skillElement)
        => TurnManagerCombatUtility.ComputeResolvedDieSum(diceRig, player, start0, span, skillElement);

    private int ComputeMaxFace(int start0, int span)
        => TurnManagerCombatUtility.ComputeMaxFace(diceRig, start0, span);

    private static int GetSkillSpan(ScriptableObject activeSkill)
    {
        switch (activeSkill)
        {
            case SkillDamageSO damage:
                return Mathf.Clamp(damage.slotsRequired, 1, 3);
            case SkillBuffDebuffSO buffDebuff:
                return Mathf.Clamp(buffDebuff.slotsRequired, 1, 3);
            default:
                return 0;
        }
    }

    private void MarkDiceSpentInRange(int start0, int span)
    {
        if (diceRig == null || diceRig.slots == null)
            return;

        for (int i = start0; i < start0 + span && i < diceRig.slots.Length; i++)
        {
            DiceSpinnerGeneric die = diceRig.slots[i] != null ? diceRig.slots[i].dice : null;
            if (die != null)
            {
                _spentDiceThisTurn.Add(die);
                DiceCombatEnchantRuntimeUtility.MarkDieUsedInCombat(die);
            }
        }
    }

    private bool TryHandleCombatVictory()
    {
        if (_victoryResolvedThisCombat)
            return true;

        if (!IsCombatWon())
            return false;

        _victoryResolvedThisCombat = true;

        if (diceRig != null)
            DiceCombatEnchantRuntimeUtility.ApplyVictoryWholeDieEffects(diceRig);

        SetPhase(Phase.Planning);
        RefreshAllViews();
        RefreshPlanningInteractivity();
        return true;
    }

    private bool IsCombatWon()
    {
        return TurnManagerCombatUtility.ResolveAliveEnemiesSnapshot(party, enemy).Count <= 0;
    }




    private void RestoreBaselineSlots()
        => TurnManagerLifecycleUtility.RestoreBaselineSlots(diceRig, _baseSlotActive, ref _slotCollapseKeepIndex);

    private void ApplySlotCollapseToRig()
        => TurnManagerLifecycleUtility.ApplySlotCollapseToRig(diceRig, player, ref _slotCollapseKeepIndex, logPhase, this);

    private void EnsureAllEnemyIntentsNow()
        => TurnManagerViewUtility.EnsureEnemyIntentsNow(TurnManagerCombatUtility.ResolveAliveEnemiesSnapshot(party, enemy), player);

    private static bool IsBasicStrikeRuntime(SkillRuntime rt)
    {
        if (rt == null)
            return false;
        return rt.coreAction == CoreAction.BasicStrike;
    }

    public bool IsDieSpentThisTurn(DiceSpinnerGeneric die)
        => die != null && _spentDiceThisTurn.Contains(die);

    private bool TryResolvePrototypeCastPlacement(ScriptableObject activeSkill, out int start0, out int anchor0, bool commit)
    {
        start0 = -1;
        anchor0 = -1;

        int span = GetSkillSpan(activeSkill);
        if (span <= 0)
            return false;

        if (!_board.TryFindEmptyPlacement(span, IsSlotAssignable0, out start0, out anchor0))
            return false;
        if (!AreSlotsActiveInRange(start0, span))
            return false;
        if (diceRig != null && !diceRig.CanFitAtDrop(start0, span))
            return false;

        var snap = _board.Capture(player);
        _board.PlaceGroup(start0, anchor0, span, activeSkill);
        bool ok = _board.RecalculateRuntimesAndRebalance(player, diceRig);
        if (!ok)
        {
            _board.Restore(snap, player);
            return false;
        }

        if (!commit)
            _board.Restore(snap, player);

        return true;
    }

}
