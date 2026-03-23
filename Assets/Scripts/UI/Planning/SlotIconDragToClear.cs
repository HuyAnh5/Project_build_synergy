using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SlotIconDragToClear : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IDropHandler
{
    public TurnManager turn;
    public int slotIndex = 1; // 1..3
    public Image iconPreview;
    public bool enableTwoSlotGroupReorderDrag = true;

    private Canvas _canvas;
    private RectTransform _canvasRT;
    private Camera _uiCam;

    private RectTransform _ghostRT;
    private DiceEquipUIManager _diceUiManager;
    private bool _groupReorderDrag;

    private void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
        _canvasRT = _canvas.transform as RectTransform;
        _uiCam = (_canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : _canvas.worldCamera;
        EnsurePreviewInputForwarder();
    }

    public void SetVisualLaneIndex(int lane1Based)
    {
        slotIndex = Mathf.Clamp(lane1Based, 1, 3);
    }

    // Unequip by clicking the slot icon (not the skill icon)
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!turn || !turn.IsPlanning) return;
        if (!iconPreview || iconPreview.sprite == null) return;
        turn.ClearSlot(slotIndex);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!turn || !turn.IsPlanning) return;
        if (!iconPreview || iconPreview.sprite == null) return;

        _groupReorderDrag = ShouldUseTwoSlotGroupReorderDrag();
        CreateGhost();
        MoveGhost(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_ghostRT) MoveGhost(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_ghostRT) Destroy(_ghostRT.gameObject);
        _ghostRT = null;

        if (!turn || !turn.IsPlanning) return;

        if (_groupReorderDrag)
        {
            _groupReorderDrag = false;
            DiceEquipUIManager diceUiManager = GetDiceUiManager();
            if (diceUiManager != null)
                diceUiManager.TryMovePlannedTwoSlotGroup(slotIndex, eventData.position, eventData.pressEventCamera);
            return;
        }

        // If not dropped on any slot => clear
        var hitGo = eventData.pointerCurrentRaycast.gameObject;
        bool droppedOnSlot = hitGo != null && hitGo.GetComponentInParent<ActionSlotDrop>() != null;

        if (!droppedOnSlot)
            turn.ClearSlot(slotIndex);
    }

    private void CreateGhost()
    {
        var go = new GameObject("SlotDragGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        go.transform.SetParent(_canvas.transform, false);
        go.transform.SetAsLastSibling();

        _ghostRT = (RectTransform)go.transform;
        _ghostRT.sizeDelta = ((RectTransform)iconPreview.transform).rect.size;
        _ghostRT.pivot = new Vector2(0.5f, 0.5f);
        _ghostRT.anchorMin = new Vector2(0.5f, 0.5f);
        _ghostRT.anchorMax = new Vector2(0.5f, 0.5f);

        var img = go.GetComponent<Image>();
        img.sprite = iconPreview.sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;

        var cg = go.GetComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.alpha = 0.9f;
    }

    private void MoveGhost(Vector2 screenPos)
    {
        if (_ghostRT == null || _canvasRT == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRT, screenPos, _uiCam, out var localPoint);

        _ghostRT.anchoredPosition = localPoint;
    }

    public void OnDrop(PointerEventData eventData)
    {
        // Nếu iconPreview đang nằm trên slot và chặn raycast,
        // ta forward drop về ActionSlotDrop của slot cha để vẫn equip được.
        var drop = GetComponentInParent<ActionSlotDrop>();
        if (drop != null)
            drop.OnDrop(eventData);
    }

    private bool ShouldUseTwoSlotGroupReorderDrag()
    {
        if (!enableTwoSlotGroupReorderDrag || turn == null)
            return false;

        DiceEquipUIManager diceUiManager = GetDiceUiManager();
        if (diceUiManager == null || !diceUiManager.enableGroupedSkillDiceReorder)
            return false;

        if (!turn.TryGetPlannedGroupAtLane(slotIndex, out int anchor0, out _, out int span))
            return false;

        return span == 2 && anchor0 == slotIndex - 1;
    }

    private DiceEquipUIManager GetDiceUiManager()
    {
        if (_diceUiManager == null)
            _diceUiManager = FindObjectOfType<DiceEquipUIManager>();
        return _diceUiManager;
    }

    private void EnsurePreviewInputForwarder()
    {
        if (iconPreview == null)
            return;

        SlotIconPreviewInputForwarder forwarder = iconPreview.GetComponent<SlotIconPreviewInputForwarder>();
        if (forwarder == null)
            forwarder = iconPreview.gameObject.AddComponent<SlotIconPreviewInputForwarder>();

        forwarder.owner = this;
        iconPreview.raycastTarget = true;
    }
}
