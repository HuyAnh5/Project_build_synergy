using DG.Tweening;
using UnityEngine;

/// <summary>
/// 3-slot passive loadout UI.
/// - No overflow.
/// - Reorder / swap supported.
/// - Kept usable during combat by default.
/// - Adaptive 'joker row' layout.
/// - Optional linked passive roots that follow the paired PassiveUi live, including while dragging.
/// </summary>
public class PassiveEquipUIManager : MonoBehaviour
{
    [Header("Wiring")]
    public RectTransform[] equipSlotAnchors = new RectTransform[3];
    public RectTransform dragLayer;
    public RunInventoryManager runInventory;

    [Header("Linked Passive Roots (optional)")]
    [Tooltip("Optional transforms that should follow PassiveUi_1..3 live, including while dragging. Can be world objects or UI transforms.")]
    public Transform[] linkedPassiveRoots = new Transform[3];
    [Tooltip("If enabled, linked passive roots follow the paired PassiveUi live.")]
    public bool mirrorLinkedPassiveRootsWithLiveUI = true;
    [Tooltip("Camera used to convert PassiveUi screen position into world position. Leave empty to use Canvas worldCamera or Camera.main.")]
    public Camera worldFollowCamera;

    [Header("Behavior")]
    [Tooltip("Optional manual lock. Leave false if you want passive drag even during combat.")]
    public bool interactionsLocked;

    [Header("Adaptive Layout")]
    public bool useAdaptiveCenterLayout = true;
    public float pairHalfSpacing = 180f;
    public float trioSideOffset = 250f;
    public float rowY = 0f;
    public bool hideEmptyAnchors = true;

    [Header("Tween")]
    public float anchorTweenDuration = 0.22f;
    public float itemSnapDuration = 0.18f;
    public Ease anchorEase = Ease.OutCubic;
    public Ease itemEase = Ease.OutBack;

    public PassiveDraggableUI[] equipped = new PassiveDraggableUI[3];
    public bool WasDropConsumedThisFrame { get; private set; }

    private readonly PassiveDraggableUI[] _displayOrderBuffer = new PassiveDraggableUI[3];
    private readonly PassiveDraggableUI[] _linkedPassiveOwners = new PassiveDraggableUI[3];
    private PassiveDraggableUI _draggingPassive;
    private int _dragSourceIndex = -1;
    private int _previewInsertIndex = -1;
    private Canvas _rootCanvas;

    private void Awake()
    {
        _rootCanvas = GetComponentInParent<Canvas>();

        if (runInventory == null)
            runInventory = GetComponentInParent<RunInventoryManager>(true);

        RebindLinkedPassiveOwnersFromCurrentOrder();
    }

    private void Start()
    {
        ApplyAdaptiveLayout(true);
        RefreshAllSlots(true);
        SyncToRunInventory();
        SyncLinkedPassiveRootsToUI(true);
    }

    private void LateUpdate()
    {
        SyncLinkedPassiveRootsToUI(false);
        WasDropConsumedThisFrame = false;
    }

    public bool CanInteract() => !interactionsLocked;

    public void Register(PassiveDraggableUI passive)
    {
        if (passive == null) return;
        passive.manager = this;
        passive.tweenDuration = itemSnapDuration;
        passive.RefreshVisual();
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

            PassiveDraggableUI passive = anchor.GetComponentInChildren<PassiveDraggableUI>(true);
            if (passive == null) continue;

            Register(passive);
            equipped[i] = passive;
        }

        CompactEquipped();
        RebindLinkedPassiveOwnersFromCurrentOrder();
        ApplyAdaptiveLayout(true);
        RefreshAllSlots(true);
        SyncToRunInventory();
        SyncLinkedPassiveRootsToUI(true);
    }

    public void NotifyBeginDrag(PassiveDraggableUI passive)
    {
        if (passive == null) return;

        _draggingPassive = passive;
        _dragSourceIndex = GetEquippedIndex(passive);
        _previewInsertIndex = _dragSourceIndex;
    }

    public void NotifyDrag(PassiveDraggableUI passive, Vector2 screenPosition, Camera eventCamera)
    {
        if (passive == null || passive != _draggingPassive) return;
        if (_dragSourceIndex < 0) return;

        int nextInsertIndex = GetInsertIndexFromScreenPosition(screenPosition, eventCamera);
        if (nextInsertIndex == _previewInsertIndex)
        {
            SyncLinkedPassiveRootsToUI(false);
            return;
        }

        _previewInsertIndex = nextInsertIndex;
        RefreshAllSlots();
        SyncLinkedPassiveRootsToUI(false);
    }

    public void NotifyEndDrag(PassiveDraggableUI passive, Vector2 screenPosition, Camera eventCamera)
    {
        if (passive == null) return;

        WasDropConsumedThisFrame = true;

        if (passive != _draggingPassive)
        {
            HandleInvalidDrop(passive);
            return;
        }

        if (_dragSourceIndex < 0)
        {
            HandleInvalidDrop(passive);
            return;
        }

        FinalizeDraggedPassive(GetInsertIndexFromScreenPosition(screenPosition, eventCamera));
    }

    public void HandleDropToEquipSlot(PassiveDraggableUI passive, int slotIndex)
    {
        WasDropConsumedThisFrame = true;
        if (!CanInteract() || passive == null) return;

        slotIndex = Mathf.Clamp(slotIndex, 0, 2);

        if (passive == _draggingPassive && _dragSourceIndex >= 0)
        {
            FinalizeDraggedPassive(slotIndex);
            return;
        }

        int fromSlot = GetEquippedIndex(passive);
        PassiveDraggableUI existing = equipped[slotIndex];

        if (fromSlot == slotIndex)
        {
            ApplyAdaptiveLayout();
            SnapToEquip(slotIndex, passive);
            SyncLinkedPassiveRootsToUI(false);
            return;
        }

        if (fromSlot < 0 && existing != null)
        {
            passive.ReturnToCachedHome();
            ApplyAdaptiveLayout();
            RefreshAllSlots();
            SyncLinkedPassiveRootsToUI(false);
            return;
        }

        if (fromSlot >= 0)
            equipped[fromSlot] = null;

        if (existing != null && fromSlot >= 0)
            equipped[fromSlot] = existing;

        equipped[slotIndex] = passive;

        CompactEquipped();
        ApplyAdaptiveLayout();
        RefreshAllSlots();
        SyncToRunInventory();
        SyncLinkedPassiveRootsToUI(false);
    }

    public void HandleInvalidDrop(PassiveDraggableUI passive)
    {
        if (passive == null) return;

        ClearDragState();
        passive.ReturnToCachedHome();
        ApplyAdaptiveLayout();
        RefreshAllSlots();
        SyncLinkedPassiveRootsToUI(false);
    }

    private void FinalizeDraggedPassive(int insertIndex)
    {
        int count = CountEquipped();
        if (_draggingPassive == null || _dragSourceIndex < 0 || count <= 0)
        {
            ClearDragState();
            return;
        }

        insertIndex = Mathf.Clamp(insertIndex, 0, Mathf.Max(0, count - 1));
        equipped = PassiveEquipStateUtility.InsertDraggedItem(equipped, _draggingPassive, insertIndex);

        ClearDragState();
        RebindLinkedPassiveOwnersFromCurrentOrder();
        ApplyAdaptiveLayout();
        RefreshAllSlots();
        SyncToRunInventory();
        SyncLinkedPassiveRootsToUI(false);
    }

    private void ClearDragState()
    {
        _draggingPassive = null;
        _dragSourceIndex = -1;
        _previewInsertIndex = -1;
    }

    private void CompactEquipped()
        => equipped = PassiveEquipStateUtility.Compact(equipped);

    private int CountEquipped()
        => PassiveEquipLayoutUtility.CountOccupied(equipped);

    private int GetEquippedIndex(PassiveDraggableUI passive)
        => PassiveEquipLayoutUtility.FindIndex(equipped, passive);

    private void SnapToEquip(int slotIndex, PassiveDraggableUI passive, bool instant = false)
    {
        if (passive == null)
            return;

        RectTransform anchor = equipSlotAnchors != null && slotIndex >= 0 && slotIndex < equipSlotAnchors.Length
            ? equipSlotAnchors[slotIndex]
            : null;
        if (anchor == null)
        {
            passive.ReturnToCachedHome();
            return;
        }

        Register(passive);
        if (instant)
        {
            RectTransform rt = passive.GetComponent<RectTransform>();
            rt.SetParent(anchor, worldPositionStays: false);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            return;
        }

        passive.SnapToAnchorAnimated(anchor, Vector2.zero);
    }

    private void RefreshAllSlots(bool instant = false)
        => PassiveEquipPresentationUtility.RefreshAllSlots(
            equipped,
            _displayOrderBuffer,
            _draggingPassive,
            _dragSourceIndex,
            _previewInsertIndex,
            equipSlotAnchors,
            instant,
            this);

    private int GetInsertIndexFromScreenPosition(Vector2 screenPosition, Camera eventCamera)
        => PassiveEquipLayoutUtility.GetInsertIndexFromScreenPosition(
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

    private void ApplyAdaptiveLayout(bool instant = false)
        => PassiveEquipPresentationUtility.ApplyAdaptiveLayout(
            equipSlotAnchors,
            CountEquipped(),
            rowY,
            useAdaptiveCenterLayout,
            pairHalfSpacing,
            trioSideOffset,
            hideEmptyAnchors,
            anchorTweenDuration,
            anchorEase,
            instant);

    private void SyncToRunInventory()
        => PassiveEquipPresentationUtility.SyncToRunInventory(equipped, runInventory);

    private void RebindLinkedPassiveOwnersFromCurrentOrder()
        => PassiveEquipWorldSyncUtility.RebindLinkedPassiveOwnersFromCurrentOrder(equipped, _linkedPassiveOwners);

    private void SyncLinkedPassiveRootsToUI(bool instant)
    {
        if (!mirrorLinkedPassiveRootsWithLiveUI) return;
        if (linkedPassiveRoots == null || linkedPassiveRoots.Length == 0) return;

        if (_linkedPassiveOwners[0] == null && _linkedPassiveOwners[1] == null && _linkedPassiveOwners[2] == null)
            RebindLinkedPassiveOwnersFromCurrentOrder();

        Camera uiCamera = GetUICamera();
        Camera worldCameraToUse = GetWorldFollowCamera();
        PassiveEquipWorldSyncUtility.SyncLinkedPassiveRootsToUI(
            mirrorLinkedPassiveRootsWithLiveUI,
            linkedPassiveRoots,
            _linkedPassiveOwners,
            instant,
            uiCamera,
            worldCameraToUse);
    }

    private Camera GetUICamera()
    {
        if (_rootCanvas == null)
            _rootCanvas = GetComponentInParent<Canvas>();
        return PassiveEquipWorldSyncUtility.GetUICamera(_rootCanvas);
    }

    private Camera GetWorldFollowCamera()
        => PassiveEquipWorldSyncUtility.GetWorldFollowCamera(worldFollowCamera, GetUICamera());
}
