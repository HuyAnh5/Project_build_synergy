using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Drag source for skill icons.
///
/// Backward compatible:
/// - Legacy path uses <see cref="skill"/> (SkillSO) exactly as before.
/// - New V2 path can bind to SkillDamageSO / SkillBuffDebuffSO / SkillPassiveSO without breaking old scenes.
/// </summary>
public class DraggableSkillIcon : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [Header("Legacy (SkillSO)")]
    public SkillSO skill;

    [Header("V2 (optional)")]
    public SkillDamageSO damageSkill;
    public SkillBuffDebuffSO buffDebuffSkill;
    public SkillPassiveSO passiveSkill;

    [Header("Runtime")]
    public TurnManager turn;

    [Range(0f, 1f)] public float inUseAlpha = 0.6f;

    private Canvas _canvas;
    private RectTransform _canvasRT;
    private Camera _uiCam;

    private Image _img;
    private CanvasGroup _cg;

    private RectTransform _ghostRT;

    public enum SkillIconKind
    {
        None,
        Legacy,
        Damage,
        BuffDebuff,
        Passive
    }

    public SkillIconKind Kind
    {
        get
        {
            // Keep legacy priority so old prefabs behave exactly the same.
            if (skill != null) return SkillIconKind.Legacy;
            if (damageSkill != null) return SkillIconKind.Damage;
            if (buffDebuffSkill != null) return SkillIconKind.BuffDebuff;
            if (passiveSkill != null) return SkillIconKind.Passive;
            return SkillIconKind.None;
        }
    }

    public bool IsV2ActiveSkill => Kind == SkillIconKind.Damage || Kind == SkillIconKind.BuffDebuff;
    public bool IsV2PassiveOnly => Kind == SkillIconKind.Passive;

    public Sprite IconSprite
    {
        get
        {
            if (skill != null) return skill.icon;
            if (damageSkill != null) return damageSkill.icon;
            if (buffDebuffSkill != null) return buffDebuffSkill.icon;
            if (passiveSkill != null) return passiveSkill.icon;
            return null;
        }
    }

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
        if (!_img) return;

        var spr = IconSprite;
        if (spr != null)
        {
            _img.sprite = spr;
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

    // Click = auto equip (legacy path only for now; V2 click-to-equip will be wired when TurnManager/PlanBoard are refactored)
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!turn || !turn.IsPlanning) return;
        if (skill == null) return; // keep behavior unchanged for legacy
        turn.TryAutoAssignFromClick(skill);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!turn || !turn.IsPlanning) return;
        if (IsV2PassiveOnly) return; // Passive cannot be dragged.
        if (Kind == SkillIconKind.None) return;

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

    public bool TryGetLegacySkill(out SkillSO s)
    {
        s = skill;
        return s != null;
    }

    public bool TryGetV2Damage(out SkillDamageSO s)
    {
        s = (skill == null) ? damageSkill : null; // do not mix with legacy
        return s != null;
    }

    public bool TryGetV2BuffDebuff(out SkillBuffDebuffSO s)
    {
        s = (skill == null) ? buffDebuffSkill : null;
        return s != null;
    }

    public bool TryGetV2Passive(out SkillPassiveSO s)
    {
        s = (skill == null && damageSkill == null && buffDebuffSkill == null) ? passiveSkill : null;
        return s != null;
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
