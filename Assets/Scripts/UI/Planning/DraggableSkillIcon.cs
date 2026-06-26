using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// UI icon for a skill. Source of truth should be RunInventoryManager.
/// - If Bind To Inventory Slot = true: skill is resolved from inventory (slot index)
/// - Else: use Skill Asset Override (single ScriptableObject)
///
/// Supports drag/click equip for active skills (SkillDamageSO / SkillBuffDebuffSO).
/// Passive (SkillPassiveSO) is NOT draggable and NOT click-to-equip.
/// </summary>
public partial class DraggableSkillIcon : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, ISkillTooltipSource
{
    private const string AddedValueColorToken = "#5CCBFF";
    private const string ReducedValueColorToken = "#FF5C7A";
    private const string IncreasedValueColorToken = "#67E88D";
    private const string FocusBadgeName = "FocusCostBadge";
    private const string SlotBadgeName = "SlotCostBadge";
    private const string ElementBadgeName = "ElementBadge";
    private const int IdleMetadataRefreshIntervalFrames = 6;

    [Title("Source")]
    [Tooltip("If enabled, this icon always reads the skill from RunInventoryManager.")]
    [SerializeField] private bool bindToInventorySlot = true;

    [ShowIf(nameof(bindToInventorySlot))]
    [SerializeField] private RunInventoryManager inventory;

    [ShowIf(nameof(bindToInventorySlot))]
    [SerializeField] private RunInventoryManager.SkillSource inventorySource = RunInventoryManager.SkillSource.Owned;

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
    [SerializeField] private SkillUiIconLibrarySO iconLibrary;
    [SerializeField] private Image focusCostBadgeBackground;
    [SerializeField] private TMP_Text focusCostBadgeText;
    [SerializeField] private Image slotCostBadgeBackground;
    [SerializeField] private Image slotCostBadgeIcon;
    [SerializeField] private TMP_Text slotCostBadgeText;
    [SerializeField] private Image elementBadgeBackground;
    [SerializeField] private Image elementBadgeIcon;
    [SerializeField] private Image skillBackgroundImage;
    [SerializeField] private SkillSlotLayout skillSlotLayout;

    [Title("Active Icon Outer Glow")]
    [SerializeField] private bool enableActiveAura = true;
    [SerializeField] private Color activeAuraBrightColor = new Color(1f, 0.82f, 0.28f, 0.9f);
    [SerializeField] private Color activeAuraWaveColor = new Color(1f, 0.62f, 0.12f, 0.34f);
    [SerializeField] private float activeAuraBrightSize = 2f;
    [SerializeField] private float activeAuraWaveSize = 8f;
    [SerializeField] private float activeAuraWaveSeconds = 1.2f;
    [Tooltip("Image source used only by the active aura. Assign manually to avoid aura inheriting the skill art image.")]
    [SerializeField] private Image activeAuraSourceImage;
    [SerializeField] private Color transientAffectedAuraBrightColor = new Color(0.36f, 0.88f, 1f, 0.95f);
    [SerializeField] private Color transientAffectedAuraWaveColor = new Color(0.36f, 0.88f, 1f, 0.5f);
    [SerializeField] private float transientAffectedAuraDuration = 0.22f;
    [SerializeField] private float transientAffectedAuraWaveSize = 10f;

    private static SkillUiIconLibrarySO _sharedIconLibrary;

    private Canvas _canvas;
    private RectTransform _canvasRT;
    private Camera _uiCam;
    private Image _img;
    private CanvasGroup _cg;
    private RectTransform _ghostRT;
    private Image _ghostImage;
    private CanvasGroup _ghostCanvasGroup;
    private ScriptableObject _resolvedAsset;
    private bool _dropAccepted;
    private Vector2 _ghostHomeAnchoredPos;
    private bool _inUse;
    private bool _castable = true;
    private bool _dragRegistered;
    private ScriptableObject _lastVisualAsset;
    private Sprite _lastVisualIcon;
    private string _lastVisualName;
    private int _lastVisualFocusCost = int.MinValue;
    private int _lastVisualSlotsRequired = int.MinValue;
    private bool _lastVisualHasElement;
    private ElementType _lastVisualElement = ElementType.Neutral;
    private SkillUiIconLibrarySO _lastResolvedIconLibrary;
    private readonly List<Image> _activeAuraWaves = new List<Image>();
    private Image _activeAuraRim;
    private readonly List<Tween> _activeAuraTweens = new List<Tween>();
    private bool _activeAuraTweensRunning;
    private bool _transientAffectedAuraRunning;
    private Sequence _transientAffectedAuraSequence;
    private bool _isActiveRuntimeSkill;
    private bool _lastActiveRuntimeSkill;
    private int _activeRuntimeTurns;
    private int _lastActiveRuntimeTurns = int.MinValue;
    private float _lastAuraWaveSeconds = -1f;
    private float _lastAuraWaveSize = -1f;
    private float _lastAuraBrightSize = -1f;

    private SkillIconPreviewController _previewController;
    private bool _pointerInside;
    private RectTransform _previewHoverContainer;
    private int _nextIdleMetadataRefreshFrame;

    // --- Click-to-Select ---
    private bool _selected;
    private Coroutine _blinkCoroutine;
    private static int _lastAffectedPulseFrame = -1;
    private static readonly Color SelectedBlinkColorA = new Color(1f, 0.92f, 0.3f, 1f);  // đỉnh sáng
    private static readonly Color SelectedBlinkColorB = new Color(1f, 0.65f, 0.1f, 0.5f); // đỉnh mờ

    partial void RefreshTesterPassiveVisualState();
    partial void HandleTesterPassivePointerClick(SkillPassiveSO passive);
    partial void ApplyTesterPassiveVisualState();

    private void Awake()
    {
        DraggableSkillIconRegistry.Register(this);

        _canvas = GetComponentInParent<Canvas>();
        _canvasRT = _canvas.transform as RectTransform;
        _uiCam = (_canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : _canvas.worldCamera;
        _previewHoverContainer = transform.parent as RectTransform;

        _img = GetComponent<Image>();
        _cg = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        if (skillSlotLayout == null)
            skillSlotLayout = GetComponent<SkillSlotLayout>();
        ApplyLayoutBindings();
        if (nameText == null)
            nameText = GetComponentInChildren<TMP_Text>(includeInactive: true);
        if (skillBackgroundImage == null)
            skillBackgroundImage = GetComponent<Image>();
        if (selfCastZone == null)
            selfCastZone = SelfCastDropZoneRegistry.Get();
        ResolveTurnManager();
        if (iconLibrary != null)
            _sharedIconLibrary = iconLibrary;
        _previewController = new SkillIconPreviewController(turn, selfCastZone, _uiCam, GetSkillAsset, GetPreviewDieValue);

        EnsureCostBadgeUi();
        EnsureActiveAuraUi();
        Refresh();
        SetInUse(false);
        SetCastable(true);
    }

    private void OnEnable()
    {
        DraggableSkillIconRegistry.Register(this);

        if (bindToInventorySlot && inventory != null)
            inventory.InventoryChanged += OnInventoryChanged;
        UiDragState.DragStateChanged += HandleUiDragStateChanged;

        ResolveTurnManager();
        Refresh();
    }

    private void OnDisable()
    {
        if (inventory != null)
            inventory.InventoryChanged -= OnInventoryChanged;
        UiDragState.DragStateChanged -= HandleUiDragStateChanged;

        if (_dragRegistered)
        {
            UiDragState.EndDrag(this);
            _dragRegistered = false;
        }

        // Deselect nếu icon bị disable
        if (_selected)
            UiDragState.DeselectSkill();

        ReleaseGhost();

        StopBlinkCoroutine();
        _pointerInside = false;
        _transientAffectedAuraSequence?.Kill();
        _transientAffectedAuraSequence = null;
        _transientAffectedAuraRunning = false;
        StopActiveAuraTweens();
        SkillTooltipUI.HideCurrent();
    }

    private void OnDestroy()
    {
        DraggableSkillIconRegistry.Unregister(this);
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
            _resolvedAsset = inventory.GetSkill(inventorySource, inventoryIndex);
            return _resolvedAsset;
        }

        _resolvedAsset = skillAssetOverride;
        return _resolvedAsset;
    }

    private bool IsPointerInsidePreviewHoverContainer(PointerEventData eventData)
    {
        if (_previewHoverContainer == null)
            return false;

        Vector2 screenPoint = eventData != null ? eventData.position : (Vector2)Input.mousePosition;
        return RectTransformUtility.RectangleContainsScreenPoint(_previewHoverContainer, screenPoint, _uiCam);
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
        if (skillSlotLayout == null)
            skillSlotLayout = GetComponent<SkillSlotLayout>();

        ApplyLayoutBindings();
        EnsureCostBadgeUi();
        if (_img != null)
        {
            _img.sprite = GetIcon();
            _img.preserveAspect = true;
        }
        RefreshLabel();
        RefreshCostBadges();
        RefreshElementBadge();
        RefreshTesterPassiveVisualState();
        RefreshActiveRuntimeState();
        ApplyVisualState();
        CaptureVisualSnapshot();
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

    private void RefreshLabel()
    {
        if (nameText == null)
            return;

        if (bindToInventorySlot && inventory != null)
        {
            nameText.text = inventory.GetSkillDisplayName(inventorySource, inventoryIndex);
        }
        else
        {
            nameText.text = SkillUiMetadataUtility.ResolveDisplayName(GetSkillAsset());
        }
    }

    public bool TryGetSkillTooltip(out Canvas canvas, out RectTransform target, out ScriptableObject asset, out SkillRuntime runtime)
    {
        canvas = _canvas;
        target = transform as RectTransform;
        asset = GetSkillAsset();
        runtime = null;
        if (turn != null && asset != null && !(asset is SkillPassiveSO))
            turn.TryGetPrototypeSkillTooltipRuntime(asset, out runtime);

        return canvas != null && target != null && asset != null;
    }

    // ---------------------------
    // ---------------------------
    // Resource + Target Preview
    // ---------------------------

    public void ShowResourcePreview(ScriptableObject asset)
        => _previewController?.ShowResourcePreview(asset);

    private void ClearResourcePreview()
        => _previewController?.ClearResourcePreview();

    private bool ReleaseResourcePreviewOwnership()
        => _previewController != null && _previewController.ReleaseResourcePreviewOwnership();

    private void Update()
    {
        if (ShouldRefreshVisualMetadataThisFrame())
            RefreshIfVisualMetadataChanged();

        TickActiveAura();
        ClearHoverResourcePreviewIfPointerLeftBridge();
        _previewController?.Tick();
    }

    private bool ShouldRefreshVisualMetadataThisFrame()
    {
        if (_pointerInside || _selected || _dragRegistered)
            return true;

        if (Time.frameCount < _nextIdleMetadataRefreshFrame)
            return false;

        _nextIdleMetadataRefreshFrame = Time.frameCount + IdleMetadataRefreshIntervalFrames;
        return true;
    }

    private void ClearHoverResourcePreviewIfPointerLeftBridge()
    {
        if (_pointerInside || _selected || UiDragState.IsDragging)
            return;

        if (IsPointerInsidePreviewHoverContainer(null) || SkillTooltipUI.IsPointerOverCurrentTooltip())
            return;

        DraggableSkillIcon selected = UiDragState.SelectedSkill;
        if (selected != null)
        {
            if (ReleaseResourcePreviewOwnership())
            {
                ScriptableObject selectedAsset = selected.GetSkillAsset();
                if (selectedAsset != null)
                    selected.ShowResourcePreview(selectedAsset);
            }

            return;
        }

        ClearResourcePreview();
    }

    private void UpdateTargetPreviewUnderCursor(PointerEventData eventData)
        => _previewController?.UpdateTargetPreviewUnderCursor(eventData);

    /// <summary>Public accessor so TargetClickable2D can get the expected die value for hover preview.</summary>
    public int GetPublicPreviewDieValue(SkillRuntime rt) => GetPreviewDieValue(rt);
    private void ApplyVisualState()
    {
        if (_img == null) return;

        float alpha = _inUse ? inUseAlpha : 1f;
        if (!_castable)
            alpha *= unavailableAlpha;

        Color c = _img.color;
        c.a = alpha;
        _img.color = c;

        ApplyTesterPassiveVisualState();

        ApplyActiveAuraVisibility();

        if (_cg != null && !_dragRegistered)
            _cg.blocksRaycasts = true;
    }

    public void SetIconLibrary(SkillUiIconLibrarySO library)
    {
        iconLibrary = library;
        if (iconLibrary != null)
            _sharedIconLibrary = iconLibrary;
        Refresh();
    }

    private SkillUiIconLibrarySO ResolveIconLibrary()
    {
        if (iconLibrary != null)
        {
            _sharedIconLibrary = iconLibrary;
            return iconLibrary;
        }

        if (_sharedIconLibrary != null)
            return _sharedIconLibrary;

        ActorWorldUI[] worldUis = ActorWorldUiRegistry.GetAllSnapshot();
        for (int i = 0; i < worldUis.Length; i++)
        {
            if (worldUis[i] != null && worldUis[i].iconLibrary != null)
            {
                _sharedIconLibrary = worldUis[i].iconLibrary;
                return _sharedIconLibrary;
            }
        }

        return null;
    }

    private void RefreshIfVisualMetadataChanged()
    {
        ScriptableObject asset = GetSkillAsset();
        Sprite currentIcon = GetIcon();
        string currentName = bindToInventorySlot && inventory != null
            ? inventory.GetSkillDisplayName(inventorySource, inventoryIndex)
            : SkillUiMetadataUtility.ResolveDisplayName(asset);

        bool hasCosts = SkillUiMetadataUtility.TryGetSkillCosts(asset, out int focusCost, out int slotsRequired);
        if (!hasCosts)
        {
            focusCost = -1;
            slotsRequired = -1;
        }

        bool hasElement = SkillUiMetadataUtility.TryGetElementType(asset, out ElementType element);
        SkillUiIconLibrarySO resolvedIconLibrary = ResolveIconLibrary();
        RefreshActiveRuntimeState();

        if (asset == _lastVisualAsset &&
            currentIcon == _lastVisualIcon &&
            string.Equals(currentName, _lastVisualName) &&
            focusCost == _lastVisualFocusCost &&
            slotsRequired == _lastVisualSlotsRequired &&
            hasElement == _lastVisualHasElement &&
            (!hasElement || element == _lastVisualElement) &&
            resolvedIconLibrary == _lastResolvedIconLibrary &&
            _isActiveRuntimeSkill == _lastActiveRuntimeSkill &&
            _activeRuntimeTurns == _lastActiveRuntimeTurns)
        {
            return;
        }

        Refresh();
    }

    private void CaptureVisualSnapshot()
    {
        ScriptableObject asset = GetSkillAsset();
        _lastVisualAsset = asset;
        _lastVisualIcon = GetIcon();
        _lastVisualName = bindToInventorySlot && inventory != null
            ? inventory.GetSkillDisplayName(inventorySource, inventoryIndex)
            : SkillUiMetadataUtility.ResolveDisplayName(asset);

        if (SkillUiMetadataUtility.TryGetSkillCosts(asset, out int focusCost, out int slotsRequired))
        {
            _lastVisualFocusCost = focusCost;
            _lastVisualSlotsRequired = slotsRequired;
        }
        else
        {
            _lastVisualFocusCost = -1;
            _lastVisualSlotsRequired = -1;
        }

        _lastVisualHasElement = SkillUiMetadataUtility.TryGetElementType(asset, out _lastVisualElement);
        if (!_lastVisualHasElement)
            _lastVisualElement = ElementType.Neutral;

        _lastResolvedIconLibrary = ResolveIconLibrary();
        _lastActiveRuntimeSkill = _isActiveRuntimeSkill;
        _lastActiveRuntimeTurns = _activeRuntimeTurns;
    }

    private void ResolveTurnManager()
    {
        if (turn == null)
            turn = TurnManagerRegistry.Get();
    }

    private void HandleUiDragStateChanged()
    {
        if (UiDragState.IsDragging)
        {
            SkillTooltipUI.HideCurrent();
            return;
        }

        if (!_pointerInside)
            return;

        ScriptableObject asset = GetSkillAsset();
        if (asset == null)
            return;

        SkillTooltipUI.Show(this);
        ShowResourcePreview(asset);
    }

    private void RefreshActiveRuntimeState()
    {
        ResolveTurnManager();
        ScriptableObject asset = GetSkillAsset();
        _isActiveRuntimeSkill = SkillActiveStateUtility.IsSkillActiveOnPlayer(asset, turn != null ? turn.player : null, out _activeRuntimeTurns);
    }

    private int GetPreviewDieValue(SkillRuntime rt)
    {
        if (turn == null || turn.diceRig == null || rt == null || !turn.diceRig.HasRolledThisTurn)
            return 0;

        ScriptableObject asset = GetSkillAsset();
        if (turn.TryGetPrototypeSkillPreviewDieValue(asset, rt, out int committedPreviewValue))
            return committedPreviewValue;

        var spentDice = new HashSet<DiceSpinnerGeneric>();
        if (turn.SpentDiceThisTurn != null)
        {
            foreach (DiceSpinnerGeneric die in turn.SpentDiceThisTurn)
                spentDice.Add(die);
        }

        int value = 0;
        int slotsNeeded = Mathf.Clamp(rt.slotsRequired, 1, 3);
        int found = 0;

        for (int i = 0; i < 3 && found < slotsNeeded; i++)
        {
            if (!turn.diceRig.IsSlotActive(i))
                continue;

            DiceSpinnerGeneric die = turn.diceRig.GetDice(i);
            if (die == null || spentDice.Contains(die))
                continue;

            value += turn.diceRig.GetResolvedContribution(i, turn.player, rt.element);
            found++;
        }

        return value;
    }
}

internal static class DraggableSkillIconRegistry
{
    private static readonly List<DraggableSkillIcon> Registered = new List<DraggableSkillIcon>(32);
    private static readonly List<DraggableSkillIcon> Snapshot = new List<DraggableSkillIcon>(32);
    private static DraggableSkillIcon[] _cachedSnapshot = System.Array.Empty<DraggableSkillIcon>();
    private static bool _snapshotDirty = true;
    private static bool _initializedFromScene;

    public static void Register(DraggableSkillIcon icon)
    {
        if (icon == null || Registered.Contains(icon))
            return;

        Registered.Add(icon);
        _snapshotDirty = true;
    }

    public static void Unregister(DraggableSkillIcon icon)
    {
        if (icon == null)
            return;

        if (Registered.Remove(icon))
            _snapshotDirty = true;
    }

    public static DraggableSkillIcon[] GetAllSnapshot()
    {
        EnsureInitializedFromScene();

        if (!_snapshotDirty)
            return _cachedSnapshot;

        Snapshot.Clear();

        for (int i = Registered.Count - 1; i >= 0; i--)
        {
            DraggableSkillIcon icon = Registered[i];
            if (icon == null)
            {
                Registered.RemoveAt(i);
                _snapshotDirty = true;
                continue;
            }

            Snapshot.Add(icon);
        }

        _cachedSnapshot = Snapshot.Count > 0 ? Snapshot.ToArray() : System.Array.Empty<DraggableSkillIcon>();
        _snapshotDirty = false;
        return _cachedSnapshot;
    }

    private static void EnsureInitializedFromScene()
    {
        if (_initializedFromScene)
            return;

        _initializedFromScene = true;
#if UNITY_2023_1_OR_NEWER
        DraggableSkillIcon[] sceneIcons = UnityEngine.Object.FindObjectsByType<DraggableSkillIcon>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        DraggableSkillIcon[] sceneIcons = UnityEngine.Object.FindObjectsOfType<DraggableSkillIcon>(true);
#endif
        if (sceneIcons == null)
            return;

        for (int i = 0; i < sceneIcons.Length; i++)
            Register(sceneIcons[i]);
    }
}

