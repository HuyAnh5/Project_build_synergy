using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public partial class ActorWorldUI : MonoBehaviour
{
    private const int DefaultStatusSlotCount = 8;
    private const int IdleWorldTooltipRefreshIntervalFrames = 3;

    public enum WorldUiTooltipSpawnDirection
    {
        Right = 0,
        Left = 1
    }

    [Serializable]
    public sealed class StatusIconSlot
    {
        public RectTransform root;
        public Image background;
        public Image iconImage;
        public TMP_Text shortLabelText;
        public TMP_Text valueText;
    }

    private readonly struct StatusVisualData
    {
        public readonly Sprite sprite;
        public readonly string shortLabel;
        public readonly string valueText;
        public readonly Color backgroundColor;

        public StatusVisualData(Sprite sprite, string shortLabel, string valueText, Color backgroundColor)
        {
            this.sprite = sprite;
            this.shortLabel = shortLabel;
            this.valueText = valueText;
            this.backgroundColor = backgroundColor;
        }
    }

    [Header("Bind")]
    public CombatActor actor;

    [Header("Follow")]
    // Đã bỏ localOffset để tôn trọng Transform Position của Unity

    [Header("Root")]
    public RectTransform worldCanvasRoot;
    public Canvas worldCanvas;
    public CanvasGroup worldCanvasGroup;
    public RectTransform tooltipAnchorRoot;
    public RectTransform tooltipBottomLimitRoot;
    public WorldUiTooltipSpawnDirection tooltipSpawnDirection = WorldUiTooltipSpawnDirection.Right;
    public Vector2 rootSize = new Vector2(220f, 156f);
    public Vector3 worldCanvasScale = new Vector3(0.01f, 0.01f, 0.01f);

    [Header("Preview Dummy")]
    public RectTransform previewDummyRoot;
    public Image previewDummyImage;
    public Color previewDummyColor = new Color(1f, 1f, 1f, 0.9f);
    public Vector2 previewDummySize = new Vector2(72f, 72f);

    [Header("Intent")]
    public RectTransform intentRoot;
    public CanvasGroup intentCanvasGroup;
    public Image intentIcon;
    public TMP_Text intentValueText;
    public Sprite intentFallbackSprite;
    public Vector2 intentSize = new Vector2(44f, 44f);

    [Header("HP / Guard")]
    public RectTransform hpBarRoot;
    public Image hpBarBackground;
    public Outline hpBarOutline;
    public Image hpBarFill;
    public TMP_Text hpText;
    public RectTransform guardRoot;
    public Image guardIcon;
    public TMP_Text guardText;
    [Tooltip("Play mode only. When enabled, GuardRoot is shown only while Guard > 0. Edit mode never auto-hides it so the prefab can be adjusted freely.")]
    public bool autoToggleGuardRootInPlayMode = true;
    public Vector2 hpBarSize = new Vector2(164f, 16f);
    public Color hpFillColor = new Color(0.92f, 0.22f, 0.2f, 1f);
    public Color hpGuardFillColor = new Color(0.13f, 0.62f, 0.95f, 1f);
    public Color hpStaggerFillColor = new Color(1f, 0.78f, 0.22f, 1f);
    public Color hpBarBackgroundColor = new Color(0.05f, 0.05f, 0.05f, 1f);
    public Color hpOutlineColor = Color.black;
    public Color hpProtectedOutlineColor = Color.white;
    public Color hpTextNormalColor = Color.white;
    public Color hpTextStaggerColor = new Color(1f, 0.88f, 0.28f, 1f);
    public Color hpHealBlinkColor = new Color(0.25f, 1f, 0.35f, 1f);
    public float hpHealBlinkDuration = 0.45f;

    [Header("Status Row")]
    public RectTransform statusRowRoot;
    [Tooltip("Optional single template slot, e.g. Status_1. If assigned, ActorWorldUI clones this one slot to render all statuses.")]
    public RectTransform statusSlotTemplateRoot;
    public StatusIconSlot[] statusSlots = new StatusIconSlot[DefaultStatusSlotCount];
    public Vector2 statusIconSize = new Vector2(18f, 18f);

    [Header("Combat UI Library")]
    public SkillUiIconLibrarySO iconLibrary;

    [Header("Targetability Overlay Blink")]
    public Color targetOverlayColor = new Color(1f, 0.92f, 0.3f, 0.55f);
    public Color targetOverlayInvalidColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
    [Range(0f, 1f)]
    public float targetOverlayMinAlpha = 0.25f;
    [Range(0f, 10f)]
    public float targetOverlayBlinkSpeed = 2.5f;
    [Range(1f, 1.5f)]
    public float targetOverlayPulseScale = 1.1f;

    [Header("Target Preview")]
    public Color hpPreviewDamageColor = new Color(1f, 0.55f, 0.1f, 0.92f);
    [Range(0f, 10f)]
    public float hpPreviewBlinkSpeed = 3f;
    [Range(0f, 1f)]
    public float hpPreviewMinAlpha = 0.35f;
    [SerializeField, Range(0.05f, 1.5f)] private float hpDamageTrailDuration = 0.45f;
    [SerializeField, Range(0f, 0.5f)] private float hpDamageTrailHold = 0.08f;

    [Header("Editor Preview")]
    public bool showEditorPreview = true;
    public bool previewShowIntent = true;
    public Sprite previewIntentSprite;
    public int previewIntentValue = 3;
    public int previewHp = 27;
    public int previewMaxHp = 30;
    public int previewGuard = 0;
    public bool previewStaggered;
    public bool previewMarked;
    public bool previewFrozen;
    public int previewChilledTurns;
    public int previewBurnStacks = 2;
    public int previewBleedStacks = 1;
    public bool previewHasAilment;
    public AilmentType previewAilment = AilmentType.Sleep;
    public int previewAilmentTurns = 2;

    [Header("Sorting")]
    public bool forceSorting = true;
    public int sortingOrder = 500;

    private EnemyBrainController _brain;
    private readonly List<StatusVisualData> _statusBuffer = new List<StatusVisualData>(DefaultStatusSlotCount);
    private readonly List<StatusIconSlot> _spawnedStatusSlots = new List<StatusIconSlot>(DefaultStatusSlotCount);
    private int _lastRuntimeHp = int.MinValue;
    private int _lastRuntimeMaxHp = int.MinValue;
    private int _lastRuntimeGuard = int.MinValue;
    private int _lastDisplayedHp = int.MinValue;
    private int _lastRuntimeStatusSignature = int.MinValue;
    private int _lastRuntimeIntentSignature = int.MinValue;
    private int _nextIdleWorldTooltipRefreshFrame;
    private bool _lastRuntimeStaggered;

    // --- Targetability overlay runtime ---
    private RectTransform _targetOverlayRoot;
    private Image _targetOverlayImage;
    private Vector3 _targetOverlayBaseScale = Vector3.one;
    private bool _targetOverlayBaseScaleCaptured;
    private bool _targetOverlayActive;
    private bool _targetOverlayIsValid = true;

    // --- Target Preview runtime ---
    private bool _targetPreviewActive;
    private TargetPreviewData _previewData;
    private Image _hpPreviewFill;   // phần cam đè lên HP bar
    private Tween _hpHealBlinkTween;
    private Tween _hpDamageTrailTween;

    private void Awake()
    {
        ActorWorldUiRegistry.Register(this);
    }

    [ContextMenu("Setup World UI Layout")]
    public void SetupWorldUiLayout()
    {
        ResolveReferences();
        DisableLegacyChildren();
        DisableAllGraphicRaycasts();

        if (forceSorting)
            ForceSortingOrder(sortingOrder);

        if (Application.isPlaying)
            HidePreviewDummyRuntime();
        else
            RefreshEditorPreview();
    }

    public void Bind(CombatActor a)
    {
        ActorWorldUiRegistry.Register(this);

        actor = a;
        InvalidateRuntimeCache();
        if (actor == null)
        {
            HideUnboundRuntimeVisuals();
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        _brain = actor.GetComponent<EnemyBrainController>();

        AttachToActorAnchor();
        ResolveReferences();
        DisableLegacyChildren();
        DisableAllGraphicRaycasts();

        if (forceSorting)
            ForceSortingOrder(sortingOrder);

        HidePreviewDummyRuntime();
        RefreshRuntime();
    }

    private void OnEnable()
    {
        ActorWorldUiRegistry.Register(this);
        InvalidateRuntimeCache();

        if (!CanRefreshInEditor())
            return;

        ResolveReferences();
        DisableLegacyChildren();

        if (Application.isPlaying)
        {
            HidePreviewDummyRuntime();
            if (actor == null)
                HideUnboundRuntimeVisuals();
        }
        else
            RefreshEditorPreview();
    }

    private void OnDestroy()
    {
        ActorWorldUiRegistry.Unregister(this);
    }

    private void InvalidateRuntimeCache()
    {
        _lastRuntimeHp = int.MinValue;
        _lastRuntimeMaxHp = int.MinValue;
        _lastRuntimeGuard = int.MinValue;
        _lastDisplayedHp = int.MinValue;
        _lastRuntimeStatusSignature = int.MinValue;
        _lastRuntimeIntentSignature = int.MinValue;
    }

    private void OnValidate()
    {
        if (!CanRefreshInEditor())
            return;

        ResolveReferences();
        DisableLegacyChildren();

        if (!Application.isPlaying)
            RefreshEditorPreview();
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying)
        {
            if (!CanRefreshInEditor())
                return;

            ResolveReferences();
            DisableLegacyChildren();
            RefreshEditorPreview();
            return;
        }

        if (actor == null || actor.IsDead || !actor.gameObject.activeInHierarchy)
        {
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
            return;
        }

        if (_brain == null)
            _brain = actor.GetComponent<EnemyBrainController>();

        AttachToActorAnchor();
        if (AreRuntimeReferencesMissing())
            ResolveReferences();
        RefreshRuntime();
        if (ShouldRefreshWorldUiTooltipsThisFrame())
            RefreshWorldUiTooltips();

        // Update overlay blink
        if (_targetOverlayActive && _targetOverlayImage != null)
        {
            Color baseColor = _targetOverlayIsValid ? targetOverlayColor : targetOverlayInvalidColor;
            float maxAlpha = baseColor.a;
            float t = 1f;

            if (_targetPreviewActive)
            {
                // Nếu đang preview (di chuột đè lên), giữ độ sáng tối đa
                baseColor.a = maxAlpha;
            }
            else
            {
                // Nếu chỉ đang hiện target hợp lệ chung chung, nhấp nháy
                t = Mathf.PingPong(Time.time * targetOverlayBlinkSpeed, 1f);
                baseColor.a = Mathf.Lerp(targetOverlayMinAlpha, maxAlpha, t);
            }

            _targetOverlayImage.color = baseColor;
            UpdateTargetOverlayPulse(t);
        }

        // Update target preview blink
        if (_targetPreviewActive)
            UpdateTargetPreviewBlink();
    }

    private bool ShouldRefreshWorldUiTooltipsThisFrame()
    {
        if (ActorWorldKeywordTooltipUI.IsShowingFor(this))
            return true;

        if (Time.frameCount < _nextIdleWorldTooltipRefreshFrame)
            return false;

        _nextIdleWorldTooltipRefreshFrame = Time.frameCount + IdleWorldTooltipRefreshIntervalFrames;
        return true;
    }

    public void ShowIntentImmediate()
    {
        if (intentCanvasGroup == null)
            return;

        intentCanvasGroup.DOKill();
        intentCanvasGroup.alpha = 1f;
    }

    public void FadeIntent(float duration = 0.25f)
    {
        if (intentCanvasGroup == null)
            return;

        intentCanvasGroup.DOKill();
        intentCanvasGroup.DOFade(0f, duration).SetEase(Ease.OutQuad);
    }

    public void PlayHealFeedback()
    {
        if (hpText == null && hpBarFill == null)
            return;

        _hpHealBlinkTween?.Kill();

        Color baseTextColor = hpText != null ? hpText.color : Color.white;
        Color baseFillColor = hpBarFill != null ? hpBarFill.color : Color.white;

        Sequence seq = DOTween.Sequence().SetUpdate(true);
        if (hpText != null)
            seq.Join(hpText.DOColor(hpHealBlinkColor, hpHealBlinkDuration * 0.4f).SetEase(Ease.OutQuad));
        if (hpBarFill != null)
            seq.Join(hpBarFill.DOColor(hpHealBlinkColor, hpHealBlinkDuration * 0.4f).SetEase(Ease.OutQuad));

        seq.AppendInterval(hpHealBlinkDuration * 0.15f);

        if (hpText != null)
            seq.Join(hpText.DOColor(baseTextColor, hpHealBlinkDuration * 0.45f).SetEase(Ease.InQuad));
        if (hpBarFill != null)
            seq.Join(hpBarFill.DOColor(baseFillColor, hpHealBlinkDuration * 0.45f).SetEase(Ease.InQuad));

        seq.OnComplete(() => _hpHealBlinkTween = null);
        _hpHealBlinkTween = seq;
    }

    // ---------------------------
    // Targetability Overlay API
    // ---------------------------

    /// <summary>
    /// Hiện overlay marker trên actor này, báo hiệu nó là target hợp lệ.
    /// isValid = true: target thật sự cast được (vàng). false: target bị chặn (xám).
    /// </summary>
    public void ShowTargetOverlay(bool isValid = true)
    {
        EnsureTargetOverlay();
        _targetOverlayActive = true;
        _targetOverlayIsValid = isValid;
        CaptureTargetOverlayBaseScale();
        if (_targetOverlayRoot != null)
            _targetOverlayRoot.gameObject.SetActive(true);
    }

    /// <summary>
    /// Ẩn overlay marker.
    /// </summary>
    public void HideTargetOverlay()
    {
        _targetOverlayActive = false;
        if (_targetOverlayRoot != null)
        {
            ResetTargetOverlayPulse();
            _targetOverlayRoot.gameObject.SetActive(false);
        }
    }

    private void EnsureTargetOverlay()
    {
        if (_targetOverlayRoot != null)
            return;

        if (worldCanvasRoot == null)
        {
            ResolveReferences();
            if (worldCanvasRoot == null) return;
        }

        // Tái sử dụng trực tiếp PreviewDummy làm Target Overlay
        if (previewDummyRoot != null)
        {
            _targetOverlayRoot = previewDummyRoot;
            _targetOverlayImage = previewDummyImage;

            if (_targetOverlayImage != null)
                _targetOverlayImage.raycastTarget = false;

            // Đặt phía sau HP bar để không che thông tin
            _targetOverlayRoot.SetAsFirstSibling();
            _targetOverlayRoot.gameObject.SetActive(false);
            CaptureTargetOverlayBaseScale();
        }
    }

    private void CaptureTargetOverlayBaseScale()
    {
        if (_targetOverlayRoot == null || _targetOverlayBaseScaleCaptured)
            return;

        _targetOverlayBaseScale = _targetOverlayRoot.localScale;
        _targetOverlayBaseScaleCaptured = true;
    }

    private void UpdateTargetOverlayPulse(float t)
    {
        if (_targetOverlayRoot == null)
            return;

        CaptureTargetOverlayBaseScale();
        float scale = Mathf.Lerp(1f, targetOverlayPulseScale, t);
        _targetOverlayRoot.localScale = _targetOverlayBaseScale * scale;
    }

    private void ResetTargetOverlayPulse()
    {
        if (_targetOverlayRoot == null || !_targetOverlayBaseScaleCaptured)
            return;

        _targetOverlayRoot.localScale = _targetOverlayBaseScale;
    }

    private static Sprite _sharedCircleSprite;

    private static Sprite CreateCircleSprite()
    {
        if (_sharedCircleSprite != null)
            return _sharedCircleSprite;

        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size * 0.5f;
        float radiusOuter = center;
        float radiusInner = center - 4f; // 4px ring

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center));
                if (dist <= radiusOuter && dist >= radiusInner)
                    tex.SetPixel(x, y, Color.white);
                else if (dist < radiusInner && dist >= radiusInner - 1f)
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, 0.3f));
                else
                    tex.SetPixel(x, y, Color.clear);
            }
        }

        tex.Apply();
        _sharedCircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return _sharedCircleSprite;
    }

    private void RefreshRuntime()
    {
        if (actor == null)
        {
            HideUnboundRuntimeVisuals();
            return;
        }

        if (Application.isPlaying && (actor.IsDead || !actor.gameObject.activeInHierarchy))
        {
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
            return;
        }

        // Nếu đang preview thì không refresh từ actual state — preview render riêng
        if (_targetPreviewActive)
            return;

        if (Application.isPlaying && actor.isPlayer)
        {
            HidePlayerWorldVitalsRuntime();
            return;
        }

        int statusSignature = BuildRuntimeStatusSignature(actor.status);
        bool staggered = actor.status != null && actor.status.staggered;
        if (_lastRuntimeHp != actor.hp ||
            _lastRuntimeMaxHp != actor.maxHP ||
            _lastRuntimeGuard != actor.guardPool ||
            _lastRuntimeStaggered != staggered)
        {
            _lastRuntimeHp = actor.hp;
            _lastRuntimeMaxHp = actor.maxHP;
            _lastRuntimeGuard = actor.guardPool;
            _lastRuntimeStaggered = staggered;
            RefreshHpAndGuard(actor.hp, actor.maxHP, actor.guardPool, staggered);
        }

        if (_lastRuntimeStatusSignature != statusSignature)
        {
            _lastRuntimeStatusSignature = statusSignature;
            RefreshStatusIcons(actor.status);
        }

        RefreshIntent();
    }

    private void HidePlayerWorldVitalsRuntime()
    {
        if (hpBarRoot != null)
            hpBarRoot.gameObject.SetActive(false);
        if (guardRoot != null)
            guardRoot.gameObject.SetActive(false);
        if (statusRowRoot != null)
            statusRowRoot.gameObject.SetActive(false);
        if (intentRoot != null)
            intentRoot.gameObject.SetActive(false);
    }

    private void HideUnboundRuntimeVisuals()
    {
        CleanupSpawnedStatusSlots();

        if (intentRoot != null)
            intentRoot.gameObject.SetActive(false);

        if (guardRoot != null && autoToggleGuardRootInPlayMode)
            guardRoot.gameObject.SetActive(false);

        if (statusSlotTemplateRoot != null)
            statusSlotTemplateRoot.gameObject.SetActive(false);

        if (statusSlots == null)
            return;

        for (int i = 0; i < statusSlots.Length; i++)
        {
            StatusIconSlot slot = statusSlots[i];
            if (slot?.root != null)
                slot.root.gameObject.SetActive(false);
        }
    }

    // ---------------------------
    // Target Preview API
    // ---------------------------

    private void RefreshHpAndGuard(int hp, int maxHp, int guard, bool staggered)
    {
        int safeHp = Mathf.Max(0, hp);
        int safeMaxHp = Mathf.Max(1, maxHp);
        int previousHp = _lastDisplayedHp == int.MinValue ? safeHp : Mathf.Clamp(_lastDisplayedHp, 0, safeMaxHp);
        bool shouldPlayDamageTrail = Application.isPlaying &&
                                     !_targetPreviewActive &&
                                     previousHp > safeHp &&
                                     hpBarFill != null;


        if (hpText != null)
        {
            CombatUiDirtySetUtility.SetTextIfChanged(hpText, $"{safeHp}/{safeMaxHp}");
            CombatUiDirtySetUtility.SetColorIfChanged(hpText, staggered ? hpTextStaggerColor : hpTextNormalColor);
        }

        if (hpBarBackground != null)
            CombatUiDirtySetUtility.SetColorIfChanged(hpBarBackground, hpBarBackgroundColor);

        if (hpBarOutline != null)
            CombatUiDirtySetUtility.SetOutlineColorIfChanged(hpBarOutline, (staggered || guard > 0) ? hpProtectedOutlineColor : hpOutlineColor);

        if (hpBarFill != null)
        {
            CombatUiDirtySetUtility.SetFillAmountIfChanged(hpBarFill, Mathf.Clamp01((float)safeHp / safeMaxHp));
            CombatUiDirtySetUtility.SetColorIfChanged(hpBarFill, staggered ? hpStaggerFillColor : (guard > 0 ? hpGuardFillColor : hpFillColor));
        }

        if (shouldPlayDamageTrail)
            PlayHpDamageTrail(previousHp, safeHp, safeMaxHp);

        if (guardRoot != null && Application.isPlaying && autoToggleGuardRootInPlayMode)
            CombatUiDirtySetUtility.SetActiveIfChanged(guardRoot.gameObject, guard > 0);
        if (guardText != null)
            CombatUiDirtySetUtility.SetTextIfChanged(guardText, Mathf.Max(0, guard).ToString());

        _lastDisplayedHp = safeHp;
    }

    private void PlayHpDamageTrail(int hpBefore, int hpAfter, int maxHp)
    {
        EnsureHpPreviewFill();
        if (_hpPreviewFill == null)
            return;

        _hpDamageTrailTween?.Kill();
        _hpPreviewFill.gameObject.SetActive(true);
        _hpPreviewFill.color = hpPreviewDamageColor;
        _hpPreviewFill.fillAmount = Mathf.Clamp01((float)hpBefore / Mathf.Max(1, maxHp));

        Sequence seq = DOTween.Sequence();
        if (hpDamageTrailHold > 0f)
            seq.AppendInterval(hpDamageTrailHold);

        seq.Append(_hpPreviewFill
            .DOFillAmount(Mathf.Clamp01((float)hpAfter / Mathf.Max(1, maxHp)), Mathf.Max(0.01f, hpDamageTrailDuration))
            .SetEase(Ease.OutCubic));
        seq.OnComplete(() =>
        {
            if (_hpPreviewFill != null && !_targetPreviewActive)
                _hpPreviewFill.gameObject.SetActive(false);
            _hpDamageTrailTween = null;
        });

        _hpDamageTrailTween = seq;
    }

}

internal static class ActorWorldUiRegistry
{
    private static readonly List<ActorWorldUI> Registered = new List<ActorWorldUI>(16);
    private static readonly List<ActorWorldUI> Snapshot = new List<ActorWorldUI>(16);
    private static ActorWorldUI[] _cachedSnapshot = System.Array.Empty<ActorWorldUI>();
    private static bool _snapshotDirty = true;
    private static bool _initializedFromScene;

    public static void Register(ActorWorldUI ui)
    {
        if (ui == null || Registered.Contains(ui))
            return;

        Registered.Add(ui);
        _snapshotDirty = true;
    }

    public static void Unregister(ActorWorldUI ui)
    {
        if (ui == null)
            return;

        if (Registered.Remove(ui))
            _snapshotDirty = true;
    }

    public static ActorWorldUI[] GetAllSnapshot()
    {
        EnsureInitializedFromScene();

        if (!_snapshotDirty)
            return _cachedSnapshot;

        Snapshot.Clear();

        for (int i = Registered.Count - 1; i >= 0; i--)
        {
            ActorWorldUI ui = Registered[i];
            if (ui == null)
            {
                Registered.RemoveAt(i);
                _snapshotDirty = true;
                continue;
            }

            Snapshot.Add(ui);
        }

        _cachedSnapshot = Snapshot.Count > 0 ? Snapshot.ToArray() : System.Array.Empty<ActorWorldUI>();
        _snapshotDirty = false;
        return _cachedSnapshot;
    }

    public static ActorWorldUI FindForActor(CombatActor actor)
    {
        if (actor == null)
            return null;

        EnsureInitializedFromScene();
        for (int i = Registered.Count - 1; i >= 0; i--)
        {
            ActorWorldUI ui = Registered[i];
            if (ui == null)
            {
                Registered.RemoveAt(i);
                _snapshotDirty = true;
                continue;
            }

            if (ui.actor == actor)
                return ui;
        }

        return null;
    }

    private static void EnsureInitializedFromScene()
    {
        if (_initializedFromScene)
            return;

        _initializedFromScene = true;
#if UNITY_2023_1_OR_NEWER
        ActorWorldUI[] sceneUis = UnityEngine.Object.FindObjectsByType<ActorWorldUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        ActorWorldUI[] sceneUis = UnityEngine.Object.FindObjectsOfType<ActorWorldUI>(true);
#endif
        if (sceneUis == null)
            return;

        for (int i = 0; i < sceneUis.Length; i++)
            Register(sceneUis[i]);
    }
}

internal static class CombatTargetPreviewPresenter
{
    public static void ShowBundle(TargetPreviewBuilder.ActionPreviewBundle bundle, ActorWorldUI[] worldUis, CombatHUD hud)
    {
        if (bundle.targetPreviews == null)
            return;

        foreach (KeyValuePair<CombatActor, TargetPreviewData> kvp in bundle.targetPreviews)
        {
            if (kvp.Key == null || !kvp.Value.valid)
                continue;

            if (kvp.Key.isPlayer)
            {
                if (hud != null)
                    hud.ShowPlayerTargetPreview(kvp.Value);
                continue;
            }

            ActorWorldUI ui = FindWorldUi(kvp.Key, worldUis);
            if (ui != null)
                ui.ShowTargetPreview(kvp.Value);
        }
    }

    public static void ClearAll(ActorWorldUI[] worldUis, CombatHUD hud)
    {
        if (worldUis != null)
        {
            for (int i = 0; i < worldUis.Length; i++)
            {
                if (worldUis[i] != null)
                    worldUis[i].ClearTargetPreview();
            }
        }

        if (hud != null)
            hud.ClearPlayerTargetPreview();
    }

    public static ActorWorldUI FindWorldUi(CombatActor actor, ActorWorldUI[] worldUis)
    {
        if (actor == null)
            return null;

        if (worldUis != null)
        {
            for (int i = 0; i < worldUis.Length; i++)
            {
                if (worldUis[i] != null && worldUis[i].actor == actor)
                    return worldUis[i];
            }
        }

        return ActorWorldUiRegistry.FindForActor(actor);
    }
}

internal static class CombatPreviewBundleUtility
{
    public static TargetPreviewBuilder.ActionPreviewBundle BuildActionBundleWithSelfGuard(
        SkillRuntime runtime,
        SkillDamageSO sourceSkill,
        CombatActor caster,
        CombatActor target,
        CombatActor player,
        BattlePartyManager2D party,
        CombatActor enemy,
        int dieValue,
        int guardLocalIndex,
        int repeatPreviewCount,
        int diceEnchantGuardGain)
    {
        int resolveCount = Mathf.Max(1, Mathf.Max(0, repeatPreviewCount) + 1);
        TargetPreviewBuilder.ActionPreviewBundle bundle =
            TargetPreviewBuilder.BuildActionBundle(runtime, caster, target, dieValue, party, enemy, resolveCount, sourceSkill);

        if (!SkillGameplayResolver.CanResolveWithNewPipeline(sourceSkill) && repeatPreviewCount > 0)
            TargetPreviewBuilder.ApplyRepeatPreviewMultiplier(ref bundle, repeatPreviewCount + 1);

        if (player != null &&
            CombatGuardPreviewUtility.TryBuildSelfGuardFinalPreview(runtime, sourceSkill, target, player, dieValue, guardLocalIndex, resolveCount, diceEnchantGuardGain, out TargetPreviewData selfGuardPreview))
        {
            bundle.targetPreviews[player] = selfGuardPreview;
            bundle.valid = true;
        }
        else if (player != null)
        {
            TargetPreviewBuilder.AddSelfResourcePreview(player, diceEnchantGuardGain, 0, ref bundle);
        }

        return bundle;
    }
}

internal static class CombatGuardPreviewUtility
{
    public static int ResolveGuardPreviewDieValue(SkillRuntime runtime, int fallbackDieValue, int guardLocalIndex)
    {
        if (runtime == null || runtime.kind != SkillKind.Guard || runtime.guardValueMode != BaseEffectValueMode.X)
            return fallbackDieValue;

        int guardValue = SkillBehaviorRuntimeUtility.GetPerDieResolvedOutput(runtime, Mathf.Max(0, guardLocalIndex));
        return guardValue > 0 ? guardValue : fallbackDieValue;
    }

    public static bool TryBuildSelfGuardFinalPreview(
        SkillRuntime runtime,
        SkillDamageSO sourceSkill,
        CombatActor hoveredActor,
        CombatActor player,
        int dieValue,
        int guardLocalIndex,
        int resolveCount,
        int diceEnchantGuardGain,
        out TargetPreviewData data)
    {
        data = default;
        if (runtime == null || player == null || hoveredActor != player || runtime.kind != SkillKind.Guard)
            return false;

        int skillGuardGain = ResolveSelfGuardGain(runtime, sourceSkill, player, dieValue, guardLocalIndex);
        if (resolveCount > 1)
            skillGuardGain *= resolveCount;

        int totalGuardGain = Mathf.Max(0, skillGuardGain) + Mathf.Max(0, diceEnchantGuardGain);
        if (totalGuardGain <= 0)
            return false;

        data = new TargetPreviewData
        {
            valid = true,
            currentHp = player.hp,
            currentMaxHp = player.maxHP,
            currentGuard = player.guardPool,
            previewHpAfter = player.hp,
            previewGuardAfter = player.guardPool + totalGuardGain,
            currentlyStaggered = player.status != null && player.status.staggered,
            currentBurn = player.status != null ? player.status.burnStacks : 0,
            currentBleed = player.status != null ? player.status.bleedStacks : 0,
            currentMarked = player.status != null && player.status.marked,
            currentFrozen = player.status != null && player.status.frozen,
            previewBurnAfter = player.status != null ? player.status.burnStacks : 0,
            previewBleedAfter = player.status != null ? player.status.bleedStacks : 0,
            previewMarkedAfter = player.status != null && player.status.marked,
            previewFrozenAfter = player.status != null && player.status.frozen,
            isSelfTarget = true,
            selfGuardGain = totalGuardGain
        };
        return true;
    }

    private static int ResolveSelfGuardGain(SkillRuntime runtime, SkillDamageSO sourceSkill, CombatActor caster, int dieValue, int guardLocalIndex)
    {
        if (runtime == null || runtime.kind != SkillKind.Guard)
            return 0;

        if (sourceSkill == null)
            sourceSkill = SkillGameplayResolver.GetSourceSkill(runtime);
        if (SkillGameplayResolver.CanResolveWithNewPipeline(sourceSkill))
        {
            SkillResolvedResult resolved = SkillGameplayResolver.Resolve(
                sourceSkill,
                runtime,
                caster,
                caster,
                SkillGameplayResolver.BuildConditionContext(runtime, caster, caster));
            if (resolved == null || !resolved.canCast || resolved.effects == null)
                return 0;

            int resolvedGuard = 0;
            for (int i = 0; i < resolved.effects.Count; i++)
            {
                ResolvedEffect effect = resolved.effects[i];
                if (effect == null || effect.sameActionFollowUp || effect.type != SkillEffectType.GainGuard)
                    continue;
                CombatActor target = effect.targetActor != null ? effect.targetActor : caster;
                if (target == caster)
                    resolvedGuard += Mathf.Max(0, effect.value);
            }

            return Mathf.Max(0, resolvedGuard);
        }

        int baseGuard;
        if (runtime.guardValueMode == BaseEffectValueMode.Flat && runtime.guardFlat > 0)
            baseGuard = SkillOutputValueUtility.AddActionAddedValue(runtime.guardFlat, runtime);
        else if (runtime.guardValueMode == BaseEffectValueMode.X)
            baseGuard = ResolveGuardPreviewDieValue(runtime, dieValue, guardLocalIndex);
        else
            baseGuard = runtime.CalculateGuard(dieValue);

        PassiveSystem passiveSystem = caster != null ? caster.GetComponent<PassiveSystem>() : null;
        float pct = passiveSystem != null ? passiveSystem.GetGuardGainPercent() : 0f;
        float multiplier = 1f + Mathf.Max(-0.99f, pct);
        return Mathf.Max(0, Mathf.FloorToInt(baseGuard * multiplier));
    }
}

/// <summary>
/// Small helpers for combat UI hot paths.
/// Unity UI/TMP marks graphics dirty even for many same-value assignments, so
/// these setters avoid redundant canvas/text rebuild work during combat ticks.
/// </summary>
internal static class CombatUiDirtySetUtility
{
    public static void SetActiveIfChanged(GameObject target, bool active)
    {
        if (target != null && target.activeSelf != active)
            target.SetActive(active);
    }

    public static void SetTextIfChanged(TMP_Text text, string value)
    {
        if (text != null && text.text != value)
            text.text = value;
    }

    public static void SetColorIfChanged(Graphic graphic, Color value)
    {
        if (graphic != null && graphic.color != value)
            graphic.color = value;
    }

    public static void SetColorIfChanged(TMP_Text text, Color value)
    {
        if (text != null && text.color != value)
            text.color = value;
    }

    public static void SetOutlineColorIfChanged(Outline outline, Color value)
    {
        if (outline != null && outline.effectColor != value)
            outline.effectColor = value;
    }

    public static void SetFillAmountIfChanged(Image image, float value)
    {
        if (image != null && !Mathf.Approximately(image.fillAmount, value))
            image.fillAmount = value;
    }

    public static void SetPreferredWidthIfChanged(LayoutElement layout, float value)
    {
        if (layout != null && !Mathf.Approximately(layout.preferredWidth, value))
            layout.preferredWidth = value;
    }

    public static void SetPreferredHeightIfChanged(LayoutElement layout, float value)
    {
        if (layout != null && !Mathf.Approximately(layout.preferredHeight, value))
            layout.preferredHeight = value;
    }

    public static void SetRectWidthIfChanged(RectTransform rect, float value)
    {
        if (rect != null && !Mathf.Approximately(rect.rect.width, value))
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, value);
    }

    public static void SetRectHeightIfChanged(RectTransform rect, float value)
    {
        if (rect != null && !Mathf.Approximately(rect.rect.height, value))
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, value);
    }
}
