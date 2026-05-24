using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dice row UI rendered directly from the current dice order.
/// Source of truth is the ordered dice list; UI only mirrors and reorders that list.
/// </summary>
public partial class DiceEquipUIManager : MonoBehaviour
{
    [Header("Legacy Wiring (unused by row layout)")]
    public RectTransform[] equipSlotAnchors = new RectTransform[3];
    public RectTransform dragLayer;

    [Header("Runtime Links")]
    public DiceSlotRig diceRig;
    public RunInventoryManager runInventory;
    public TurnManager turnManager;

    [Header("Linked Combat Slots (optional)")]
    public RectTransform[] linkedCombatSlotAnchors = new RectTransform[3];

    [Header("Linked World Dice Slots (optional)")]
    public bool mirrorDiceRigSlotsWithLiveUI = true;
    public Camera worldFollowCamera;

    [Header("Behavior")]
    public bool lockWhenCombatManagerExists = true;
    public bool enableGroupedSkillDiceReorder = true;

    [Header("Balatro Row Layout")]
    public RectTransform layoutContainer;
    public float spacing = 250f;
    public float rowY = 0f;
    public Vector2 diceUiSize = new Vector2(100f, 100f);
    public bool autoCreateMissingUi = true;

    [Header("Tween")]
    public float itemSnapDuration = 0.18f;
    public Ease itemEase = Ease.OutBack;

    [Header("Selection")]
    public bool keepSelectionOnRepeatedClick = true;

    public DiceDraggableUI[] equipped = new DiceDraggableUI[RunInventoryManager.EQUIPPED_DICE_COUNT];
    public bool WasDropConsumedThisFrame { get; private set; }

    private readonly List<DiceSpinnerGeneric> _orderedDice = new List<DiceSpinnerGeneric>(RunInventoryManager.EQUIPPED_DICE_COUNT);
    private readonly List<DiceDraggableUI> _orderedUi = new List<DiceDraggableUI>(RunInventoryManager.EQUIPPED_DICE_COUNT);
    private readonly Dictionary<DiceSpinnerGeneric, DiceDraggableUI> _uiByDice = new Dictionary<DiceSpinnerGeneric, DiceDraggableUI>();
    private readonly DiceDraggableUI[] _worldSlotOwners = new DiceDraggableUI[RunInventoryManager.EQUIPPED_DICE_COUNT];
    private readonly Transform[] _worldSlotRoots = new Transform[RunInventoryManager.EQUIPPED_DICE_COUNT];

    private Canvas _rootCanvas;
    private RectTransform _container;
    private readonly List<DiceDraggableUI> _selectedDice = new List<DiceDraggableUI>(RunInventoryManager.EQUIPPED_DICE_COUNT);
    private DiceDraggableUI _draggingDice;
    private int _dragSourceIndex = -1;
    private int _previewInsertIndex = -1;
    private bool _suppressInventoryRefresh;
    public event System.Action SelectionChanged;
    private static readonly Color[] DefaultDiceUiColors =
    {
        new Color(1f, 0.514151f, 0.514151f, 1f),
        new Color(0.77953416f, 0.5613208f, 1f, 1f),
        new Color(0.6821208f, 1f, 0.5801887f, 1f),
        new Color(0.45f, 0.83f, 1f, 1f),
        new Color(1f, 0.82f, 0.45f, 1f),
    };

    private void Awake()
    {
        _rootCanvas = GetComponentInParent<Canvas>();
        _container = layoutContainer != null ? layoutContainer : transform as RectTransform;

        if (runInventory == null)
            runInventory = GetComponentInParent<RunInventoryManager>(true);

        if (diceRig == null && runInventory != null)
            diceRig = runInventory.DiceRig;

        RefreshTurnManagerRef();
        CacheDiceUi();
        RefreshFromAuthoritativeOrder(true);
    }

    private void OnEnable()
    {
        if (runInventory != null)
            runInventory.InventoryChanged += HandleInventoryChanged;
    }

    private void Start()
    {
        RefreshFromAuthoritativeOrder(true);
    }

    private void OnDisable()
    {
        if (runInventory != null)
            runInventory.InventoryChanged -= HandleInventoryChanged;
    }

    private void LateUpdate()
    {
        RefreshCombatDiceRuntimeState(false);
        SyncWorldSlotRootsToUI(false);
        WasDropConsumedThisFrame = false;
    }

    public bool CanInteract()
    {
        if (!lockWhenCombatManagerExists)
            return true;

        RefreshTurnManagerRef();
        if (turnManager == null || !turnManager.isActiveAndEnabled)
            return true;

        return turnManager.IsPlanning;
    }

    public void Register(DiceDraggableUI dice)
    {
        if (dice == null)
            return;

        dice.manager = this;
        dice.tweenDuration = itemSnapDuration;
    }

    [ContextMenu("Rebuild From Children")]
    public void RebuildFromChildren()
    {
        CacheDiceUi();
        RefreshFromAuthoritativeOrder(true);
    }

    [ContextMenu("Remove Rightmost Dice")]
    public void RemoveRightmostDice()
    {
        if (_orderedDice.Count == 0)
            return;

        List<DiceSpinnerGeneric> previous = new List<DiceSpinnerGeneric>(_orderedDice);
        List<DiceSpinnerGeneric> reordered = new List<DiceSpinnerGeneric>(_orderedDice);
        reordered.RemoveAt(reordered.Count - 1);
        if (TryApplyOrderedDice(reordered, previous, notifyInventoryChanged: !ShouldUseDiceRigOrderForRefresh()))
            RefreshVisualState(false);
    }

    public void RemoveLastDice()
    {
        RemoveRightmostDice();
    }

    public void NotifyBeginDrag(DiceDraggableUI dice)
    {
        if (dice == null)
            return;

        WasDropConsumedThisFrame = false;
        _draggingDice = dice;
        _dragSourceIndex = _orderedUi.IndexOf(dice);
        _previewInsertIndex = _dragSourceIndex;

        if (_selectedDice.Contains(dice))
            RemoveSelectedDice(dice, true);
    }

    public void NotifyDrag(DiceDraggableUI dice, Vector2 screenPosition, Camera eventCamera)
    {
        if (dice == null || dice != _draggingDice || _dragSourceIndex < 0)
            return;

        int nextInsertIndex = GetInsertIndexFromScreenPosition(screenPosition, eventCamera);
        if (nextInsertIndex == _previewInsertIndex)
            return;

        _previewInsertIndex = nextInsertIndex;
        RefreshRowLayout(false);
        SyncWorldSlotRootsToUI(false);
    }

    public void NotifyEndDrag(DiceDraggableUI dice, Vector2 screenPosition, Camera eventCamera)
    {
        WasDropConsumedThisFrame = true;
        if (dice == null || dice != _draggingDice)
        {
            HandleInvalidDrop(dice);
            return;
        }

        CommitDrag(GetInsertIndexFromScreenPosition(screenPosition, eventCamera));
    }

    public void HandleDropToEquipSlot(DiceDraggableUI dice, int slotIndex)
    {
        WasDropConsumedThisFrame = true;
        if (dice == null || dice != _draggingDice)
            return;

        CommitDrag(Mathf.Clamp(slotIndex, 0, Mathf.Max(0, _orderedUi.Count - 1)));
    }

    public void HandleInvalidDrop(DiceDraggableUI dice)
    {
        if (dice != null)
            dice.ReturnToCachedHome();

        ClearDragState();
        RefreshRowLayout(false);
        SyncWorldSlotRootsToUI(false);
    }

    public void HandleDiceClicked(DiceDraggableUI dice)
    {
        if (dice == null)
            return;

        if (_selectedDice.Contains(dice))
        {
            ToggleSelectedDice(dice);
            return;
        }

        ToggleSelectedDice(dice);
    }

    public void HandleDiceBeginDrag(DiceDraggableUI dice)
    {
        if (dice != null && _selectedDice.Contains(dice))
            RemoveSelectedDice(dice, true);
    }

    public bool TryMovePlannedTwoSlotGroup(int anchorLane1Based, Vector2 screenPosition, Camera eventCamera)
    {
        if (!enableGroupedSkillDiceReorder || !CanInteract())
            return false;

        RefreshTurnManagerRef();
        if (turnManager == null)
            return false;

        if (!turnManager.TryGetPlannedGroupAtLane(anchorLane1Based, out int anchor0, out int start0, out int span))
            return false;
        if (span != 2 || anchor0 != start0)
            return false;
        if (start0 < 0 || start0 + 1 >= _orderedDice.Count)
            return false;

        int targetStart = GetClosestTwoSlotStartFromScreenPosition(screenPosition, eventCamera);
        if (targetStart == start0)
            return true;

        List<DiceSpinnerGeneric> previous = new List<DiceSpinnerGeneric>(_orderedDice);
        List<DiceSpinnerGeneric> reordered = new List<DiceSpinnerGeneric>(_orderedDice);
        DiceSpinnerGeneric first = reordered[start0];
        DiceSpinnerGeneric second = reordered[start0 + 1];
        reordered.RemoveAt(start0 + 1);
        reordered.RemoveAt(start0);
        reordered.Insert(targetStart, second);
        reordered.Insert(targetStart, first);

        if (!TryApplyOrderedDice(reordered, previous, notifyInventoryChanged: !ShouldUseDiceRigOrderForRefresh()))
            return false;

        RefreshVisualState(false);
        return true;
    }

    public void SyncOutputs()
    {
        SyncOutputs(notifyInventoryChanged: !ShouldUseDiceRigOrderForRefresh());
    }


    private void CommitDrag(int insertIndex)
    {
        if (_draggingDice == null || _dragSourceIndex < 0)
        {
            ClearDragState();
            return;
        }

        List<DiceSpinnerGeneric> previous = new List<DiceSpinnerGeneric>(_orderedDice);
        insertIndex = Mathf.Clamp(insertIndex, 0, Mathf.Max(0, _orderedDice.Count - 1));

        DiceSpinnerGeneric dragged = _draggingDice.dice;
        _orderedDice.RemoveAt(_dragSourceIndex);
        _orderedDice.Insert(insertIndex, dragged);

        if (!TryApplyOrderedDice(_orderedDice, previous, notifyInventoryChanged: !ShouldUseDiceRigOrderForRefresh()))
        {
            _orderedDice.Clear();
            _orderedDice.AddRange(previous);
            RebuildUiOrderFromDiceList();
            UpdateEquippedArray();
        }

        ClearDragState();
        RefreshVisualState(false);
    }

    private bool TryApplyOrderedDice(List<DiceSpinnerGeneric> nextOrder, List<DiceSpinnerGeneric> previousOrder, bool notifyInventoryChanged)
    {
        List<DiceSpinnerGeneric> nextSnapshot = new List<DiceSpinnerGeneric>(nextOrder);
        List<DiceSpinnerGeneric> previousSnapshot = new List<DiceSpinnerGeneric>(previousOrder);

        _orderedDice.Clear();
        _orderedDice.AddRange(nextSnapshot);
        RebuildUiOrderFromDiceList();
        UpdateEquippedArray();
        SyncOutputs(notifyInventoryChanged);

        if (!CommitPlanningPermutation(previousSnapshot, _orderedDice))
        {
            _orderedDice.Clear();
            _orderedDice.AddRange(previousSnapshot);
            RebuildUiOrderFromDiceList();
            UpdateEquippedArray();
            SyncOutputs(notifyInventoryChanged);
            return false;
        }

        return true;
    }

    private bool CommitPlanningPermutation(List<DiceSpinnerGeneric> previousOrder, List<DiceSpinnerGeneric> nextOrder)
    {
        RefreshTurnManagerRef();
        if (turnManager == null || !turnManager.IsPlanning)
            return true;

        int[] permutation = BuildPermutation(previousOrder, nextOrder, RunInventoryManager.EQUIPPED_DICE_COUNT);
        return turnManager.CommitDiceLaneReorder(permutation);
    }

    private void SyncOutputs(bool notifyInventoryChanged)
    {
        DiceSpinnerGeneric[] assets = new DiceSpinnerGeneric[RunInventoryManager.EQUIPPED_DICE_COUNT];
        for (int i = 0; i < assets.Length && i < _orderedDice.Count; i++)
            assets[i] = _orderedDice[i];

        if (runInventory != null)
        {
            _suppressInventoryRefresh = true;
            try
            {
                runInventory.SetDiceLayout(assets, notifyChanged: notifyInventoryChanged);
            }
            finally
            {
                _suppressInventoryRefresh = false;
            }
        }

        if (diceRig != null)
        {
            diceRig.ApplyDiceLayout(assets);

            for (int i = 0; i < assets.Length; i++)
            {
                diceRig.SetSlotActive(i, assets[i] != null);
            }

            DiceEquipWorldSyncUtility.RefreshDiceRigRollInfosAfterReorder(diceRig);
        }
    }

    private void SyncWorldSlotRootsToUI(bool instant)
    {
        if (!mirrorDiceRigSlotsWithLiveUI || diceRig == null)
            return;

        Camera uiCamera = DiceEquipWorldSyncUtility.GetUICamera(_rootCanvas);
        Camera worldCameraToUse = DiceEquipWorldSyncUtility.GetWorldFollowCamera(worldFollowCamera, uiCamera);
        DiceEquipWorldSyncUtility.SyncWorldSlotRootsToUI(
            mirrorDiceRigSlotsWithLiveUI,
            diceRig,
            _worldSlotOwners,
            _worldSlotRoots,
            instant,
            uiCamera,
            worldCameraToUse);
    }

    private void RefreshTurnManagerRef()
    {
        if (turnManager == null)
            turnManager = FindObjectOfType<TurnManager>(true);
    }

    private void HandleInventoryChanged()
    {
        if (_suppressInventoryRefresh || _draggingDice != null)
            return;

        RefreshFromAuthoritativeOrder(false);
    }

    private bool ShouldUseDiceRigOrderForRefresh()
    {
        RefreshTurnManagerRef();
        if (turnManager == null || !turnManager.isActiveAndEnabled)
            return false;

        return turnManager.phase == TurnManager.Phase.Planning ||
               turnManager.phase == TurnManager.Phase.AwaitTarget ||
               turnManager.phase == TurnManager.Phase.Executing;
    }

    private void ClearDragState()
    {
        _draggingDice = null;
        _dragSourceIndex = -1;
        _previewInsertIndex = -1;
    }

    private RectTransform GetLayoutContainer()
    {
        if (layoutContainer != null && _container != layoutContainer)
            _container = layoutContainer;

        if (_container == null)
            _container = transform as RectTransform;

        return _container;
    }

    private static int[] BuildPermutation(List<DiceSpinnerGeneric> previousOrder, List<DiceSpinnerGeneric> nextOrder, int slotCount)
    {
        int[] permutation = new int[slotCount];
        DiceSpinnerGeneric[] previous = new DiceSpinnerGeneric[slotCount];
        DiceSpinnerGeneric[] next = new DiceSpinnerGeneric[slotCount];

        for (int i = 0; i < slotCount; i++)
        {
            permutation[i] = i;
            previous[i] = i < previousOrder.Count ? previousOrder[i] : null;
            next[i] = i < nextOrder.Count ? nextOrder[i] : null;
        }

        for (int newIndex = 0; newIndex < slotCount; newIndex++)
        {
            DiceSpinnerGeneric target = next[newIndex];
            if (target == null)
                continue;

            for (int oldIndex = 0; oldIndex < slotCount; oldIndex++)
            {
                if (previous[oldIndex] == target)
                {
                    permutation[newIndex] = oldIndex;
                    break;
                }
            }
        }

        return permutation;
    }

    public static Color GetDefaultDiceUiColor(int index)
    {
        if (DefaultDiceUiColors.Length == 0)
            return Color.white;

        index = Mathf.Abs(index);
        return DefaultDiceUiColors[index % DefaultDiceUiColors.Length];
    }
}

