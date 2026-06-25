using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public partial class TurnManager : MonoBehaviour
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
    [Header("Phase Button UI")]
    [SerializeField] private Button continueButton;
    [SerializeField] private TMP_Text continueButtonLabel;

    [Tooltip("If TRUE, disables planning UI until dice have finished rolling.\n" +
             "If your Roll button lives inside these CanvasGroups, keep this FALSE or move the Roll button out.")]
    public bool lockPlanningUIUntilRolled = false;

    public enum Phase { Planning, AwaitTarget, Executing, EnemyTurn }
    public Phase phase = Phase.Planning;

    [Header("Enemy Turn")]
    public float delayBetweenEnemyAttacks = 0.2f;

    [Header("Player Roll")]
    [Tooltip("Automatically rolls dice when the player turn begins.")]
    public bool autoRollOnPlayerTurnStart = true;

    [Header("Debug")]
    public bool logPhase = true;
    public KeyCode toggleLogKey = KeyCode.F10;

    // Player can keep adjusting planning/reorder/select during the whole planning phase.
    public bool IsPlanning => phase == Phase.Planning;
    public bool CanInteractWithSkills => phase == Phase.Planning && !IsSkillInteractionLockedForCurrentRollWindow() && !ArePlayerCommandsLocked && !_endTurnQueued;
    public bool ArePlayerCommandsLocked => _externalPlayerInteractionLock || _defeatResolvedThisCombat || player == null || player.IsDead;
    public bool IsDiceReorderLocked => _diceReorderLocked;
    public event Action CombatVictoryResolved;

    private readonly ActionSlotDrop[] _drops = new ActionSlotDrop[3];
    // --- Slot Collapse support ---
    private readonly bool[] _baseSlotActive = new bool[3];
    private int _slotCollapseKeepIndex = -1;

    private readonly SkillPlanBoard _board = new SkillPlanBoard();
    private readonly CombatActorRuntimeContext _playerContext = new CombatActorRuntimeContext();
    private readonly EnemyTurnCoordinator _enemyTurnCoordinator = new EnemyTurnCoordinator();
    private readonly Queue<TurnManagerQueuedPlayerCommand> _queuedPlayerCommands = new Queue<TurnManagerQueuedPlayerCommand>();
    private readonly List<DiceSpinnerGeneric> _usedSelectedDiceBuffer = new List<DiceSpinnerGeneric>(RunInventoryManager.EQUIPPED_DICE_COUNT);
    private int _cursor = 0;
    private readonly HashSet<DiceSpinnerGeneric> _spentDiceThisTurn = new HashSet<DiceSpinnerGeneric>();
    private readonly HashSet<DiceSpinnerGeneric> _pendingUsedVisualDiceThisTurn = new HashSet<DiceSpinnerGeneric>();
    private bool _victoryResolvedThisCombat;
    private bool _defeatResolvedThisCombat;
    private bool _externalPlayerInteractionLock;
    private bool _diceReorderLocked;
    private bool _isProcessingQueuedPlayerCommands;
    private bool _endTurnQueued;
    private bool _continueButtonLookupAttempted;
    private Coroutine _autoRollCoroutine;
    private DiceEquipUIManager _diceEquipUiManager;

    void Awake()
    {
        TurnManagerRegistry.Register(this);
    }

    void Start()
    {
        _board.Reset();

        if (party != null)
        {
            party.EnsureSpawned();
            if (party.Player != null) player = party.Player;
        }

        _playerContext.Bind(player);

        if (diceRig != null)
        {
            diceRig.onAllDiceRolled += OnDiceRolled;

            // ? hook dice debuff layer
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
        _defeatResolvedThisCombat = false;

        if (logPhase)
        {
            Debug.Log($"[TM] Start refs: player={(player ? player.name : "NULL")} enemy={(enemy ? enemy.name : "NULL")} party={(party ? party.name : "NULL")} diceRig={(diceRig ? diceRig.name : "NULL")} executor={(executor ? executor.name : "NULL")}", this);
            Debug.Log($"[TM] player.status={(player && player.status ? player.status.name : "NULL")}", this);
        }

        RefreshPlanningInteractivity();
        RefreshAllPreviews();
        UpdateAllIconsDim();
        UpdateAllDiceDim();
        ResolveContinueButtonUi();
        RefreshContinueButtonUi();
        BeginNewPlayerTurn();
        if (diceRig != null)
            diceRig.ShowRandomPresentationFaces();
        EnsureAllEnemyIntentsNow();
    }


    void OnDestroy()
    {
        TurnManagerRegistry.Unregister(this);

        if (diceRig != null)
        {
            diceRig.onAllDiceRolled -= OnDiceRolled;
            diceRig.onComputeAllDiceDelta -= ComputeAllDiceDelta;
        }
    }

    public void BeginPrototypeCombat()
    {
        if (party != null && party.Player != null)
            player = party.Player;

        _victoryResolvedThisCombat = false;
        _defeatResolvedThisCombat = false;
        _externalPlayerInteractionLock = false;
        _isProcessingQueuedPlayerCommands = false;
        _endTurnQueued = false;
        _spentDiceThisTurn.Clear();
        _pendingUsedVisualDiceThisTurn.Clear();
        _queuedPlayerCommands.Clear();
        _board.Reset();
        _cursor = 0;

        _playerContext.Bind(player);
        if (diceRig != null)
            DiceCombatEnchantRuntimeUtility.BeginCombat(diceRig);

        if (executor != null)
            executor.ResetPlayerCastVisualState();

        BeginNewPlayerTurn();
        if (diceRig != null)
            diceRig.ShowRandomPresentationFaces();
        EnsureAllEnemyIntentsNow();
    }


    void Update()
    {
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
        if (ArePlayerCommandsLocked) return;
        if (diceRig.HasRolledThisTurn || diceRig.IsRolling) return;

        diceRig.RollOnceSequential();
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

    public bool IsSkillEquipped(SkillDamageSO skill) => _board.IsSkillEquipped(skill);
    public bool IsSkillEquipped(SkillBuffDebuffSO skill) => _board.IsSkillEquipped(skill);

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

    public bool TryGetPlannedSkillTooltipAtLane(int lane1Based, out ScriptableObject asset, out SkillRuntime runtime)
    {
        asset = null;
        runtime = null;

        if (!TryGetPlannedGroupAtLane(lane1Based, out int anchor0, out _, out _))
            return false;

        asset = _board.GetCellSkillAsset(anchor0);
        runtime = _board.GetAnchorRuntime(anchor0);
        return asset != null;
    }

    public void RefreshPlanningAfterDiceValueReorder()
    {
        if (diceRig != null)
            diceRig.RefreshRollInfoCache();

        _board.RecalculateRuntimesAndRebalance(player, diceRig);
        RefreshAllViews();
        RefreshPlanningInteractivity();
    }

    public void RefreshPlanningAfterDiceValueReorder(DiceSpinnerGeneric changedDie, bool triggersRollPassives = true)
    {
        RefreshPlanningAfterDiceValueReorder(changedDie != null ? new[] { changedDie } : null, triggersRollPassives);
    }

    public void RefreshPlanningAfterDiceValueReorder(System.Collections.Generic.IReadOnlyList<DiceSpinnerGeneric> changedDice, bool triggersRollPassives = true)
    {
        if (diceRig != null)
            diceRig.RefreshRollInfoCache();

        _playerContext.Bind(player);
        if (triggersRollPassives && _playerContext.PassiveSystem != null)
        {
            if (changedDice != null)
                _playerContext.PassiveSystem.OnDiceRolled(player, diceRig, changedDice);
        }

        RefreshPlanningAfterDiceValueReorder();
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
        return die == null || (!_spentDiceThisTurn.Contains(die) && die.IsCurrentFaceUsable());
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
        SkillTooltipUI.RefreshCurrent();
    }


    private bool IsValidTargetForPendingSkill(CombatActor clicked)
    {
        var rt = _board.GetAnchorRuntime(_cursor);
        return TurnManagerTargetingUtility.IsValidTargetForPendingSkill(rt, clicked, player, party, enemy);
    }


    private void SetPhase(Phase newPhase)
    {
        if (phase == newPhase) return;

        phase = newPhase;
        RefreshContinueButtonUi();

        if (logPhase)
            Debug.Log($"[TurnManager] Phase => {phase}", this);
    }

    private bool IsSkillInteractionLockedForCurrentRollWindow()
    {
        if (diceRig == null)
            return false;

        return !diceRig.HasRolledThisTurn || diceRig.IsRolling;
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

    private void MarkDiceSpentInRange(int start0, int span, int paymentMask = -1)
    {
        if (diceRig == null || diceRig.slots == null)
            return;

        for (int i = start0; i < start0 + span && i < diceRig.slots.Length; i++)
        {
            if (paymentMask >= 0 && (paymentMask & (1 << i)) == 0)
                continue;
            DiceSpinnerGeneric die = diceRig.slots[i] != null ? diceRig.slots[i].dice : null;
            if (die != null)
            {
                _spentDiceThisTurn.Add(die);
                _pendingUsedVisualDiceThisTurn.Add(die);
                DiceCombatEnchantRuntimeUtility.MarkDieUsedInCombat(die);
            }
        }
    }

    private void ResolveContinueButtonUi()
    {
        if (_continueButtonLookupAttempted && continueButton == null)
            return;

        if (continueButton == null)
        {
            _continueButtonLookupAttempted = true;
#if UNITY_2023_1_OR_NEWER
            Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            Button[] buttons = FindObjectsOfType<Button>(true);
#endif
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button == null)
                    continue;

                var onClick = button.onClick;
                int count = onClick.GetPersistentEventCount();
                for (int j = 0; j < count; j++)
                {
                    if (onClick.GetPersistentTarget(j) == this && onClick.GetPersistentMethodName(j) == nameof(OnContinue))
                    {
                        continueButton = button;
                        break;
                    }
                }

                if (continueButton != null)
                    break;
            }
        }

        if (continueButtonLabel == null && continueButton != null)
            continueButtonLabel = continueButton.GetComponentInChildren<TMP_Text>(true);
    }

    private void RefreshContinueButtonUi()
    {
        ResolveContinueButtonUi();

        if (continueButtonLabel != null)
        {
            continueButtonLabel.text = (phase == Phase.EnemyTurn || _endTurnQueued) ? "Enemy Phase" : "End Phase";
        }

        if (continueButton != null)
        {
            continueButton.interactable = IsPlanning && !ArePlayerCommandsLocked && !_endTurnQueued;
        }
    }




    private void EnsureAllEnemyIntentsNow()
        => TurnManagerViewUtility.EnsureEnemyIntentsNow(TurnManagerCombatUtility.ResolveAliveEnemiesSnapshot(party, enemy), player);

}

internal static class TurnManagerRegistry
{
    private static TurnManager _instance;
    private static bool _initializedFromScene;

    public static void Register(TurnManager turnManager)
    {
        if (turnManager == null)
            return;

        _instance = turnManager;
    }

    public static void Unregister(TurnManager turnManager)
    {
        if (turnManager == null || _instance != turnManager)
            return;

        _instance = null;
        _initializedFromScene = false;
    }

    public static TurnManager Get()
    {
        if (_instance != null)
            return _instance;

        EnsureInitializedFromScene();
        return _instance;
    }

    private static void EnsureInitializedFromScene()
    {
        if (_initializedFromScene)
            return;

        _initializedFromScene = true;
#if UNITY_2023_1_OR_NEWER
        _instance = UnityEngine.Object.FindFirstObjectByType<TurnManager>(FindObjectsInactive.Include);
#else
        _instance = UnityEngine.Object.FindObjectOfType<TurnManager>(true);
#endif
    }
}
