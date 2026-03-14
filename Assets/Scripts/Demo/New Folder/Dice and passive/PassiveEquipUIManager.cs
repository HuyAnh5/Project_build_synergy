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
        ApplyInsertedOrder(insertIndex);

        ClearDragState();
        RebindLinkedPassiveOwnersFromCurrentOrder();
        ApplyAdaptiveLayout();
        RefreshAllSlots();
        SyncToRunInventory();
        SyncLinkedPassiveRootsToUI(false);
    }

    private void ApplyInsertedOrder(int insertIndex)
    {
        PassiveDraggableUI dragged = _draggingPassive;
        if (dragged == null) return;

        PassiveDraggableUI[] reordered = new PassiveDraggableUI[3];
        int write = 0;
        bool inserted = false;

        for (int i = 0; i < equipped.Length; i++)
        {
            PassiveDraggableUI current = equipped[i];
            if (current == null || current == dragged) continue;

            if (!inserted && write == insertIndex)
            {
                reordered[write++] = dragged;
                inserted = true;
            }

            if (write < reordered.Length)
                reordered[write++] = current;
        }

        if (!inserted && write < reordered.Length)
            reordered[write++] = dragged;

        equipped = reordered;
    }

    private void ClearDragState()
    {
        _draggingPassive = null;
        _dragSourceIndex = -1;
        _previewInsertIndex = -1;
    }

    private void CompactEquipped()
    {
        int write = 0;
        PassiveDraggableUI[] compact = new PassiveDraggableUI[3];
        for (int i = 0; i < equipped.Length; i++)
        {
            if (equipped[i] == null) continue;
            if (write >= compact.Length) break;
            compact[write++] = equipped[i];
        }
        equipped = compact;
    }

    private int CountEquipped()
    {
        int c = 0;
        for (int i = 0; i < equipped.Length; i++)
            if (equipped[i] != null) c++;
        return c;
    }

    private int GetEquippedIndex(PassiveDraggableUI passive)
    {
        if (passive == null) return -1;
        for (int i = 0; i < equipped.Length; i++)
            if (equipped[i] == passive) return i;
        return -1;
    }

    private void SnapToEquip(int slotIndex, PassiveDraggableUI passive, bool instant = false)
    {
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
        }
        else
        {
            passive.SnapToAnchorAnimated(anchor, Vector2.zero);
        }
    }

    private void RefreshAllSlots(bool instant = false)
    {
        BuildDisplayedOrder();

        for (int i = 0; i < _displayOrderBuffer.Length; i++)
        {
            PassiveDraggableUI passive = _displayOrderBuffer[i];
            if (passive == null || passive == _draggingPassive) continue;
            SnapToEquip(i, passive, instant);
        }
    }

    private void BuildDisplayedOrder()
    {
        for (int i = 0; i < _displayOrderBuffer.Length; i++)
            _displayOrderBuffer[i] = null;

        if (_draggingPassive == null || _dragSourceIndex < 0 || _previewInsertIndex < 0)
        {
            for (int i = 0; i < equipped.Length; i++)
                _displayOrderBuffer[i] = equipped[i];
            return;
        }

        int count = CountEquipped();
        int insertIndex = Mathf.Clamp(_previewInsertIndex, 0, Mathf.Max(0, count - 1));
        int write = 0;
        bool inserted = false;

        for (int i = 0; i < equipped.Length; i++)
        {
            PassiveDraggableUI current = equipped[i];
            if (current == null || current == _draggingPassive) continue;

            if (!inserted && write == insertIndex)
            {
                _displayOrderBuffer[write++] = _draggingPassive;
                inserted = true;
            }

            if (write < _displayOrderBuffer.Length)
                _displayOrderBuffer[write++] = current;
        }

        if (!inserted && write < _displayOrderBuffer.Length)
            _displayOrderBuffer[write] = _draggingPassive;
    }

    private int GetInsertIndexFromScreenPosition(Vector2 screenPosition, Camera eventCamera)
    {
        int count = CountEquipped();
        if (count <= 1)
            return 0;

        RectTransform reference = GetAnchorReferenceRect();
        if (reference == null)
            return Mathf.Clamp(_dragSourceIndex, 0, Mathf.Max(0, count - 1));

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(reference, screenPosition, eventCamera, out Vector2 local))
            return Mathf.Clamp(_dragSourceIndex, 0, Mathf.Max(0, count - 1));

        Vector2[] positions = BuildAdaptivePositions(count, rowY);
        float x = local.x;

        for (int i = 0; i < count - 1; i++)
        {
            float midpoint = (positions[i].x + positions[i + 1].x) * 0.5f;
            if (x < midpoint)
                return i;
        }

        return count - 1;
    }

    private RectTransform GetAnchorReferenceRect()
    {
        if (equipSlotAnchors != null)
        {
            for (int i = 0; i < equipSlotAnchors.Length; i++)
            {
                RectTransform anchor = equipSlotAnchors[i];
                if (anchor == null) continue;

                RectTransform parent = anchor.parent as RectTransform;
                if (parent != null) return parent;
                return anchor;
            }
        }

        return transform as RectTransform;
    }

    private void ApplyAdaptiveLayout(bool instant = false)
    {
        if (!useAdaptiveCenterLayout || equipSlotAnchors == null || equipSlotAnchors.Length == 0)
            return;

        int count = CountEquipped();
        Vector2[] positions = BuildAdaptivePositions(count, rowY);

        for (int i = 0; i < equipSlotAnchors.Length; i++)
        {
            RectTransform anchor = equipSlotAnchors[i];
            if (anchor == null) continue;

            bool occupied = i < count;
            if (!anchor.gameObject.activeSelf && (!hideEmptyAnchors || occupied))
                anchor.gameObject.SetActive(true);

            Vector2 target = positions[Mathf.Clamp(i, 0, positions.Length - 1)];
            anchor.DOKill();
            if (instant)
                anchor.anchoredPosition = target;
            else
                anchor.DOAnchorPos(target, anchorTweenDuration).SetEase(anchorEase).SetUpdate(true);

            anchor.gameObject.SetActive(!hideEmptyAnchors || occupied);
        }
    }

    private Vector2[] BuildAdaptivePositions(int count, float targetY)
    {
        Vector2[] positions = new Vector2[3]
        {
            new Vector2(-trioSideOffset, targetY),
            new Vector2(0f, targetY),
            new Vector2(trioSideOffset, targetY)
        };

        if (count <= 0)
            return positions;

        if (count == 1)
        {
            positions[0] = new Vector2(0f, targetY);
        }
        else if (count == 2)
        {
            positions[0] = new Vector2(-pairHalfSpacing, targetY);
            positions[1] = new Vector2(pairHalfSpacing, targetY);
        }
        else
        {
            positions[0] = new Vector2(-trioSideOffset, targetY);
            positions[1] = new Vector2(0f, targetY);
            positions[2] = new Vector2(trioSideOffset, targetY);
        }

        return positions;
    }

    private void SyncToRunInventory()
    {
        if (runInventory == null) return;

        SkillPassiveSO[] assets = new SkillPassiveSO[3];
        for (int i = 0; i < 3; i++)
            assets[i] = equipped[i] != null ? equipped[i].passive : null;

        runInventory.SetPassiveLayout(assets);
    }

    private void RebindLinkedPassiveOwnersFromCurrentOrder()
    {
        for (int i = 0; i < _linkedPassiveOwners.Length; i++)
            _linkedPassiveOwners[i] = i < equipped.Length ? equipped[i] : null;
    }

    private void SyncLinkedPassiveRootsToUI(bool instant)
    {
        if (!mirrorLinkedPassiveRootsWithLiveUI) return;
        if (linkedPassiveRoots == null || linkedPassiveRoots.Length == 0) return;

        if (_linkedPassiveOwners[0] == null && _linkedPassiveOwners[1] == null && _linkedPassiveOwners[2] == null)
            RebindLinkedPassiveOwnersFromCurrentOrder();

        Camera uiCamera = GetUICamera();
        Camera worldCameraToUse = GetWorldFollowCamera();
        if (worldCameraToUse == null) return;

        for (int i = 0; i < _linkedPassiveOwners.Length && i < linkedPassiveRoots.Length; i++)
        {
            Transform linkedRoot = linkedPassiveRoots[i];
            PassiveDraggableUI owner = _linkedPassiveOwners[i];
            if (linkedRoot == null || owner == null) continue;
            if (!owner.gameObject.activeInHierarchy) continue;

            if (!TryGetPassiveUICenterWorldPosition(owner, uiCamera, worldCameraToUse, linkedRoot.position, out Vector3 targetWorld))
                continue;

            if (instant)
                linkedRoot.position = targetWorld;
            else
                linkedRoot.position = Vector3.Lerp(linkedRoot.position, targetWorld, 1f);
        }
    }

    private bool TryGetPassiveUICenterWorldPosition(PassiveDraggableUI owner, Camera uiCamera, Camera worldCameraToUse, Vector3 currentWorld, out Vector3 targetWorld)
    {
        targetWorld = currentWorld;
        if (owner == null) return false;

        RectTransform rt = owner.GetComponent<RectTransform>();
        if (rt == null) return false;

        Vector3 uiWorldCenter = rt.TransformPoint(rt.rect.center);
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, uiWorldCenter);

        float depth = Vector3.Dot(currentWorld - worldCameraToUse.transform.position, worldCameraToUse.transform.forward);
        if (depth <= 0.001f)
            depth = Mathf.Abs(worldCameraToUse.transform.InverseTransformPoint(currentWorld).z);
        if (depth <= 0.001f)
            depth = 10f;

        targetWorld = worldCameraToUse.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, depth));
        return true;
    }

    private Camera GetUICamera()
    {
        if (_rootCanvas == null)
            _rootCanvas = GetComponentInParent<Canvas>();

        if (_rootCanvas != null && _rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            if (_rootCanvas.worldCamera != null)
                return _rootCanvas.worldCamera;
        }

        return Camera.main;
    }

    private Camera GetWorldFollowCamera()
    {
        if (worldFollowCamera != null)
            return worldFollowCamera;

        Camera uiCam = GetUICamera();
        if (uiCam != null)
            return uiCam;

        return Camera.main;
    }
}
