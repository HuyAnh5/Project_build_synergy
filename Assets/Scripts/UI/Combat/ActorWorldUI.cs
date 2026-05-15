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
public class ActorWorldUI : MonoBehaviour
{
    private const int DefaultStatusSlotCount = 8;

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
            HidePreviewDummyRuntime();
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
            return;

        if (Application.isPlaying && (actor.IsDead || !actor.gameObject.activeInHierarchy))
        {
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
            return;
        }

        // Nếu đang preview thì không refresh từ actual state — preview render riêng
        if (_targetPreviewActive)
            return;

        bool staggered = actor.status != null && actor.status.staggered;
        RefreshHpAndGuard(actor.hp, actor.maxHP, actor.guardPool, staggered);
        RefreshStatusIcons(actor.status);
        RefreshIntent();
    }

    // ---------------------------
    // Target Preview API
    // ---------------------------

    /// <summary>
    /// Hiện preview kết quả action lên HP/Guard/Status của actor này.
    /// Gọi mỗi frame khi drag skill đè lên target.
    /// </summary>
    public void ShowTargetPreview(TargetPreviewData data)
    {
        _targetPreviewActive = true;
        _previewData = data;
        EnsureHpPreviewFill();

        // --- Tính trạng thái thanh HP ---
        int maxHp = Mathf.Max(1, data.currentMaxHp);
        int hpAfter = data.previewHpAfter;
        int hpBefore = data.currentHp;
        int guardAfter = data.previewGuardAfter;

        // Stagger: nếu guard bị phá HOẶC target đã stagger sẵn
        bool willBeStaggered = data.willBreakGuard || data.currentlyStaggered;

        // --- Render HP bar chính với HP after (phần còn lại sau action) ---
        if (hpBarFill != null)
        {
            // Nếu hồi máu, hpBarFill chính vẫn giữ mức cũ (hpBefore) để fill phụ xanh nhấp nháy lộ ra.
            hpBarFill.fillAmount = data.hpLost < 0 ? Mathf.Clamp01((float)hpBefore / maxHp) : Mathf.Clamp01((float)hpAfter / maxHp);
            hpBarFill.color = willBeStaggered ? hpStaggerFillColor : (guardAfter > 0 ? hpGuardFillColor : hpFillColor);
        }

        // --- Render phần cam/xanh lá: HP sắp mất/hồi ---
        if (_hpPreviewFill != null)
        {
            if (data.hpLost > 0) // Mất máu
            {
                // FillAmount = toàn bộ phần HP trước action (cam + đỏ tạo visual đúng)
                _hpPreviewFill.fillAmount = Mathf.Clamp01((float)hpBefore / maxHp);
                _hpPreviewFill.color = hpPreviewDamageColor;
                _hpPreviewFill.gameObject.SetActive(true);
            }
            else if (data.hpLost < 0) // Hồi máu
            {
                // FillAmount = toàn bộ phần HP sau action (xanh + đỏ tạo visual đúng)
                _hpPreviewFill.fillAmount = Mathf.Clamp01((float)hpAfter / maxHp);
                _hpPreviewFill.color = hpHealBlinkColor;
                _hpPreviewFill.gameObject.SetActive(true);
            }
            else
            {
                _hpPreviewFill.gameObject.SetActive(false);
            }
        }

        // --- HP text preview ---
        if (hpText != null)
        {
            if (data.hpLost < 0)
                hpText.text = $"{Mathf.Max(0, hpAfter)}/{maxHp} (+{-data.hpLost})";
            else if (data.hpLost > 0)
                hpText.text = $"{Mathf.Max(0, hpAfter)}/{maxHp} (-{data.hpLost})";
            else
                hpText.text = $"{Mathf.Max(0, hpAfter)}/{maxHp}";
            // Color sẽ nhấp nháy trong UpdateTargetPreviewBlink
        }

        // --- Guard preview ---
        if (guardRoot != null)
            guardRoot.gameObject.SetActive(guardAfter > 0);
        if (guardText != null)
            guardText.text = Mathf.Max(0, guardAfter).ToString();

        // --- Outline preview ---
        if (hpBarOutline != null)
            hpBarOutline.effectColor = (willBeStaggered || guardAfter > 0) ? hpProtectedOutlineColor : hpOutlineColor;

        if (hpBarBackground != null)
            hpBarBackground.color = hpBarBackgroundColor;

        // --- Status icons preview ---
        BuildPreviewStatusBufferFromData(data);
        ApplyStatusBuffer();
    }

    /// <summary>
    /// Tắt preview, quay về hiển thị state thật của actor.
    /// </summary>
    public void ClearTargetPreview()
    {
        if (!_targetPreviewActive)
            return;

        _targetPreviewActive = false;

        if (_hpPreviewFill != null)
            _hpPreviewFill.gameObject.SetActive(false);

        // Reset lại màu text về bình thường trước khi refresh
        if (hpText != null)
            hpText.color = hpTextNormalColor;
        if (guardText != null)
            guardText.color = Color.white;

        // Force refresh lại state thật ngay lập tức
        if (actor != null)
        {
            bool staggered = actor.status != null && actor.status.staggered;
            RefreshHpAndGuard(actor.hp, actor.maxHP, actor.guardPool, staggered);
            RefreshStatusIcons(actor.status);
        }
    }

    private void UpdateTargetPreviewBlink()
    {
        float t = Mathf.PingPong(Time.time * hpPreviewBlinkSpeed, 1f);

        // --- HP preview fill (cam hoặc xanh) nhấp nháy ---
        if (_hpPreviewFill != null && _hpPreviewFill.gameObject.activeSelf)
        {
            Color baseColor = _previewData.hpLost < 0 ? hpHealBlinkColor : hpPreviewDamageColor;
            Color c = baseColor;
            c.a = Mathf.Lerp(hpPreviewMinAlpha, baseColor.a, t);
            _hpPreviewFill.color = c;
        }

        // --- HP text nhấp nháy nếu có thay đổi ---
        if (hpText != null && _previewData.hpLost != 0)
        {
            Color baseColor = _previewData.hpLost < 0 ? hpHealBlinkColor : hpPreviewDamageColor;
            Color textColor = Color.Lerp(baseColor, Color.white, t);
            hpText.color = textColor;
        }

        // --- Guard text nhấp nháy nếu có thay đổi ---
        if (guardText != null && _previewData.previewGuardAfter != _previewData.currentGuard)
        {
            Color textColor = Color.Lerp(hpPreviewDamageColor, Color.white, t);
            guardText.color = textColor;
        }

        // --- Status nhấp nháy ---
        for (int i = 0; i < statusSlots.Length; i++)
        {
            if (i >= _statusBuffer.Count) break;
            StatusIconSlot slot = statusSlots[i];
            StatusVisualData data = _statusBuffer[i];
            if (slot == null || slot.root == null || !slot.root.gameObject.activeSelf) continue;

            bool isBlinking = false;
            bool isConsume = false;
            
            if (data.shortLabel == "BU")
            {
                if (_previewData.previewBurnAfter > _previewData.currentBurn) isBlinking = true;
                else if (_previewData.previewBurnAfter < _previewData.currentBurn) { isBlinking = true; isConsume = true; }
            }
            else if (data.shortLabel == "BL")
            {
                if (_previewData.previewBleedAfter > _previewData.currentBleed) isBlinking = true;
                else if (_previewData.previewBleedAfter < _previewData.currentBleed) { isBlinking = true; isConsume = true; }
            }
            else if (data.shortLabel == "MK")
            {
                isBlinking = _previewData.willTriggerMarkShock;
            }

            if (isBlinking)
            {
                Color blinkColor = Color.Lerp(hpPreviewDamageColor, Color.white, t);
                if (isConsume)
                {
                    if (slot.iconImage != null) slot.iconImage.color = blinkColor;
                    if (slot.valueText != null) slot.valueText.color = blinkColor;
                }
                else
                {
                    if (slot.iconImage != null && data.shortLabel == "MK") slot.iconImage.color = blinkColor;
                    if (slot.valueText != null) slot.valueText.color = blinkColor;
                }
            }
        }
    }

    private void EnsureHpPreviewFill()
    {
        if (_hpPreviewFill != null)
            return;

        if (hpBarFill == null)
            return;

        // Tạo Image fill phụ nằm phía sau fill chính → phần dư ra = phần cam
        GameObject go = new GameObject("HpPreviewFill", typeof(RectTransform), typeof(Image));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(hpBarFill.rectTransform.parent, false);

        // Copy layout từ fill chính
        RectTransform fillRt = hpBarFill.rectTransform;
        rt.anchorMin = fillRt.anchorMin;
        rt.anchorMax = fillRt.anchorMax;
        rt.pivot = fillRt.pivot;
        rt.offsetMin = fillRt.offsetMin;
        rt.offsetMax = fillRt.offsetMax;
        rt.sizeDelta = fillRt.sizeDelta;
        rt.anchoredPosition = fillRt.anchoredPosition;

        // Thứ tự: Background → PreviewFill (cam) → MainFill → HpText (luôn trên cùng)
        // Đặt preview fill TRUỚC fill chính (sibling index thấp hơn = render trước)
        rt.SetSiblingIndex(fillRt.GetSiblingIndex()); // sau bước này previewFill cùng index, fillRt bị đẩy lên +1

        // Đảm bảo hpText luôn là sibling cuối (render trên cùng)
        if (hpText != null)
            hpText.rectTransform.SetAsLastSibling();

        _hpPreviewFill = go.GetComponent<Image>();
        _hpPreviewFill.sprite = hpBarFill.sprite;
        _hpPreviewFill.type = Image.Type.Filled;
        _hpPreviewFill.fillMethod = hpBarFill.fillMethod;
        _hpPreviewFill.fillOrigin = hpBarFill.fillOrigin;
        _hpPreviewFill.color = hpPreviewDamageColor;
        _hpPreviewFill.raycastTarget = false;
        go.SetActive(false);
    }

    private void BuildPreviewStatusBufferFromData(TargetPreviewData data)
    {
        _statusBuffer.Clear();

        // Rebuild status icons based on preview data
        // Freeze
        if (data.previewFrozenAfter)
            AddStatusVisual(CombatUiStatusIconKind.Freeze, "FR", string.Empty, new Color(0.4f, 0.78f, 1f, 0.96f));

        // Chilled: keep from current actor state if not frozen (preview doesn't change chilled)
        if (actor != null && actor.status != null && !data.previewFrozenAfter && actor.status.chilledTurns > 0)
            AddStatusVisual(CombatUiStatusIconKind.Chilled, "CH", actor.status.chilledTurns.ToString(), new Color(0.58f, 0.9f, 1f, 0.96f));

        // Mark
        if (data.previewMarkedAfter)
            AddStatusVisual(CombatUiStatusIconKind.Mark, "MK", string.Empty, new Color(1f, 0.88f, 0.28f, 0.96f));

        // Burn
        if (data.previewBurnAfter > 0 || data.currentBurn > 0)
            AddStatusVisual(CombatUiStatusIconKind.Burn, "BU", data.previewBurnAfter.ToString(), new Color(1f, 0.42f, 0.22f, 0.96f));

        // Bleed
        if (data.previewBleedAfter > 0 || data.currentBleed > 0)
            AddStatusVisual(CombatUiStatusIconKind.Bleed, "BL", data.previewBleedAfter.ToString(), new Color(0.82f, 0.14f, 0.2f, 0.96f));

        // Ailment: keep from current actor state (preview doesn't typically change ailments from Attack skills)
        if (actor != null && actor.status != null && actor.status.HasAilment(out AilmentType ailment, out int turnsLeft))
            AddStatusVisual(CombatUiStatusIconKind.Ailment, GetAilmentShortLabel(ailment), Mathf.Max(1, turnsLeft).ToString(), new Color(0.72f, 0.5f, 1f, 0.96f));
    }

    private void AddStatusVisual(CombatUiStatusIconKind kind, string shortLabel, string valueText, Color fallbackBackground)
    {
        if (TryGetStatusVisual(kind, out StatusVisualData data, shortLabel, valueText, fallbackBackground))
        {
            _statusBuffer.Add(data);
            return;
        }

        _statusBuffer.Add(new StatusVisualData(null, shortLabel, valueText, fallbackBackground));
    }

    private bool TryGetStatusVisual(CombatUiStatusIconKind kind, out StatusVisualData data)
    {
        return TryGetStatusVisual(kind, out data, string.Empty, string.Empty, Color.white);
    }

    private bool TryGetStatusVisual(CombatUiStatusIconKind kind, out StatusVisualData data, string shortLabel, string valueText, Color fallbackBackground)
    {
        if (iconLibrary != null && iconLibrary.TryGetStatusIcon(kind, out Sprite sprite, out Color backgroundColor, out _))
        {
            data = new StatusVisualData(sprite, shortLabel, valueText, backgroundColor);
            return true;
        }

        data = new StatusVisualData(null, shortLabel, valueText, fallbackBackground);
        return false;
    }

    private void RefreshHpAndGuard(int hp, int maxHp, int guard, bool staggered)
    {
        int safeHp = Mathf.Max(0, hp);
        int safeMaxHp = Mathf.Max(1, maxHp);

        if (guardIcon != null)
        {
            if (TryGetStatusVisual(CombatUiStatusIconKind.Guard, out StatusVisualData guardData))
                guardIcon.sprite = guardData.sprite;
            else
                guardIcon.sprite = null;
        }

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

        if (guardRoot != null)
            guardRoot.gameObject.SetActive(guard > 0);
        if (guardText != null)
            guardText.text = Mathf.Max(0, guard).ToString();
    }

    private void RefreshIntent()
    {
        if (intentRoot == null)
            return;

        Sprite iconSprite = null;
        string valueText = string.Empty;
        bool showIntent = actor != null && !actor.isPlayer && TryGetIntentPresentation(out iconSprite, out valueText);
        intentRoot.gameObject.SetActive(showIntent);
        if (!showIntent)
            return;

        if (intentCanvasGroup != null)
            intentCanvasGroup.alpha = 1f;

        if (intentIcon != null)
        {
            intentIcon.sprite = iconSprite != null ? iconSprite : intentFallbackSprite;
            intentIcon.enabled = intentIcon.sprite != null;
            intentIcon.color = intentIcon.enabled ? Color.white : new Color(1f, 1f, 1f, 0f);
        }

        if (intentValueText != null)
            intentValueText.text = valueText;
    }

    private bool TryGetIntentPresentation(out Sprite iconSprite, out string valueText)
    {
        iconSprite = null;
        valueText = string.Empty;

        if (_brain == null || !_brain.CurrentIntent.hasIntent || _brain.definition == null)
            return false;

        int moveIndex = _brain.CurrentIntent.moveIndex;
        if (moveIndex < 0 || moveIndex >= _brain.definition.moves.Count)
            return false;

        EnemyDefinitionSO.EnemyMoveSlot move = _brain.definition.moves[moveIndex];
        if (move == null)
            return false;

        if (move.damageSkill != null)
        {
            iconSprite = move.damageSkill.icon;

            SkillRuntime runtime = SkillRuntime.FromDamage(move.damageSkill);
            if (runtime.kind == SkillKind.Attack)
            {
                int damage = Mathf.Max(0, runtime.CalculateDamage(0));
                valueText = damage > 0 ? damage.ToString() : string.Empty;
            }
            else if (runtime.kind == SkillKind.Guard)
            {
                int guard = Mathf.Max(0, runtime.CalculateGuard(0));
                valueText = guard > 0 ? guard.ToString() : string.Empty;
            }
        }
        else if (move.buffDebuffSkill != null)
        {
            iconSprite = move.buffDebuffSkill.icon;
        }

        if (iconSprite == null)
            iconSprite = intentFallbackSprite;

        return iconSprite != null || !string.IsNullOrEmpty(valueText);
    }

    private void RefreshStatusIcons(StatusController status)
    {
        BuildStatusBuffer(status);
        ApplyStatusBuffer();
    }

    private void BuildStatusBuffer(StatusController status)
    {
        _statusBuffer.Clear();
        if (status == null)
            return;

        if (status.frozen)
            AddStatusVisual(CombatUiStatusIconKind.Freeze, "FR", string.Empty, new Color(0.4f, 0.78f, 1f, 0.96f));
        if (status.chilledTurns > 0)
            AddStatusVisual(CombatUiStatusIconKind.Chilled, "CH", status.chilledTurns.ToString(), new Color(0.58f, 0.9f, 1f, 0.96f));
        if (status.marked)
            AddStatusVisual(CombatUiStatusIconKind.Mark, "MK", string.Empty, new Color(1f, 0.88f, 0.28f, 0.96f));
        if (status.burnStacks > 0)
            AddStatusVisual(CombatUiStatusIconKind.Burn, "BU", status.burnStacks.ToString(), new Color(1f, 0.42f, 0.22f, 0.96f));
        if (status.bleedStacks > 0)
            AddStatusVisual(CombatUiStatusIconKind.Bleed, "BL", status.bleedStacks.ToString(), new Color(0.82f, 0.14f, 0.2f, 0.96f));
        if (status.HasAilment(out AilmentType ailment, out int turnsLeft))
            AddStatusVisual(CombatUiStatusIconKind.Ailment, GetAilmentShortLabel(ailment), Mathf.Max(1, turnsLeft).ToString(), new Color(0.72f, 0.5f, 1f, 0.96f));
    }

    private void RefreshEditorPreview()
    {
        bool showPreview = !Application.isPlaying && showEditorPreview;

        if (previewDummyRoot != null)
            previewDummyRoot.gameObject.SetActive(showPreview);

        if (!showPreview)
            return;

        RefreshHpAndGuard(previewHp, previewMaxHp, previewGuard, previewStaggered);
        BuildPreviewStatusBuffer();
        ApplyStatusBuffer();

        if (intentRoot != null)
            intentRoot.gameObject.SetActive(previewShowIntent);

        if (intentCanvasGroup != null)
            intentCanvasGroup.alpha = 1f;

        if (intentIcon != null)
        {
            intentIcon.sprite = previewIntentSprite != null ? previewIntentSprite : intentFallbackSprite;
            intentIcon.enabled = intentIcon.sprite != null;
            intentIcon.color = intentIcon.enabled ? Color.white : new Color(1f, 1f, 1f, 0f);
        }

        if (intentValueText != null)
            intentValueText.text = previewIntentValue > 0 ? previewIntentValue.ToString() : string.Empty;
    }

    private void BuildPreviewStatusBuffer()
    {
        _statusBuffer.Clear();

        if (previewFrozen)
            AddStatusVisual(CombatUiStatusIconKind.Freeze, "FR", string.Empty, new Color(0.4f, 0.78f, 1f, 0.96f));
        if (previewChilledTurns > 0)
            AddStatusVisual(CombatUiStatusIconKind.Chilled, "CH", previewChilledTurns.ToString(), new Color(0.58f, 0.9f, 1f, 0.96f));
        if (previewMarked)
            AddStatusVisual(CombatUiStatusIconKind.Mark, "MK", string.Empty, new Color(1f, 0.88f, 0.28f, 0.96f));
        if (previewBurnStacks > 0)
            AddStatusVisual(CombatUiStatusIconKind.Burn, "BU", previewBurnStacks.ToString(), new Color(1f, 0.42f, 0.22f, 0.96f));
        if (previewBleedStacks > 0)
            AddStatusVisual(CombatUiStatusIconKind.Bleed, "BL", previewBleedStacks.ToString(), new Color(0.82f, 0.14f, 0.2f, 0.96f));
        if (previewHasAilment)
            AddStatusVisual(CombatUiStatusIconKind.Ailment, GetAilmentShortLabel(previewAilment), Mathf.Max(1, previewAilmentTurns).ToString(), new Color(0.72f, 0.5f, 1f, 0.96f));
    }

    private void ApplyStatusBuffer()
    {
        if (statusSlots == null)
            return;

        for (int i = 0; i < statusSlots.Length; i++)
        {
            StatusIconSlot slot = statusSlots[i];
            if (slot == null || slot.root == null)
                continue;

            bool show = i < _statusBuffer.Count;
            slot.root.gameObject.SetActive(show);
            if (!show)
                continue;

            StatusVisualData data = _statusBuffer[i];

            if (slot.background != null)
                slot.background.color = data.backgroundColor;

            if (slot.iconImage != null)
            {
                slot.iconImage.sprite = data.sprite;
                slot.iconImage.enabled = data.sprite != null;
                slot.iconImage.color = Color.white;
            }

            if (slot.shortLabelText != null)
            {
                string label = data.sprite == null ? data.shortLabel : string.Empty;
                slot.shortLabelText.text = label;
                slot.shortLabelText.gameObject.SetActive(!string.IsNullOrEmpty(label));
            }

            if (slot.valueText != null)
            {
                slot.valueText.text = data.valueText;
                slot.valueText.color = Color.white;
                slot.valueText.gameObject.SetActive(!string.IsNullOrEmpty(data.valueText));
            }
        }
    }

    private void ResolveReferences()
    {
        worldCanvasRoot = ResolveRectTransform(worldCanvasRoot, "WorldCanvasRoot");
        if (worldCanvasRoot == null)
            return;

        if (worldCanvas == null)
            worldCanvas = worldCanvasRoot.GetComponent<Canvas>();
        if (worldCanvasGroup == null)
            worldCanvasGroup = worldCanvasRoot.GetComponent<CanvasGroup>();

        previewDummyRoot = ResolveRectTransform(previewDummyRoot, "WorldCanvasRoot/PreviewDummy");
        if (previewDummyImage == null && previewDummyRoot != null)
            previewDummyImage = previewDummyRoot.GetComponent<Image>();

        intentRoot = ResolveRectTransform(intentRoot, "WorldCanvasRoot/IntentRoot");
        if (intentCanvasGroup == null && intentRoot != null)
            intentCanvasGroup = intentRoot.GetComponent<CanvasGroup>();
        if (intentIcon == null && intentRoot != null)
            intentIcon = FindChildComponent<Image>(intentRoot, "Icon");
        if (intentValueText == null && intentRoot != null)
            intentValueText = FindChildComponent<TMP_Text>(intentRoot, "Value");

        hpBarRoot = ResolveRectTransform(hpBarRoot, "WorldCanvasRoot/HpBarRoot");
        if (hpBarBackground == null && hpBarRoot != null)
            hpBarBackground = FindChildComponent<Image>(hpBarRoot, "Background");
        if (hpBarOutline == null && hpBarBackground != null)
            hpBarOutline = hpBarBackground.GetComponent<Outline>();
        if (hpBarFill == null && hpBarBackground != null)
            hpBarFill = FindChildComponent<Image>(hpBarBackground.rectTransform, "Fill");
        if (hpText == null && hpBarBackground != null)
            hpText = FindChildComponent<TMP_Text>(hpBarBackground.rectTransform, "HpText");

        guardRoot = ResolveRectTransform(guardRoot, "WorldCanvasRoot/HpBarRoot/GuardRoot");
        if (guardIcon == null && guardRoot != null)
            guardIcon = FindChildComponent<Image>(guardRoot, "Icon");
        if (guardText == null && guardRoot != null)
            guardText = FindChildComponent<TMP_Text>(guardRoot, "Value");

        statusRowRoot = ResolveRectTransform(statusRowRoot, "WorldCanvasRoot/StatusRow");
        if (statusSlots == null || statusSlots.Length != DefaultStatusSlotCount)
            statusSlots = new StatusIconSlot[DefaultStatusSlotCount];

        if (statusRowRoot == null)
            return;

        for (int i = 0; i < statusSlots.Length; i++)
        {
            StatusIconSlot slot = statusSlots[i] ?? new StatusIconSlot();
            RectTransform root = ResolveRectTransform(slot.root, $"WorldCanvasRoot/StatusRow/Status_{i + 1}");
            slot.root = root;
            if (slot.root != null)
            {
                if (slot.background == null)
                    slot.background = slot.root.GetComponent<Image>();
                if (slot.iconImage == null)
                    slot.iconImage = FindChildComponent<Image>(slot.root, "Icon");
                if (slot.shortLabelText == null)
                    slot.shortLabelText = FindChildComponent<TMP_Text>(slot.root, "ShortLabel");
                if (slot.valueText == null)
                    slot.valueText = FindChildComponent<TMP_Text>(slot.root, "Value");
            }

            statusSlots[i] = slot;
        }

        if (guardIcon != null && TryGetStatusVisual(CombatUiStatusIconKind.Guard, out StatusVisualData guardData))
        {
            guardIcon.sprite = guardData.sprite;
            guardIcon.color = Color.white;
        }
    }

    private void AttachToActorAnchor()
    {
        if (actor == null)
            return;

        // Bỏ qua uiAnchor, luôn ép UI Canvas dính vào đúng gốc của Actor
        Transform anchor = actor.transform;
        if (anchor == null)
            return;

        if (transform.parent != anchor)
            transform.SetParent(anchor, false);

        // Đã bỏ dòng ghi đè localPosition để tôn trọng Transform Y = 1.2
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }

    private void HidePreviewDummyRuntime()
    {
        if (previewDummyRoot != null)
            previewDummyRoot.gameObject.SetActive(false);
    }

    private void DisableAllGraphicRaycasts()
    {
        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
            graphics[i].raycastTarget = false;
    }

    private void DisableLegacyChildren()
    {
        DisableLegacyChild("HP_Text");
        DisableLegacyChild("Guard_Text");
        DisableLegacyChild("Status_Text");
        DisableLegacyChild("Intent_Text");
    }

    private void DisableLegacyChild(string childName)
    {
        Transform child = transform.Find(childName);
        if (child != null && child.gameObject.activeSelf)
            child.gameObject.SetActive(false);
    }

    private void ForceSortingOrder(int order)
    {
        if (worldCanvas != null)
        {
            worldCanvas.overrideSorting = true;
            worldCanvas.sortingOrder = order;
        }
    }

    private RectTransform ResolveRectTransform(RectTransform current, string path)
    {
        if (current != null)
            return current;

        Transform found = transform.Find(path);
        return found as RectTransform;
    }

    private static T FindChildComponent<T>(Transform parent, string childName) where T : Component
    {
        if (parent == null)
            return null;

        Transform child = parent.Find(childName);
        return child != null ? child.GetComponent<T>() : null;
    }

    private static string GetAilmentShortLabel(AilmentType ailment)
    {
        string text = ailment.ToString().ToUpperInvariant();
        return text.Length <= 2 ? text : text.Substring(0, 2);
    }

    private bool CanRefreshInEditor()
    {
#if UNITY_EDITOR
        if (EditorUtility.IsPersistent(this) && !PrefabUtility.IsPartOfPrefabInstance(this))
            return false;
#endif
        return true;
    }
}
