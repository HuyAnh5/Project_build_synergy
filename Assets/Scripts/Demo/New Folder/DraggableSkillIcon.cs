using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableSkillIcon : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    public SkillSO skill;
    public TurnManager turn;

    [Range(0f, 1f)] public float inUseAlpha = 0.6f;

    private Canvas _canvas;
    private RectTransform _canvasRT;
    private Camera _uiCam;

    private Image _img;
    private CanvasGroup _cg;

    private RectTransform _ghostRT;

    private void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
        _canvasRT = _canvas.transform as RectTransform;
        _uiCam = (_canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : _canvas.worldCamera;

        _img = GetComponent<Image>();
        _cg = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

        Refresh();
        SetInUse(false);
    }

    public void Refresh()
    {
        if (_img && skill && skill.icon)
        {
            _img.sprite = skill.icon;
            _img.preserveAspect = true;
        }
    }

    public void SetInUse(bool inUse)
    {
        if (!_img) return;
        var c = _img.color;
        c.a = inUse ? inUseAlpha : 1f;
        _img.color = c;
    }

    // Click = auto equip (still allows duplicates)
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!turn || !turn.IsPlanning) return;
        turn.TryAutoAssignFromClick(skill);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!turn || !turn.IsPlanning) return;

        CreateGhost();
        MoveGhost(eventData.position);

        // Let drop zones receive raycasts
        _cg.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_ghostRT) MoveGhost(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_ghostRT) Destroy(_ghostRT.gameObject);
        _ghostRT = null;

        _cg.blocksRaycasts = true;
    }

    private void CreateGhost()
    {
        var go = new GameObject("DragGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        go.transform.SetParent(_canvas.transform, false);
        go.transform.SetAsLastSibling();

        _ghostRT = (RectTransform)go.transform;
        _ghostRT.sizeDelta = ((RectTransform)transform).rect.size;
        _ghostRT.pivot = new Vector2(0.5f, 0.5f);
        _ghostRT.anchorMin = new Vector2(0.5f, 0.5f);
        _ghostRT.anchorMax = new Vector2(0.5f, 0.5f);

        var img = go.GetComponent<Image>();
        img.sprite = _img ? _img.sprite : null;
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

}
