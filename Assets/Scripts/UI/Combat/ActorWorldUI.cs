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

    [Header("Target Preview")]
    public Color hpPreviewDamageColor = new Color(1f, 0.55f, 0.1f, 0.92f);
    [Range(0f, 10f)]
    public float hpPreviewBlinkSpeed = 3f;
    [Range(0f, 1f)]
    public float hpPreviewMinAlpha = 0.35f;

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

    // --- Targetability overlay runtime ---
    private RectTransform _targetOverlayRoot;
    private Image _targetOverlayImage;
    private bool _targetOverlayActive;
    private bool _targetOverlayIsValid = true;

    // --- Target Preview runtime ---
    private bool _targetPreviewActive;
    private TargetPreviewData _previewData;
    private Image _hpPreviewFill;   // phần cam đè lên HP bar
    private Tween _hpHealBlinkTween;

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
        actor = a;
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
        ResolveReferences();
        RefreshRuntime();
        RefreshWorldUiTooltips();

        // Update overlay blink
        if (_targetOverlayActive && _targetOverlayImage != null)
        {
            Color baseColor = _targetOverlayIsValid ? targetOverlayColor : targetOverlayInvalidColor;
            float maxAlpha = baseColor.a;

            if (_targetPreviewActive)
            {
                // Nếu đang preview (di chuột đè lên), giữ độ sáng tối đa
                baseColor.a = maxAlpha;
            }
            else
            {
                // Nếu chỉ đang hiện target hợp lệ chung chung, nhấp nháy
                float t = Mathf.PingPong(Time.time * targetOverlayBlinkSpeed, 1f);
                baseColor.a = Mathf.Lerp(targetOverlayMinAlpha, maxAlpha, t);
            }

            _targetOverlayImage.color = baseColor;
        }

        // Update target preview blink
        if (_targetPreviewActive)
            UpdateTargetPreviewBlink();
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
            _targetOverlayRoot.gameObject.SetActive(false);
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
        }
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

        bool staggered = actor.status != null && actor.status.staggered;
        RefreshHpAndGuard(actor.hp, actor.maxHP, actor.guardPool, staggered);
        RefreshStatusIcons(actor.status);
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


        if (hpText != null)
        {
            hpText.text = $"{safeHp}/{safeMaxHp}";
            hpText.color = staggered ? hpTextStaggerColor : hpTextNormalColor;
        }

        if (hpBarBackground != null)
            hpBarBackground.color = hpBarBackgroundColor;

        if (hpBarOutline != null)
            hpBarOutline.effectColor = (staggered || guard > 0) ? hpProtectedOutlineColor : hpOutlineColor;

        if (hpBarFill != null)
        {
            hpBarFill.fillAmount = Mathf.Clamp01((float)safeHp / safeMaxHp);
            hpBarFill.color = staggered ? hpStaggerFillColor : (guard > 0 ? hpGuardFillColor : hpFillColor);
        }

        if (guardRoot != null && Application.isPlaying && autoToggleGuardRootInPlayMode)
            guardRoot.gameObject.SetActive(guard > 0);
        if (guardText != null)
            guardText.text = Mathf.Max(0, guard).ToString();
    }



}
