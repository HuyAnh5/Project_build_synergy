using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// UI icon for a skill. Source of truth should be RunInventoryManager.
/// - If Bind To Inventory Slot = true: skill is resolved from inventory (Fixed/Owned + index)
/// - Else: use Skill Asset Override (single ScriptableObject)
///
/// Supports drag/click equip for active skills (SkillDamageSO / SkillBuffDebuffSO).
/// Passive (SkillPassiveSO) is NOT draggable and NOT click-to-equip.
/// </summary>
public class DraggableSkillIcon : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [Title("Source")]
    [Tooltip("If enabled, this icon always reads the skill from RunInventoryManager (Fixed/Owned slot).")]
    [SerializeField] private bool bindToInventorySlot = true;

    [ShowIf(nameof(bindToInventorySlot))]
    [SerializeField] private RunInventoryManager inventory;

    public enum InventorySkillSource { Fixed, Owned }

    [ShowIf(nameof(bindToInventorySlot))]
    [SerializeField] private InventorySkillSource inventorySource = InventorySkillSource.Owned;

    [ShowIf(nameof(bindToInventorySlot))]
    [Min(0)]
    [SerializeField] private int inventoryIndex = 0;

    [HideIf(nameof(bindToInventorySlot))]
    [Tooltip("Used only when not bound to inventory. Single reference, no legacy/new split.")]
    [SerializeField] private ScriptableObject skillAssetOverride;

    [Title("Turn")]
    [SerializeField] private TurnManager turn;

    [Title("Visual")]
    [Range(0f, 1f)]
    [SerializeField] private float inUseAlpha = 0.6f;
    [Range(0f, 1f)]
    [SerializeField] private float unavailableAlpha = 0.4f;
    [SerializeField] private float invalidDropReturnDuration = 0.16f;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private SelfCastDropZone selfCastZone;

    private Canvas _canvas;
    private RectTransform _canvasRT;
    private Camera _uiCam;
    private Image _img;
    private CanvasGroup _cg;
    private RectTransform _ghostRT;
    private ScriptableObject _resolvedAsset;
    private bool _dropAccepted;
    private Vector2 _ghostHomeAnchoredPos;
    private bool _inUse;
    private bool _castable = true;

    private void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
        _canvasRT = _canvas.transform as RectTransform;
        _uiCam = (_canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : _canvas.worldCamera;

        _img = GetComponent<Image>();
        _cg = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        if (nameText == null)
            nameText = GetComponentInChildren<TMP_Text>(includeInactive: true);
        if (selfCastZone == null)
            selfCastZone = FindObjectOfType<SelfCastDropZone>(true);

        Refresh();
        SetInUse(false);
        SetCastable(true);
    }

    private void OnEnable()
    {
        if (bindToInventorySlot && inventory != null)
            inventory.InventoryChanged += OnInventoryChanged;

        Refresh();
    }

    private void OnDisable()
    {
        if (inventory != null)
            inventory.InventoryChanged -= OnInventoryChanged;
    }

    private void OnInventoryChanged()
    {
        Refresh();
    }

    public bool IsPassive
    {
        get
        {
            var a = GetSkillAsset();
            return a is SkillPassiveSO;
        }
    }

    public ScriptableObject GetSkillAsset()
    {
        if (bindToInventorySlot && inventory != null)
        {
            var src = (inventorySource == InventorySkillSource.Fixed)
                ? RunInventoryManager.SkillSource.Fixed
                : RunInventoryManager.SkillSource.Owned;

            _resolvedAsset = inventory.GetSkill(src, inventoryIndex);
            return _resolvedAsset;
        }

        _resolvedAsset = skillAssetOverride;
        return _resolvedAsset;
    }

    private Sprite GetIcon()
    {
        var a = GetSkillAsset();
        if (a is SkillDamageSO ds) return ds.icon;
        if (a is SkillBuffDebuffSO bd) return bd.icon;
        if (a is SkillPassiveSO ps) return ps.icon;
        return null;
    }

    public void Refresh()
    {
        if (_img != null)
        {
            _img.sprite = GetIcon();
            _img.preserveAspect = true;
        }
        RefreshLabel();
        ApplyVisualState();
    }

    public void SetInUse(bool inUse)
    {
        _inUse = inUse;
        ApplyVisualState();
    }

    public void SetCastable(bool castable)
    {
        _castable = castable;
        ApplyVisualState();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!turn || !turn.CanInteractWithSkills) return;

        var a = GetSkillAsset();
        if (a == null) return;
        if (a is SkillPassiveSO) return;
        if (!CanDragCurrentSkill()) return;

        if (a is SkillDamageSO ds) { turn.TryAutoAssignFromClick(ds); return; }
        if (a is SkillBuffDebuffSO bd) { turn.TryAutoAssignFromClick(bd); return; }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!turn || !turn.CanInteractWithSkills) return;

        var a = GetSkillAsset();
        if (a == null) return;
        if (a is SkillPassiveSO) return;
        if (!CanDragCurrentSkill()) return;

        _dropAccepted = false;
        CreateGhost();
        MoveGhost(eventData.position);
        _cg.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_ghostRT != null)
            MoveGhost(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _cg.blocksRaycasts = true;
        if (_ghostRT == null) return;

        if (!_dropAccepted &&
            eventData != null &&
            IsSelfTargetSkill(GetSkillAsset()) &&
            selfCastZone != null &&
            selfCastZone.ContainsScreenPoint(eventData.position, _uiCam))
        {
            _dropAccepted = turn != null && turn.TryCastDraggedSkillToSelf(GetSkillAsset());
        }

        if (_dropAccepted)
        {
            Destroy(_ghostRT.gameObject);
            _ghostRT = null;
            return;
        }

        _ghostRT.DOKill();
        _ghostRT.DOAnchorPos(_ghostHomeAnchoredPos, invalidDropReturnDuration)
            .SetEase(Ease.OutCubic)
            .OnComplete(() =>
            {
                if (_ghostRT != null)
                    Destroy(_ghostRT.gameObject);
                _ghostRT = null;
            });
    }

    public void NotifyDropAccepted()
    {
        _dropAccepted = true;
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

        RectTransform sourceRt = transform as RectTransform;
        if (sourceRt != null && _canvasRT != null)
        {
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(_uiCam, sourceRt.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRT, screenPos, _uiCam, out _ghostHomeAnchoredPos);
        }
        else
        {
            _ghostHomeAnchoredPos = Vector2.zero;
        }

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

    public void SetBindToInventory(RunInventoryManager inv, bool isFixed, int index)
    {
        bindToInventorySlot = true;
        inventory = inv;
        inventorySource = isFixed ? InventorySkillSource.Fixed : InventorySkillSource.Owned;
        inventoryIndex = index;
        Refresh();
    }

    private bool CanDragCurrentSkill()
    {
        if (turn == null) return false;
        ScriptableObject asset = GetSkillAsset();
        if (asset == null || asset is SkillPassiveSO) return false;
        return turn.CanPrototypeCastSkillNow(asset);
    }

    public bool IsSelfTargetSkillAsset()
        => IsSelfTargetSkill(GetSkillAsset());

    public static bool IsSelfTargetSkill(ScriptableObject asset)
    {
        switch (asset)
        {
            case SkillDamageSO damage:
                return damage.target == SkillTargetRule.Self;
            case SkillBuffDebuffSO buffDebuff:
                return buffDebuff.target == SkillTargetRule.Self;
            default:
                return false;
        }
    }

    private void RefreshLabel()
    {
        if (nameText == null)
            return;

        string label = string.Empty;
        if (bindToInventorySlot && inventory != null)
        {
            var src = inventorySource == InventorySkillSource.Fixed
                ? RunInventoryManager.SkillSource.Fixed
                : RunInventoryManager.SkillSource.Owned;
            label = inventory.GetSkillDisplayName(src, inventoryIndex);
        }
        else
        {
            label = ResolveDisplayName(GetSkillAsset());
        }

        nameText.text = label;
    }

    private static string ResolveDisplayName(ScriptableObject asset)
    {
        switch (asset)
        {
            case SkillDamageSO damage:
                if (damage.coreAction == CoreAction.BasicStrike)
                    return "Basic Attack";
                if (damage.coreAction == CoreAction.BasicGuard)
                    return "Basic Guard";
                return damage.displayName;

            case SkillBuffDebuffSO buffDebuff:
                return buffDebuff.displayName;

            case SkillPassiveSO passive:
                return passive.displayName;

            default:
                return string.Empty;
        }
    }

    private void ApplyVisualState()
    {
        if (_img == null) return;

        float alpha = _inUse ? inUseAlpha : 1f;
        if (!_castable)
            alpha *= unavailableAlpha;

        Color c = _img.color;
        c.a = alpha;
        _img.color = c;

        if (_cg != null)
            _cg.blocksRaycasts = _castable;
    }
}
