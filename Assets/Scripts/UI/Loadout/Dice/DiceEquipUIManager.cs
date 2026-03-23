using DG.Tweening;
using UnityEngine;

/// <summary>
/// 3-slot dice loadout UI.
/// - No overflow.
/// - Reorder / swap only.
/// - Locked whenever an active TurnManager exists in the scene.
/// - Adaptive 'joker row' layout:
///   1 equipped -> center
///   2 equipped -> left/right
///   3 equipped -> left/center/right
/// - Optional linked combat SlotsPanel row that mirrors the same adaptive X layout.
/// - Optional linked world DiceSlotRig slot roots that follow the paired DiceUi live.
/// </summary>
public class DiceEquipUIManager : MonoBehaviour
{
    [Header("Wiring")]
    public RectTransform[] equipSlotAnchors = new RectTransform[3];
    public RectTransform dragLayer;
    public DiceSlotRig diceRig;
    public RunInventoryManager runInventory;
    public TurnManager turnManager;

    [Header("Linked Combat SlotsPanel (optional)")]
    [Tooltip("Assign Canvas/SlotsPanel/Slot1..3 here so combat slots move with dice layout.")]
    public RectTransform[] linkedCombatSlotAnchors = new RectTransform[3];
    [Tooltip("Mirror the adaptive X layout onto SlotsPanel/Slot1..3.")]
    public bool mirrorAdaptiveLayoutToCombatSlots = true;
    [Tooltip("Hide empty combat slot anchors together with empty dice anchors.")]
    public bool hideEmptyCombatSlotAnchors = true;
    [Tooltip("Optional extra X offset applied only to combat slots.")]
    public float combatSlotsXOffset = 0f;

    [Header("Linked World Dice Slots (optional)")]
    [Tooltip("If enabled, DiceSlotRig slotRoot objects follow the paired DiceUi live, including while dragging.")]
    public bool mirrorDiceRigSlotsWithLiveUI = true;
    [Tooltip("Camera used to convert DiceUi screen position into world position. Leave empty to use Canvas worldCamera or Camera.main.")]
    public Camera worldFollowCamera;

    [Header("Behavior")]
    [Tooltip("If true, any active TurnManager in the scene will lock dice dragging.")]
    public bool lockWhenCombatManagerExists = true;
    [Tooltip("If true, dragging a die inside a planned 2-slot skill only allows local swap, while dragging the centered skill icon can move the whole 2-die group.")]
    public bool enableGroupedSkillDiceReorder = true;

    [Header("Adaptive Layout")]
    [Tooltip("Auto-present like Balatro jokers: 1 center, 2 split, 3 spread.")]
    public bool useAdaptiveCenterLayout = true;
    [Tooltip("Half distance used when 2 items are equipped.")]
    public float pairHalfSpacing = 180f;
    [Tooltip("Side offset used when 3 items are equipped.")]
    public float trioSideOffset = 250f;
    [Tooltip("Local Y for the whole dice row.")]
    public float rowY = 0f;
    [Tooltip("Hide empty anchor objects so only occupied dice positions remain visible.")]
    public bool hideEmptyAnchors = true;

    [Header("Tween")]
    public float anchorTweenDuration = 0.22f;
    public float itemSnapDuration = 0.18f;
    public Ease anchorEase = Ease.OutCubic;
    public Ease itemEase = Ease.OutBack;

    [Header("Combat Slot Preview Tween")]
    public float combatSlotPreviewDuration = 0.18f;
    public Ease combatSlotPreviewEase = Ease.OutBack;

    public DiceDraggableUI[] equipped = new DiceDraggableUI[3];
    public bool WasDropConsumedThisFrame { get; private set; }

    private readonly float[] _combatSlotBaseY = new float[3];
    private readonly RectTransform[] _combatSlotIdentity = new RectTransform[3];
    private readonly DiceDraggableUI[] _displayOrderBuffer = new DiceDraggableUI[3];
    private readonly RectTransform[] _combatDisplayBuffer = new RectTransform[3];
    private readonly DiceDraggableUI[] _worldSlotOwners = new DiceDraggableUI[3];
    private readonly Transform[] _worldSlotRoots = new Transform[3];
    private bool _capturedCombatSlotY;

    private DiceDraggableUI _draggingDice;
    private RectTransform _draggingCombatSlot;
    private int _dragSourceIndex = -1;
    private int _previewInsertIndex = -1;
    private Canvas _rootCanvas;

    private void Awake()
    {
        _rootCanvas = GetComponentInParent<Canvas>();

        if (runInventory == null)
            runInventory = GetComponentInParent<RunInventoryManager>(true);

        if (diceRig == null && runInventory != null)
            diceRig = runInventory.DiceRig;

        RefreshTurnManagerRef();
        CaptureCombatSlotBaseY();
        RebindWorldSlotOwnersFromCurrentOrder();
    }

    private void Start()
    {
        ApplyAdaptiveLayout(true);
        RebindCombatSlotLaneIndices();
        RefreshAllSlots(true);
        SyncOutputs();
        SyncWorldSlotRootsToUI(true);
    }

    private void LateUpdate()
    {
        SyncWorldSlotRootsToUI(false);
        WasDropConsumedThisFrame = false;
    }

    public bool CanInteract()
    {
        if (!lockWhenCombatManagerExists) return true;

        RefreshTurnManagerRef();
        if (turnManager == null) return true;
        if (!turnManager.isActiveAndEnabled) return true;

        return turnManager.IsPlanning;
    }

    public void Register(DiceDraggableUI dice)
    {
        if (dice == null) return;
        dice.manager = this;
        dice.tweenDuration = itemSnapDuration;
    }

    [ContextMenu("Rebuild From Children")]
    public void RebuildFromChildren()
    {
        for (int i = 0; i < equipped.Length; i++)
            equipped[i] = null;

        for (int i = 0; i < equipSlotAnchors.Length; i++)
        {
            RectTransform anchor = equipSlotAnchors[i];
            if (anchor == null) continue;

            DiceDraggableUI dice = anchor.GetComponentInChildren<DiceDraggableUI>(true);
            if (dice == null) continue;

            Register(dice);
            equipped[i] = dice;
        }

        CompactEquipped();
        RebindWorldSlotOwnersFromCurrentOrder();
        ApplyAdaptiveLayout(true);
        RebindCombatSlotLaneIndices();
        RefreshAllSlots(true);
        SyncOutputs();
        SyncWorldSlotRootsToUI(true);
    }

    public void NotifyBeginDrag(DiceDraggableUI dice)
    {
        if (dice == null) return;

        _draggingDice = dice;
        _dragSourceIndex = GetEquippedIndex(dice);
        _previewInsertIndex = _dragSourceIndex;
        _draggingCombatSlot = IsDirectGroupDiceDrag(dice) ? null : GetCombatSlotAt(_dragSourceIndex);
        if (_draggingCombatSlot != null)
            RefreshCombatSlotPreview(true);
    }

    public void NotifyDrag(DiceDraggableUI dice, Vector2 screenPosition, Camera eventCamera)
    {
        if (dice == null || dice != _draggingDice) return;
        if (_dragSourceIndex < 0) return;

        if (IsDirectGroupDiceDrag(dice))
        {
            int groupedInsertIndex = GetDragInsertIndex(screenPosition, eventCamera);
            if (groupedInsertIndex != _previewInsertIndex)
            {
                _previewInsertIndex = groupedInsertIndex;
                RefreshAllSlots();
            }

            SyncWorldSlotRootsToUI(false);
            return;
        }

        int nextInsertIndex = GetDragInsertIndex(screenPosition, eventCamera);
        if (nextInsertIndex != _previewInsertIndex)
        {
            _previewInsertIndex = nextInsertIndex;
            RefreshAllSlots();
            RefreshCombatSlotPreview();
        }

        SyncWorldSlotRootsToUI(false);
    }

    public void NotifyEndDrag(DiceDraggableUI dice, Vector2 screenPosition, Camera eventCamera)
    {
        if (dice == null) return;

        WasDropConsumedThisFrame = true;

        if (dice != _draggingDice)
        {
            HandleInvalidDrop(dice);
            return;
        }

        if (_dragSourceIndex < 0)
        {
            HandleInvalidDrop(dice);
            return;
        }

        if (IsDirectGroupDiceDrag(dice))
        {
            FinalizeDirectGroupedDiceDrag(_previewInsertIndex >= 0 ? _previewInsertIndex : GetClosestEquipSlotIndexFromScreenPosition(screenPosition, eventCamera));
            return;
        }

        FinalizeDraggedDice(GetDragInsertIndex(screenPosition, eventCamera));
    }

    public void HandleDropToEquipSlot(DiceDraggableUI dice, int slotIndex)
    {
        WasDropConsumedThisFrame = true;
        if (!CanInteract() || dice == null) return;

        slotIndex = Mathf.Clamp(slotIndex, 0, 2);

        if (dice == _draggingDice && TryGetDirectDragAllowedRange(dice, out int minSlot, out int maxSlot))
        {
            if (slotIndex < minSlot || slotIndex > maxSlot)
            {
                HandleInvalidDrop(dice);
                return;
            }

            FinalizeDirectGroupedDiceDrag(slotIndex);
            return;
        }

        if (dice == _draggingDice && TryAdjustExternalGroupDropTarget(slotIndex, out int adjustedSlotIndex))
        {
            if (adjustedSlotIndex < 0)
            {
                HandleInvalidDrop(dice);
                return;
            }

            slotIndex = adjustedSlotIndex;
        }

        if (dice == _draggingDice && _dragSourceIndex >= 0)
        {
            FinalizeDraggedDice(slotIndex);
            return;
        }

        int fromSlot = GetEquippedIndex(dice);
        DiceDraggableUI existing = equipped[slotIndex];

        if (fromSlot == slotIndex)
        {
            ApplyAdaptiveLayout();
            SnapToEquip(slotIndex, dice);
            SyncWorldSlotRootsToUI(false);
            return;
        }

        if (fromSlot < 0 && existing != null)
        {
            dice.ReturnToCachedHome();
            ApplyAdaptiveLayout();
            RefreshAllSlots();
            SyncWorldSlotRootsToUI(false);
            return;
        }

        if (fromSlot >= 0)
            equipped[fromSlot] = null;

        if (existing != null && fromSlot >= 0)
            equipped[fromSlot] = existing;

        equipped[slotIndex] = dice;

        if (linkedCombatSlotAnchors != null && fromSlot >= 0 && fromSlot < linkedCombatSlotAnchors.Length && slotIndex < linkedCombatSlotAnchors.Length)
            linkedCombatSlotAnchors = DiceEquipStateUtility.MoveCombatSlot(linkedCombatSlotAnchors, fromSlot, slotIndex);

        CompactEquipped();
        ApplyAdaptiveLayout();
        RebindCombatSlotLaneIndices();
        RefreshAllSlots();
        SyncOutputs();
        SyncWorldSlotRootsToUI(false);
    }

    public void HandleInvalidDrop(DiceDraggableUI dice)
    {
        if (dice == null) return;

        ClearDragState();
        dice.ReturnToCachedHome();
        ApplyAdaptiveLayout();
        RefreshAllSlots();
        SyncWorldSlotRootsToUI(false);
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
        if (span != 2)
            return false;
        if (anchor0 != start0)
            return false;

        int targetStart0 = GetClosestTwoSlotStartFromScreenPosition(screenPosition, eventCamera);
        if (targetStart0 == start0)
            return true;

        int[] permutation = BuildTwoSlotGroupPermutation(start0, targetStart0);
        if (permutation == null)
            return false;

        DiceDraggableUI[] oldEquipped = (DiceDraggableUI[])equipped.Clone();
        RectTransform[] oldCombatAnchors = linkedCombatSlotAnchors != null ? (RectTransform[])linkedCombatSlotAnchors.Clone() : null;

        equipped = DiceEquipStateUtility.ApplyPermutation(equipped, permutation);
        if (linkedCombatSlotAnchors != null && linkedCombatSlotAnchors.Length == permutation.Length)
            linkedCombatSlotAnchors = DiceEquipStateUtility.ApplyPermutation(linkedCombatSlotAnchors, permutation);

        RebindCombatSlotLaneIndices();
        SyncOutputs();

        bool committed = turnManager.CommitDiceLaneReorder(permutation);
        if (!committed)
        {
            equipped = oldEquipped;
            if (oldCombatAnchors != null)
                linkedCombatSlotAnchors = oldCombatAnchors;
            RebindCombatSlotLaneIndices();
            SyncOutputs();
            ApplyAdaptiveLayout();
            RefreshAllSlots();
            SyncWorldSlotRootsToUI(false);
            return false;
        }

        ApplyAdaptiveLayout();
        RefreshAllSlots();
        SyncWorldSlotRootsToUI(false);
        return true;
    }

    private void FinalizeDraggedDice(int insertIndex)
    {
        int count = CountEquipped();
        if (_draggingDice == null || _dragSourceIndex < 0 || count <= 0)
        {
            ClearDragState();
            return;
        }

        insertIndex = Mathf.Clamp(insertIndex, 0, Mathf.Max(0, count - 1));
        _previewInsertIndex = insertIndex;

        DiceDraggableUI[] oldEquipped = (DiceDraggableUI[])equipped.Clone();
        RectTransform[] oldCombatAnchors = linkedCombatSlotAnchors != null ? (RectTransform[])linkedCombatSlotAnchors.Clone() : null;
        int[] permutation = BuildCommittedPermutation();

        ApplyInsertedOrder(insertIndex);
        ApplyInsertedOrderToCombatSlots(insertIndex);
        RebindCombatSlotLaneIndices();
        SyncOutputs();

        bool committed = true;
        if (turnManager != null)
            committed = turnManager.CommitDiceLaneReorder(permutation);

        if (!committed)
        {
            equipped = oldEquipped;
            if (oldCombatAnchors != null)
                linkedCombatSlotAnchors = oldCombatAnchors;
            RebindCombatSlotLaneIndices();
            SyncOutputs();
        }

        ClearDragState();
        ApplyAdaptiveLayout();
        RefreshAllSlots();
        SyncWorldSlotRootsToUI(false);
    }

    private void FinalizeDirectGroupedDiceDrag(int targetSlotIndex)
    {
        if (_draggingDice == null || _dragSourceIndex < 0)
        {
            ClearDragState();
            return;
        }

        targetSlotIndex = Mathf.Clamp(targetSlotIndex, 0, 2);

        if (!TryGetDirectDragAllowedRange(_draggingDice, out int minSlot, out int maxSlot))
        {
            HandleInvalidDrop(_draggingDice);
            return;
        }

        if (targetSlotIndex < minSlot || targetSlotIndex > maxSlot)
        {
            HandleInvalidDrop(_draggingDice);
            return;
        }

        if (targetSlotIndex != _dragSourceIndex)
            equipped = DiceEquipStateUtility.SwapItems(equipped, _dragSourceIndex, targetSlotIndex);

        SyncOutputs();
        RefreshTurnManagerRef();
        if (turnManager != null)
            turnManager.RefreshPlanningAfterDiceValueReorder();

        ClearDragState();
        ApplyAdaptiveLayout();
        RefreshAllSlots();
        SyncWorldSlotRootsToUI(false);
    }

    private void ApplyInsertedOrder(int insertIndex)
    {
        if (_draggingDice == null) return;
        equipped = DiceEquipStateUtility.InsertDraggedItem(equipped, _draggingDice, insertIndex);
    }

    private void ApplyInsertedOrderToCombatSlots(int insertIndex)
    {
        if (_draggingCombatSlot == null)
            return;

        linkedCombatSlotAnchors = DiceEquipStateUtility.ReorderCombatSlots(
            linkedCombatSlotAnchors,
            _draggingCombatSlot,
            insertIndex,
            CountEquipped());
    }

    private int[] BuildCommittedPermutation()
    {
        int count = CountEquipped();
        int insertIndex = Mathf.Clamp(_previewInsertIndex, 0, Mathf.Max(0, count - 1));
        return DiceEquipStateUtility.BuildPermutation((DiceDraggableUI[])equipped.Clone(), _draggingDice, insertIndex);
    }

    private RectTransform GetCombatSlotAt(int index)
        => DiceEquipStateUtility.GetCombatSlotAt(linkedCombatSlotAnchors, index);

    private void ClearDragState()
    {
        _draggingDice = null;
        _draggingCombatSlot = null;
        _dragSourceIndex = -1;
        _previewInsertIndex = -1;
    }

    private void CompactEquipped()
        => equipped = DiceEquipStateUtility.Compact(equipped);

    private void RebindCombatSlotLaneIndices()
    {
        RefreshTurnManagerRef();
        DiceEquipStateUtility.RebindCombatSlotLaneIndices(linkedCombatSlotAnchors, turnManager);
    }

    private int CountEquipped()
        => DiceEquipLayoutUtility.CountOccupied(equipped);

    private int GetEquippedIndex(DiceDraggableUI dice)
        => DiceEquipLayoutUtility.FindIndex(equipped, dice);

    private void SnapToEquip(int slotIndex, DiceDraggableUI dice, bool instant = false)
    {
        RectTransform anchor = equipSlotAnchors != null && slotIndex >= 0 && slotIndex < equipSlotAnchors.Length
            ? equipSlotAnchors[slotIndex]
            : null;

        if (anchor == null)
        {
            dice.ReturnToCachedHome();
            return;
        }

        Register(dice);
        if (instant)
        {
            RectTransform rt = dice.GetComponent<RectTransform>();
            rt.SetParent(anchor, worldPositionStays: false);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
        }
        else
        {
            dice.SnapToAnchorAnimated(anchor, Vector2.zero);
        }
    }

    private void RefreshAllSlots(bool instant = false)
    {
        DiceEquipLayoutUtility.BuildDisplayedOrder(equipped, _displayOrderBuffer, _draggingDice, _dragSourceIndex, _previewInsertIndex);

        for (int i = 0; i < _displayOrderBuffer.Length; i++)
        {
            DiceDraggableUI dice = _displayOrderBuffer[i];
            if (dice == null || dice == _draggingDice) continue;
            SnapToEquip(i, dice, instant);
        }
    }

    private void RefreshCombatSlotPreview(bool instant = false)
    {
        CaptureCombatSlotBaseY();
        DiceEquipPresentationUtility.RefreshCombatSlotPreview(
            mirrorAdaptiveLayoutToCombatSlots,
            linkedCombatSlotAnchors,
            _combatDisplayBuffer,
            _draggingCombatSlot,
            _dragSourceIndex,
            _previewInsertIndex,
            CountEquipped(),
            hideEmptyCombatSlotAnchors,
            rowY,
            useAdaptiveCenterLayout,
            pairHalfSpacing,
            trioSideOffset,
            combatSlotsXOffset,
            _combatSlotIdentity,
            _combatSlotBaseY,
            instant,
            combatSlotPreviewDuration,
            combatSlotPreviewEase);
    }

    private int GetInsertIndexFromScreenPosition(Vector2 screenPosition, Camera eventCamera)
        => DiceEquipLayoutUtility.GetInsertIndexFromScreenPosition(
            equipped,
            _dragSourceIndex,
            equipSlotAnchors,
            transform,
            useAdaptiveCenterLayout,
            pairHalfSpacing,
            trioSideOffset,
            rowY,
            screenPosition,
            eventCamera);

    private int GetDragInsertIndex(Vector2 screenPosition, Camera eventCamera)
    {
        int insertIndex = GetInsertIndexFromScreenPosition(screenPosition, eventCamera);
        if (!enableGroupedSkillDiceReorder)
            return insertIndex;

        if (!TryGetDirectDragAllowedRange(_draggingDice, out int minInsert, out int maxInsert))
            return AdjustInsertIndexForExternalGroupBlock(insertIndex, screenPosition, eventCamera);

        return Mathf.Clamp(insertIndex, minInsert, maxInsert);
    }

    private int GetClosestEquipSlotIndexFromScreenPosition(Vector2 screenPosition, Camera eventCamera)
    {
        Vector3 pointerWorld;
        RectTransformUtility.ScreenPointToWorldPointInRectangle(transform as RectTransform, screenPosition, eventCamera, out pointerWorld);

        int bestIndex = 0;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < equipSlotAnchors.Length; i++)
        {
            RectTransform anchor = equipSlotAnchors[i];
            if (anchor == null)
                continue;

            float distance = Mathf.Abs(pointerWorld.x - anchor.position.x);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private bool TryGetDirectDragAllowedRange(DiceDraggableUI dice, out int minInsert, out int maxInsert)
    {
        minInsert = -1;
        maxInsert = -1;

        if (dice == null)
            return false;

        int draggedIndex = dice == _draggingDice ? _dragSourceIndex : GetEquippedIndex(dice);
        if (draggedIndex < 0)
            return false;

        RefreshTurnManagerRef();
        if (turnManager == null)
            return false;

        if (!turnManager.TryGetPlannedGroupAtLane(draggedIndex + 1, out _, out int start0, out int span))
            return false;
        if (span != 2)
            return false;

        minInsert = start0;
        maxInsert = span == 2 ? start0 + 1 : start0 + span - 1;
        return true;
    }

    private bool IsDirectGroupDiceDrag(DiceDraggableUI dice)
        => enableGroupedSkillDiceReorder && TryGetDirectDragAllowedRange(dice, out _, out _);

    private int AdjustInsertIndexForExternalGroupBlock(int proposedInsertIndex, Vector2 screenPosition, Camera eventCamera)
    {
        if (_draggingDice == null)
            return proposedInsertIndex;

        int draggedIndex = _dragSourceIndex;
        if (draggedIndex < 0)
            return proposedInsertIndex;

        if (!TryFindTwoSlotGroup(out int groupStart, out int groupEnd))
            return proposedInsertIndex;

        if (draggedIndex >= groupStart && draggedIndex <= groupEnd)
            return proposedInsertIndex;

        float pointerX = GetPointerWorldX(screenPosition, eventCamera);
        float leftEdgeX = GetAnchorWorldX(groupStart);
        float rightEdgeX = GetAnchorWorldX(groupEnd);

        if (draggedIndex > groupEnd)
        {
            if (pointerX < leftEdgeX)
                return groupStart;

            return Mathf.Clamp(draggedIndex, 0, 2);
        }

        if (draggedIndex < groupStart)
        {
            if (pointerX > rightEdgeX)
                return groupEnd;

            return Mathf.Clamp(draggedIndex, 0, 2);
        }

        return proposedInsertIndex;
    }

    private bool TryFindTwoSlotGroup(out int groupStart, out int groupEnd)
    {
        groupStart = -1;
        groupEnd = -1;

        RefreshTurnManagerRef();
        if (turnManager == null)
            return false;

        for (int lane1 = 1; lane1 <= 3; lane1++)
        {
            if (!turnManager.TryGetPlannedGroupAtLane(lane1, out _, out int start0, out int span))
                continue;
            if (span != 2)
                continue;

            groupStart = start0;
            groupEnd = start0 + 1;
            return true;
        }

        return false;
    }

    private bool TryAdjustExternalGroupDropTarget(int proposedSlotIndex, out int adjustedSlotIndex)
    {
        adjustedSlotIndex = proposedSlotIndex;

        if (_dragSourceIndex < 0)
            return false;

        if (!TryFindTwoSlotGroup(out int groupStart, out int groupEnd))
            return false;

        if (_dragSourceIndex >= groupStart && _dragSourceIndex <= groupEnd)
            return false;

        if (_dragSourceIndex > groupEnd)
        {
            if (proposedSlotIndex == groupEnd)
            {
                adjustedSlotIndex = -1;
                return true;
            }

            if (proposedSlotIndex == groupStart)
            {
                adjustedSlotIndex = groupStart;
                return true;
            }
        }

        if (_dragSourceIndex < groupStart)
        {
            if (proposedSlotIndex == groupStart)
            {
                adjustedSlotIndex = -1;
                return true;
            }

            if (proposedSlotIndex == groupEnd)
            {
                adjustedSlotIndex = groupEnd;
                return true;
            }
        }

        return false;
    }

    private float GetPointerWorldX(Vector2 screenPosition, Camera eventCamera)
    {
        Vector3 pointerWorld;
        RectTransformUtility.ScreenPointToWorldPointInRectangle(transform as RectTransform, screenPosition, eventCamera, out pointerWorld);
        return pointerWorld.x;
    }

    private float GetAnchorWorldX(int slotIndex)
    {
        if (equipSlotAnchors == null || slotIndex < 0 || slotIndex >= equipSlotAnchors.Length || equipSlotAnchors[slotIndex] == null)
            return transform.position.x;
        return equipSlotAnchors[slotIndex].position.x;
    }

    private int GetClosestTwoSlotStartFromScreenPosition(Vector2 screenPosition, Camera eventCamera)
    {
        Vector3 pointerWorld;
        RectTransformUtility.ScreenPointToWorldPointInRectangle(transform as RectTransform, screenPosition, eventCamera, out pointerWorld);

        Vector3 start0Center = GetTwoSlotCenterWorldPos(0);
        Vector3 start1Center = GetTwoSlotCenterWorldPos(1);

        float distToStart0 = Mathf.Abs(pointerWorld.x - start0Center.x);
        float distToStart1 = Mathf.Abs(pointerWorld.x - start1Center.x);
        return distToStart0 <= distToStart1 ? 0 : 1;
    }

    private Vector3 GetTwoSlotCenterWorldPos(int start0)
    {
        start0 = Mathf.Clamp(start0, 0, 1);
        RectTransform left = equipSlotAnchors != null && start0 < equipSlotAnchors.Length ? equipSlotAnchors[start0] : null;
        RectTransform right = equipSlotAnchors != null && start0 + 1 < equipSlotAnchors.Length ? equipSlotAnchors[start0 + 1] : null;

        if (left == null && right == null)
            return transform.position;
        if (left == null)
            return right.position;
        if (right == null)
            return left.position;

        return (left.position + right.position) * 0.5f;
    }

    private static int[] BuildTwoSlotGroupPermutation(int currentStart0, int targetStart0)
    {
        currentStart0 = Mathf.Clamp(currentStart0, 0, 1);
        targetStart0 = Mathf.Clamp(targetStart0, 0, 1);

        if (currentStart0 == targetStart0)
            return new[] { 0, 1, 2 };

        if (currentStart0 == 0 && targetStart0 == 1)
            return new[] { 2, 0, 1 };

        if (currentStart0 == 1 && targetStart0 == 0)
            return new[] { 1, 2, 0 };

        return null;
    }

    private void ApplyAdaptiveLayout(bool instant = false)
    {
        CaptureCombatSlotBaseY();
        DiceEquipPresentationUtility.ApplyAdaptiveLayout(
            equipSlotAnchors,
            linkedCombatSlotAnchors,
            CountEquipped(),
            rowY,
            useAdaptiveCenterLayout,
            pairHalfSpacing,
            trioSideOffset,
            hideEmptyAnchors,
            mirrorAdaptiveLayoutToCombatSlots,
            hideEmptyCombatSlotAnchors,
            combatSlotsXOffset,
            _combatSlotBaseY,
            anchorTweenDuration,
            anchorEase,
            instant);
    }

    private void CaptureCombatSlotBaseY()
        => DiceEquipPresentationUtility.CaptureCombatSlotBaseY(linkedCombatSlotAnchors, _combatSlotIdentity, _combatSlotBaseY, ref _capturedCombatSlotY);

    public void SyncOutputs()
        => DiceEquipPresentationUtility.SyncOutputs(equipped, runInventory, diceRig);

    private void RebindWorldSlotOwnersFromCurrentOrder()
        => DiceEquipWorldSyncUtility.RebindWorldSlotOwnersFromCurrentOrder(equipped, _worldSlotOwners, _worldSlotRoots, diceRig);

    private void SyncWorldSlotRootsToUI(bool instant)
    {
        if (!mirrorDiceRigSlotsWithLiveUI) return;
        if (diceRig == null) return;

        if (_worldSlotOwners[0] == null && _worldSlotOwners[1] == null && _worldSlotOwners[2] == null)
            RebindWorldSlotOwnersFromCurrentOrder();

        DiceEquipWorldFollowUtility.SyncWorldSlotRootsToUI(
            mirrorDiceRigSlotsWithLiveUI,
            diceRig,
            _worldSlotOwners,
            _worldSlotRoots,
            instant,
            ref _rootCanvas,
            this,
            worldFollowCamera);
    }

    private void RefreshTurnManagerRef()
    {
        if (turnManager == null)
            turnManager = FindObjectOfType<TurnManager>(true);
    }
}
