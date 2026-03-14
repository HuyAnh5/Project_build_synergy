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

    public DiceDraggableUI[] equipped = new DiceDraggableUI[3];
    public bool WasDropConsumedThisFrame { get; private set; }

    private readonly float[] _combatSlotBaseY = new float[3];
    private readonly DiceDraggableUI[] _displayOrderBuffer = new DiceDraggableUI[3];
    private readonly DiceDraggableUI[] _worldSlotOwners = new DiceDraggableUI[3];
    private readonly Transform[] _worldSlotRoots = new Transform[3];
    private bool _capturedCombatSlotY;

    private DiceDraggableUI _draggingDice;
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
        return turnManager == null || !turnManager.isActiveAndEnabled;
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
    }

    public void NotifyDrag(DiceDraggableUI dice, Vector2 screenPosition, Camera eventCamera)
    {
        if (dice == null || dice != _draggingDice) return;
        if (_dragSourceIndex < 0) return;

        int nextInsertIndex = GetInsertIndexFromScreenPosition(screenPosition, eventCamera);
        if (nextInsertIndex == _previewInsertIndex)
        {
            SyncWorldSlotRootsToUI(false);
            return;
        }

        _previewInsertIndex = nextInsertIndex;
        RefreshAllSlots();
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

        FinalizeDraggedDice(GetInsertIndexFromScreenPosition(screenPosition, eventCamera));
    }

    public void HandleDropToEquipSlot(DiceDraggableUI dice, int slotIndex)
    {
        WasDropConsumedThisFrame = true;
        if (!CanInteract() || dice == null) return;

        slotIndex = Mathf.Clamp(slotIndex, 0, 2);

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

        CompactEquipped();
        ApplyAdaptiveLayout();
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

    private void FinalizeDraggedDice(int insertIndex)
    {
        int count = CountEquipped();
        if (_draggingDice == null || _dragSourceIndex < 0 || count <= 0)
        {
            ClearDragState();
            return;
        }

        insertIndex = Mathf.Clamp(insertIndex, 0, Mathf.Max(0, count - 1));
        ApplyInsertedOrder(insertIndex);

        ClearDragState();
        ApplyAdaptiveLayout();
        RefreshAllSlots();
        SyncOutputs();
        SyncWorldSlotRootsToUI(false);
    }

    private void ApplyInsertedOrder(int insertIndex)
    {
        DiceDraggableUI dragged = _draggingDice;
        if (dragged == null) return;

        DiceDraggableUI[] reordered = new DiceDraggableUI[3];
        int write = 0;
        bool inserted = false;

        for (int i = 0; i < equipped.Length; i++)
        {
            DiceDraggableUI current = equipped[i];
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
        _draggingDice = null;
        _dragSourceIndex = -1;
        _previewInsertIndex = -1;
    }

    private void CompactEquipped()
    {
        int write = 0;
        DiceDraggableUI[] compact = new DiceDraggableUI[3];
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

    private int GetEquippedIndex(DiceDraggableUI dice)
    {
        if (dice == null) return -1;
        for (int i = 0; i < equipped.Length; i++)
            if (equipped[i] == dice) return i;
        return -1;
    }

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
        BuildDisplayedOrder();

        for (int i = 0; i < _displayOrderBuffer.Length; i++)
        {
            DiceDraggableUI dice = _displayOrderBuffer[i];
            if (dice == null || dice == _draggingDice) continue;
            SnapToEquip(i, dice, instant);
        }
    }

    private void BuildDisplayedOrder()
    {
        for (int i = 0; i < _displayOrderBuffer.Length; i++)
            _displayOrderBuffer[i] = null;

        if (_draggingDice == null || _dragSourceIndex < 0 || _previewInsertIndex < 0)
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
            DiceDraggableUI current = equipped[i];
            if (current == null || current == _draggingDice) continue;

            if (!inserted && write == insertIndex)
            {
                _displayOrderBuffer[write++] = _draggingDice;
                inserted = true;
            }

            if (write < _displayOrderBuffer.Length)
                _displayOrderBuffer[write++] = current;
        }

        if (!inserted && write < _displayOrderBuffer.Length)
            _displayOrderBuffer[write] = _draggingDice;
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
        if (!useAdaptiveCenterLayout)
            return;

        int count = CountEquipped();
        Vector2[] dicePositions = BuildAdaptivePositions(count, rowY);
        ApplyPositionsToAnchors(equipSlotAnchors, dicePositions, count, hideEmptyAnchors, 0f, false, instant);

        if (mirrorAdaptiveLayoutToCombatSlots)
        {
            CaptureCombatSlotBaseY();
            ApplyPositionsToAnchors(linkedCombatSlotAnchors, dicePositions, count, hideEmptyCombatSlotAnchors, combatSlotsXOffset, true, instant);
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

    private void ApplyPositionsToAnchors(
        RectTransform[] anchors,
        Vector2[] positions,
        int occupiedCount,
        bool hideEmpty,
        float xOffset,
        bool preserveAnchorY,
        bool instant)
    {
        if (anchors == null || anchors.Length == 0)
            return;

        for (int i = 0; i < anchors.Length; i++)
        {
            RectTransform anchor = anchors[i];
            if (anchor == null) continue;

            bool occupied = i < occupiedCount;
            if (!anchor.gameObject.activeSelf && (!hideEmpty || occupied))
                anchor.gameObject.SetActive(true);

            Vector2 pos = positions[Mathf.Clamp(i, 0, positions.Length - 1)];
            float y = preserveAnchorY && i < _combatSlotBaseY.Length ? _combatSlotBaseY[i] : pos.y;
            Vector2 target = new Vector2(pos.x + xOffset, y);

            anchor.DOKill();
            if (instant)
                anchor.anchoredPosition = target;
            else
                anchor.DOAnchorPos(target, anchorTweenDuration).SetEase(anchorEase).SetUpdate(true);

            anchor.gameObject.SetActive(!hideEmpty || occupied);
        }
    }

    private void CaptureCombatSlotBaseY()
    {
        if (_capturedCombatSlotY) return;
        if (linkedCombatSlotAnchors == null) return;

        for (int i = 0; i < linkedCombatSlotAnchors.Length && i < _combatSlotBaseY.Length; i++)
        {
            RectTransform rt = linkedCombatSlotAnchors[i];
            _combatSlotBaseY[i] = rt != null ? rt.anchoredPosition.y : 0f;
        }

        _capturedCombatSlotY = true;
    }

    public void SyncOutputs()
    {
        DiceSpinnerGeneric[] assets = new DiceSpinnerGeneric[3];
        for (int i = 0; i < 3; i++)
            assets[i] = equipped[i] != null ? equipped[i].dice : null;

        if (runInventory != null)
            runInventory.SetDiceLayout(assets);
        else if (diceRig != null)
        {
            for (int i = 0; i < 3; i++)
            {
                diceRig.AssignDiceToSlot(i, assets[i]);
                diceRig.SetSlotActive(i, assets[i] != null);
            }
        }
    }

    private void RebindWorldSlotOwnersFromCurrentOrder()
    {
        for (int i = 0; i < _worldSlotOwners.Length; i++)
        {
            _worldSlotOwners[i] = i < equipped.Length ? equipped[i] : null;
            _worldSlotRoots[i] = null;

            if (diceRig != null && diceRig.slots != null && i < diceRig.slots.Length && diceRig.slots[i] != null)
            {
                GameObject slotRootGo = diceRig.slots[i].slotRoot;
                if (slotRootGo != null)
                    _worldSlotRoots[i] = slotRootGo.transform;
                else if (diceRig.slots[i].diceRoot != null)
                    _worldSlotRoots[i] = diceRig.slots[i].diceRoot.transform;
            }
        }
    }

    private void SyncWorldSlotRootsToUI(bool instant)
    {
        if (!mirrorDiceRigSlotsWithLiveUI) return;
        if (diceRig == null) return;

        if (_worldSlotOwners[0] == null && _worldSlotOwners[1] == null && _worldSlotOwners[2] == null)
            RebindWorldSlotOwnersFromCurrentOrder();

        Camera uiCamera = GetUICamera();
        Camera worldCameraToUse = GetWorldFollowCamera();
        if (worldCameraToUse == null) return;

        for (int i = 0; i < _worldSlotRoots.Length; i++)
        {
            Transform slotRoot = _worldSlotRoots[i];
            DiceDraggableUI owner = _worldSlotOwners[i];
            if (slotRoot == null || owner == null) continue;

            if (!owner.gameObject.activeInHierarchy)
                continue;

            if (!TryGetDiceUICenterWorldPosition(owner, uiCamera, worldCameraToUse, slotRoot.position, out Vector3 targetWorld))
                continue;

            if (instant)
                slotRoot.position = targetWorld;
            else
                slotRoot.position = Vector3.Lerp(slotRoot.position, targetWorld, 1f);
        }
    }

    private bool TryGetDiceUICenterWorldPosition(DiceDraggableUI owner, Camera uiCamera, Camera worldCameraToUse, Vector3 currentWorld, out Vector3 targetWorld)
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

    private void RefreshTurnManagerRef()
    {
        if (turnManager == null)
            turnManager = FindObjectOfType<TurnManager>(true);
    }
}
