using DG.Tweening;
using UnityEngine;

/// <summary>
/// 3-slot passive loadout UI.
/// - No overflow.
/// - Reorder / swap supported.
/// - Kept usable during combat by default.
/// - Adaptive 'joker row' layout.
/// </summary>
public class PassiveEquipUIManager : MonoBehaviour
{
    [Header("Wiring")]
    public RectTransform[] equipSlotAnchors = new RectTransform[3];
    public RectTransform dragLayer;
    public RunInventoryManager runInventory;

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
    private PassiveDraggableUI _draggingPassive;
    private int _dragSourceIndex = -1;
    private int _previewInsertIndex = -1;

    private void Awake()
    {
        if (runInventory == null)
            runInventory = GetComponentInParent<RunInventoryManager>(true);

        BootstrapFromAnchorsIfNeeded();
    }

    private void Start()
    {
        BootstrapFromAnchorsIfNeeded();
        ApplyAdaptiveLayout(true);
        RefreshAllSlots(true);
        SyncToRunInventory();
    }

    private void LateUpdate()
    {
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
        ApplyAdaptiveLayout(true);
        RefreshAllSlots(true);
        SyncToRunInventory();
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
            return;

        _previewInsertIndex = nextInsertIndex;
        RefreshAllSlots();
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
            return;
        }

        if (fromSlot < 0 && existing != null)
        {
            passive.ReturnToCachedHome();
            ApplyAdaptiveLayout();
            RefreshAllSlots();
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
    }

    public void HandleInvalidDrop(PassiveDraggableUI passive)
    {
        if (passive == null) return;

        ClearDragState();
        passive.ReturnToCachedHome();
        ApplyAdaptiveLayout();
        RefreshAllSlots();
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
        ApplyAdaptiveLayout();
        RefreshAllSlots();
        SyncToRunInventory();
    }

    private void ClearDragState()
    {
        _draggingPassive = null;
        _dragSourceIndex = -1;
        _previewInsertIndex = -1;
    }

    private void BootstrapFromAnchorsIfNeeded()
    {
        if (equipSlotAnchors == null || equipSlotAnchors.Length == 0)
            return;

        bool hasSerializedEquipped = false;
        bool needsManagerRebind = false;
        bool foundPassiveChildInAnchor = false;

        if (equipped == null || equipped.Length != 3)
            equipped = new PassiveDraggableUI[3];

        for (int i = 0; i < equipped.Length; i++)
        {
            PassiveDraggableUI passive = equipped[i];
            if (passive == null)
                continue;

            hasSerializedEquipped = true;
            if (passive.manager != this)
                needsManagerRebind = true;
        }

        for (int i = 0; i < equipSlotAnchors.Length; i++)
        {
            RectTransform anchor = equipSlotAnchors[i];
            if (anchor == null)
                continue;

            PassiveDraggableUI childPassive = anchor.GetComponentInChildren<PassiveDraggableUI>(true);
            if (childPassive == null)
                continue;

            foundPassiveChildInAnchor = true;
            if (childPassive.manager != this)
                needsManagerRebind = true;
        }

        if (!foundPassiveChildInAnchor)
            return;

        if (!hasSerializedEquipped || needsManagerRebind)
        {
            for (int i = 0; i < equipped.Length; i++)
                equipped[i] = null;

            for (int i = 0; i < equipSlotAnchors.Length && i < equipped.Length; i++)
            {
                RectTransform anchor = equipSlotAnchors[i];
                if (anchor == null)
                    continue;

                PassiveDraggableUI passive = anchor.GetComponentInChildren<PassiveDraggableUI>(true);
                if (passive == null)
                    continue;

                Register(passive);
                equipped[i] = passive;
            }

            CompactEquipped();
        }
        else
        {
            for (int i = 0; i < equipped.Length; i++)
                Register(equipped[i]);
        }
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
}
