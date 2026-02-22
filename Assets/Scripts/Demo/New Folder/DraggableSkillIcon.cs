using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Sirenix.OdinInspector;

/// <summary>
/// UI icon for a skill. Source of truth should be RunInventoryManager.
/// - If Bind To Inventory Slot = true: skill is resolved from inventory (Fixed/Owned + index)
/// - Else: use Skill Asset Override (single ScriptableObject)
///
/// Supports drag/click equip for active skills (SkillDamageSO / SkillBuffDebuffSO / legacy SkillSO if you still have it).
/// Passive (SkillPassiveSO) is NOT draggable and NOT click-to-equip.
/// </summary>
public class DraggableSkillIcon : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    // -------------------------
    // Binding (Inspector clean)
    // -------------------------
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

    // -------------------------
    // Runtime
    // -------------------------
    private Canvas _canvas;
    private RectTransform _canvasRT;
    private Camera _uiCam;

    private Image _img;
    private CanvasGroup _cg;

    private RectTransform _ghostRT;

    private ScriptableObject _resolvedAsset;

    // -------------------------
    // Unity
    // -------------------------
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

    // -------------------------
    // Public helpers (keep functions)
    // -------------------------
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
        // 1) Inventory slot binding (source of truth)
        if (bindToInventorySlot && inventory != null)
        {
            // ✅ Use RunInventoryManager API (no FixedSkills/OwnedSkills properties needed)
            var src = (inventorySource == InventorySkillSource.Fixed)
                ? RunInventoryManager.SkillSource.Fixed
                : RunInventoryManager.SkillSource.Owned;

            _resolvedAsset = inventory.GetSkill(src, inventoryIndex);
            return _resolvedAsset;
        }

        // 2) Single override reference
        _resolvedAsset = skillAssetOverride;
        return _resolvedAsset;
    }


    private Sprite GetIcon()
    {
        var a = GetSkillAsset();
        if (a is SkillDamageSO ds) return ds.icon;
        if (a is SkillBuffDebuffSO bd) return bd.icon;
        if (a is SkillPassiveSO ps) return ps.icon;

        // Optional legacy support if you still keep SkillSO in project:
        if (a is SkillSO ls) return ls.icon;

        return null;
    }

    public void Refresh()
    {
        if (!_img) return;
        _img.sprite = GetIcon();
        _img.preserveAspect = true;

        // If slot is empty, you can hide icon visually (optional):
        // gameObject.SetActive(_img.sprite != null);
    }

    public void SetInUse(bool inUse)
    {
        if (!_img) return;
        var c = _img.color;
        c.a = inUse ? inUseAlpha : 1f;
        _img.color = c;
    }

    // -------------------------
    // Click = auto equip (ONLY active)
    // -------------------------
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!turn || !turn.IsPlanning) return;

        var a = GetSkillAsset();
        if (a == null) return;
        if (a is SkillPassiveSO) return;

        // Call the overloads you already use in your TurnManager.
        if (a is SkillDamageSO ds) { turn.TryAutoAssignFromClick(ds); return; }
        if (a is SkillBuffDebuffSO bd) { turn.TryAutoAssignFromClick(bd); return; }
        if (a is SkillSO ls) { turn.TryAutoAssignFromClick(ls); return; }
    }

    // -------------------------
    // Drag
    // -------------------------
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!turn || !turn.IsPlanning) return;

        var a = GetSkillAsset();
        if (a == null) return;
        if (a is SkillPassiveSO) return;

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

    public void SetBindToInventory(RunInventoryManager inv, bool isFixed, int index)
    {
        bindToInventorySlot = true;
        inventory = inv;
        inventorySource = isFixed
            ? InventorySkillSource.Fixed
            : InventorySkillSource.Owned;
        inventoryIndex = index;
        Refresh();
    }
}
