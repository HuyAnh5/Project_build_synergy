using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class CombatHUD
{
    private const int DefaultPlayerStatusSlotCount = 8;

    private readonly List<PlayerStatusVisualData> _playerStatusBuffer = new List<PlayerStatusVisualData>(DefaultPlayerStatusSlotCount);

    [Header("Player HP")]
    public RectTransform playerHpBarRoot;
    public Image playerHpBarBackground;
    public Outline playerHpBarOutline;
    public Image playerHpBarFill;
    public TMP_Text playerHpText;
    public RectTransform playerGuardRoot;
    public Image playerGuardIcon;
    public TMP_Text playerGuardText;
    public bool autoTogglePlayerGuardRootInPlayMode = true;
    public Color playerHpFillColor = new Color(0.82f, 0.16f, 0.16f, 1f);
    public Color playerHpGuardFillColor = new Color(0.13f, 0.62f, 0.95f, 1f);
    public Color playerHpStaggerFillColor = new Color(1f, 0.78f, 0.18f, 1f);
    public Color playerHpBarBackgroundColor = new Color(0.07f, 0.08f, 0.12f, 0.95f);
    public Color playerHpOutlineColor = Color.black;
    public Color playerHpProtectedOutlineColor = Color.white;
    public Color playerHpTextNormalColor = Color.white;
    public Color playerHpTextStaggerColor = new Color(1f, 0.88f, 0.28f, 1f);
    public Color playerHpHealBlinkColor = new Color(0.25f, 1f, 0.35f, 1f);
    public Color playerHpPreviewDamageColor = new Color(1f, 0.48f, 0.18f, 0.95f);
    [Range(1f, 8f)] public float playerHpPreviewBlinkSpeed = 4f;
    [Range(0f, 1f)] public float playerHpPreviewMinAlpha = 0.28f;

    [Header("Player Status")]
    public RectTransform playerStatusRowRoot;
    public RectTransform playerStatusSlotTemplateRoot;
    public ActorWorldUI.StatusIconSlot[] playerStatusSlots = new ActorWorldUI.StatusIconSlot[DefaultPlayerStatusSlotCount];
    public Vector2 playerStatusIconSize = new Vector2(36f, 36f);
    public SkillUiIconLibrarySO playerIconLibrary;

    private int _lastHp = int.MinValue;
    private int _lastMaxHp = int.MinValue;
    private int _lastGuard = int.MinValue;
    private int _lastStatusSignature = int.MinValue;
    private bool _lastStaggered;
    private bool _playerTargetPreviewActive;
    private TargetPreviewData _playerPreviewData;
    private Image _playerHpPreviewFill;

    private readonly struct PlayerStatusVisualData
    {
        public readonly Sprite sprite;
        public readonly string shortLabel;
        public readonly string valueText;
        public readonly Color backgroundColor;

        public PlayerStatusVisualData(Sprite sprite, string shortLabel, string valueText, Color backgroundColor)
        {
            this.sprite = sprite;
            this.shortLabel = shortLabel;
            this.valueText = valueText;
            this.backgroundColor = backgroundColor;
        }
    }

    public void ShowPlayerTargetPreview(TargetPreviewData data)
    {
        if (!data.valid)
            return;

        _playerTargetPreviewActive = true;
        _playerPreviewData = data;
        EnsurePlayerVitalsUi();
        EnsurePlayerHpPreviewFill();

        int maxHp = Mathf.Max(1, data.currentMaxHp);
        int hpAfter = data.previewHpAfter;
        int hpBefore = data.currentHp;
        int guardAfter = data.previewGuardAfter;
        bool willBeStaggered = data.willBreakGuard || data.currentlyStaggered;

        if (playerHpBarFill != null)
        {
            playerHpBarFill.fillAmount = data.hpLost < 0
                ? Mathf.Clamp01((float)hpBefore / maxHp)
                : Mathf.Clamp01((float)hpAfter / maxHp);
            playerHpBarFill.color = willBeStaggered ? playerHpStaggerFillColor : (guardAfter > 0 ? playerHpGuardFillColor : playerHpFillColor);
        }

        if (_playerHpPreviewFill != null)
        {
            if (data.hpLost > 0)
            {
                _playerHpPreviewFill.fillAmount = Mathf.Clamp01((float)hpBefore / maxHp);
                _playerHpPreviewFill.color = playerHpPreviewDamageColor;
                _playerHpPreviewFill.gameObject.SetActive(true);
            }
            else if (data.hpLost < 0)
            {
                _playerHpPreviewFill.fillAmount = Mathf.Clamp01((float)hpAfter / maxHp);
                _playerHpPreviewFill.color = playerHpHealBlinkColor;
                _playerHpPreviewFill.gameObject.SetActive(true);
            }
            else
            {
                _playerHpPreviewFill.gameObject.SetActive(false);
            }
        }

        if (playerHpText != null)
        {
            if (data.hpLost < 0)
                playerHpText.text = $"{Mathf.Max(0, hpAfter)}/{maxHp} (+{-data.hpLost})";
            else if (data.hpLost > 0)
                playerHpText.text = $"{Mathf.Max(0, hpAfter)}/{maxHp} (-{data.hpLost})";
            else
                playerHpText.text = $"{Mathf.Max(0, hpAfter)}/{maxHp}";
        }

        if (playerHpBarOutline != null)
            playerHpBarOutline.effectColor = (willBeStaggered || guardAfter > 0) ? playerHpProtectedOutlineColor : playerHpOutlineColor;
        if (playerHpBarBackground != null)
            playerHpBarBackground.color = playerHpBarBackgroundColor;

        RefreshPlayerGuardVisual(guardAfter);

        BuildPlayerPreviewStatusBuffer(data);
        ApplyPlayerStatusBuffer();
    }

    public void ClearPlayerTargetPreview()
    {
        if (!_playerTargetPreviewActive)
            return;

        _playerTargetPreviewActive = false;
        if (_playerHpPreviewFill != null)
            _playerHpPreviewFill.gameObject.SetActive(false);
        if (playerHpText != null)
            playerHpText.color = playerHpTextNormalColor;
        if (playerGuardText != null)
            playerGuardText.color = Color.white;

        ForceRefreshPlayerVitals();
    }

    private void EnsurePlayerVitalsUi()
    {
        RectTransform parentRect = ResolvePlayerVitalsParent();
        if (parentRect == null)
            return;

        if (playerHpBarRoot == null)
            playerHpBarRoot = parentRect.Find("PlayerHpBarRoot") as RectTransform;
        if (playerHpBarRoot == null)
            playerHpBarRoot = CreatePlayerHpBarRoot(parentRect);
        if (playerHpBarRoot != null)
        {
            if (playerHpBarBackground == null)
                playerHpBarBackground = playerHpBarRoot.GetComponent<Image>();
            if (playerHpBarOutline == null)
                playerHpBarOutline = playerHpBarRoot.GetComponent<Outline>();
            if (playerHpBarFill == null)
                playerHpBarFill = FindChildComponent<Image>(playerHpBarRoot, "Fill");
            if (playerHpBarFill == null)
                playerHpBarFill = CreatePlayerHpFill(playerHpBarRoot);
            if (playerHpText == null)
                playerHpText = FindChildComponent<TMP_Text>(playerHpBarRoot, "HpText");
            if (playerHpText == null)
                playerHpText = CreatePlayerHpText(playerHpBarRoot);

            if (playerGuardRoot == null)
                playerGuardRoot = playerHpBarRoot.Find("PlayerGuardRoot") as RectTransform;
            if (playerGuardRoot == null)
                playerGuardRoot = playerHpBarRoot.Find("GuardRoot") as RectTransform;
            if (playerGuardRoot == null)
                playerGuardRoot = CreatePlayerGuardRoot(playerHpBarRoot);
            if (playerGuardIcon == null && playerGuardRoot != null)
                playerGuardIcon = FindChildComponent<Image>(playerGuardRoot, "Icon");
            if (playerGuardText == null && playerGuardRoot != null)
                playerGuardText = FindChildComponent<TMP_Text>(playerGuardRoot, "Value");
            RefreshPlayerGuardIcon();
        }

        if (playerStatusRowRoot == null)
            playerStatusRowRoot = parentRect.Find("PlayerStatusRowRoot") as RectTransform;
        if (playerStatusRowRoot == null)
            playerStatusRowRoot = parentRect.Find("StatusRowRoot") as RectTransform;
        if (playerStatusRowRoot == null)
            playerStatusRowRoot = CreatePlayerStatusRowRoot(parentRect);

        ResolvePlayerStatusSlots();
    }

    private RectTransform CreatePlayerHpBarRoot(RectTransform parent)
    {
        GameObject root = new GameObject("PlayerHpBarRoot", typeof(RectTransform), typeof(Image), typeof(Outline));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.sizeDelta = new Vector2(220f, 18f);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = playerFocusBarRoot != null
            ? playerFocusBarRoot.anchoredPosition + new Vector2(0f, 56f)
            : Vector2.zero;

        Image image = root.GetComponent<Image>();
        image.color = playerHpBarBackgroundColor;
        image.raycastTarget = false;

        Outline outline = root.GetComponent<Outline>();
        outline.effectColor = playerHpOutlineColor;
        outline.effectDistance = new Vector2(1f, -1f);
        return rect;
    }

    private Image CreatePlayerHpFill(RectTransform parent)
    {
        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        RectTransform rect = fill.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(2f, 2f);
        rect.offsetMax = new Vector2(-2f, -2f);

        Image image = fill.GetComponent<Image>();
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Horizontal;
        image.fillOrigin = (int)Image.OriginHorizontal.Left;
        image.color = playerHpFillColor;
        image.raycastTarget = false;
        return image;
    }

    private TMP_Text CreatePlayerHpText(RectTransform parent)
    {
        GameObject textObject = new GameObject("HpText", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TMP_Text text = textObject.GetComponent<TMP_Text>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 16f;
        text.color = playerHpTextNormalColor;
        text.raycastTarget = false;
        return text;
    }

    private RectTransform CreatePlayerGuardRoot(RectTransform parent)
    {
        GameObject rootObject = new GameObject("GuardRoot", typeof(RectTransform), typeof(Image));
        RectTransform root = rootObject.GetComponent<RectTransform>();
        root.SetParent(parent, false);
        root.sizeDelta = new Vector2(42f, 42f);
        root.anchorMin = new Vector2(0f, 0.5f);
        root.anchorMax = new Vector2(0f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.anchoredPosition = new Vector2(-14f, 0f);

        Image background = rootObject.GetComponent<Image>();
        background.color = new Color(0.13f, 0.62f, 0.95f, 0.95f);
        background.raycastTarget = false;

        GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.SetParent(root, false);
        iconRect.anchorMin = new Vector2(0.08f, 0.08f);
        iconRect.anchorMax = new Vector2(0.92f, 0.92f);
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;
        playerGuardIcon = iconObject.GetComponent<Image>();
        playerGuardIcon.preserveAspect = true;
        playerGuardIcon.raycastTarget = false;

        GameObject valueObject = new GameObject("Value", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform valueRect = valueObject.GetComponent<RectTransform>();
        valueRect.SetParent(root, false);
        valueRect.anchorMin = Vector2.zero;
        valueRect.anchorMax = Vector2.one;
        valueRect.offsetMin = Vector2.zero;
        valueRect.offsetMax = Vector2.zero;
        playerGuardText = valueObject.GetComponent<TMP_Text>();
        playerGuardText.alignment = TextAlignmentOptions.Center;
        playerGuardText.fontSize = 14f;
        playerGuardText.color = Color.white;
        playerGuardText.raycastTarget = false;
        return root;
    }

    private RectTransform CreatePlayerStatusRowRoot(RectTransform parent)
    {
        GameObject root = new GameObject("PlayerStatusRowRoot", typeof(RectTransform));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.sizeDelta = new Vector2(260f, playerStatusIconSize.y);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = playerFocusBarRoot != null
            ? playerFocusBarRoot.anchoredPosition + new Vector2(0f, -46f)
            : new Vector2(0f, -46f);
        return rect;
    }

    private RectTransform ResolvePlayerVitalsParent()
    {
        if (playerHpBarRoot != null && playerHpBarRoot.parent is RectTransform hpParent)
            return hpParent;
        if (playerFocusText != null && playerFocusText.rectTransform.parent is RectTransform focusTextParent)
            return focusTextParent;
        if (playerFocusBarRoot != null && playerFocusBarRoot.parent is RectTransform focusBarParent)
            return focusBarParent;
        return transform as RectTransform;
    }

    private void RefreshPlayerVitalsIfChanged()
    {
        if (_playerTargetPreviewActive)
        {
            UpdatePlayerTargetPreviewBlink();
            return;
        }

        if (player == null)
            return;

        int statusSignature = BuildStatusSignature(player.status);
        bool staggered = player.status != null && player.status.staggered;
        if (_lastHp == player.hp && _lastMaxHp == player.maxHP && _lastGuard == player.guardPool &&
            _lastStaggered == staggered && _lastStatusSignature == statusSignature)
            return;

        ForceRefreshPlayerVitals();
    }

    private void ForceRefreshPlayerVitals()
    {
        if (player == null)
            return;

        bool staggered = player.status != null && player.status.staggered;
        _lastHp = player.hp;
        _lastMaxHp = player.maxHP;
        _lastGuard = player.guardPool;
        _lastStaggered = staggered;
        _lastStatusSignature = BuildStatusSignature(player.status);

        RefreshPlayerHpAndGuard(player.hp, player.maxHP, player.guardPool, staggered);
        BuildPlayerStatusBuffer(player.status, player.guardPool);
        ApplyPlayerStatusBuffer();
    }

    private void RefreshPlayerHpAndGuard(int hp, int maxHp, int guard, bool staggered)
    {
        int safeHp = Mathf.Max(0, hp);
        int safeMaxHp = Mathf.Max(1, maxHp);

        if (playerHpText != null)
        {
            playerHpText.text = $"{safeHp}/{safeMaxHp}";
            playerHpText.color = staggered ? playerHpTextStaggerColor : playerHpTextNormalColor;
        }

        if (playerHpBarBackground != null)
            playerHpBarBackground.color = playerHpBarBackgroundColor;
        if (playerHpBarOutline != null)
            playerHpBarOutline.effectColor = (staggered || guard > 0) ? playerHpProtectedOutlineColor : playerHpOutlineColor;
        if (playerHpBarFill != null)
        {
            playerHpBarFill.fillAmount = Mathf.Clamp01((float)safeHp / safeMaxHp);
            playerHpBarFill.color = staggered ? playerHpStaggerFillColor : (guard > 0 ? playerHpGuardFillColor : playerHpFillColor);
        }

        RefreshPlayerGuardVisual(guard);
    }

    private void RefreshPlayerGuardVisual(int guard)
    {
        if (playerGuardRoot != null && Application.isPlaying && autoTogglePlayerGuardRootInPlayMode)
            playerGuardRoot.gameObject.SetActive(guard > 0);
        if (playerGuardText != null)
        {
            playerGuardText.text = Mathf.Max(0, guard).ToString();
            playerGuardText.color = Color.white;
        }
    }

    private void RefreshPlayerGuardIcon()
    {
        if (playerGuardIcon == null)
            return;

        if (playerIconLibrary != null && playerIconLibrary.TryGetStatusIcon(CombatUiStatusIconKind.Guard, out Sprite sprite, out _, out _))
        {
            playerGuardIcon.sprite = sprite;
            playerGuardIcon.color = Color.white;
            playerGuardIcon.enabled = sprite != null;
        }
    }

    private void EnsurePlayerHpPreviewFill()
    {
        if (_playerHpPreviewFill != null || playerHpBarFill == null)
            return;

        GameObject go = new GameObject("PlayerHpPreviewFill", typeof(RectTransform), typeof(Image));
        RectTransform rt = go.GetComponent<RectTransform>();
        RectTransform fillRt = playerHpBarFill.rectTransform;
        rt.SetParent(fillRt.parent, false);
        rt.anchorMin = fillRt.anchorMin;
        rt.anchorMax = fillRt.anchorMax;
        rt.pivot = fillRt.pivot;
        rt.offsetMin = fillRt.offsetMin;
        rt.offsetMax = fillRt.offsetMax;
        rt.sizeDelta = fillRt.sizeDelta;
        rt.anchoredPosition = fillRt.anchoredPosition;
        rt.SetSiblingIndex(fillRt.GetSiblingIndex());

        if (playerHpText != null)
            playerHpText.rectTransform.SetAsLastSibling();

        _playerHpPreviewFill = go.GetComponent<Image>();
        _playerHpPreviewFill.sprite = playerHpBarFill.sprite;
        _playerHpPreviewFill.type = Image.Type.Filled;
        _playerHpPreviewFill.fillMethod = playerHpBarFill.fillMethod;
        _playerHpPreviewFill.fillOrigin = playerHpBarFill.fillOrigin;
        _playerHpPreviewFill.color = playerHpPreviewDamageColor;
        _playerHpPreviewFill.raycastTarget = false;
        go.SetActive(false);
    }

    private void UpdatePlayerTargetPreviewBlink()
    {
        float t = Mathf.PingPong(Time.time * playerHpPreviewBlinkSpeed, 1f);

        if (_playerHpPreviewFill != null && _playerHpPreviewFill.gameObject.activeSelf)
        {
            Color baseColor = _playerPreviewData.hpLost < 0 ? playerHpHealBlinkColor : playerHpPreviewDamageColor;
            Color color = baseColor;
            color.a = Mathf.Lerp(playerHpPreviewMinAlpha, baseColor.a, t);
            _playerHpPreviewFill.color = color;
        }

        if (playerHpText != null && _playerPreviewData.hpLost != 0)
        {
            Color baseColor = _playerPreviewData.hpLost < 0 ? playerHpHealBlinkColor : playerHpPreviewDamageColor;
            playerHpText.color = Color.Lerp(baseColor, Color.white, t);
        }

        if (playerGuardText != null && _playerPreviewData.previewGuardAfter != _playerPreviewData.currentGuard)
            playerGuardText.color = Color.Lerp(playerHpPreviewDamageColor, Color.white, t);

        for (int i = 0; i < playerStatusSlots.Length; i++)
        {
            if (i >= _playerStatusBuffer.Count)
                break;

            ActorWorldUI.StatusIconSlot slot = playerStatusSlots[i];
            PlayerStatusVisualData data = _playerStatusBuffer[i];
            if (slot == null || slot.root == null || !slot.root.gameObject.activeSelf)
                continue;

            bool isBlinking = false;
            bool isConsume = false;
            if (data.shortLabel == "BU")
            {
                if (_playerPreviewData.previewBurnAfter > _playerPreviewData.currentBurn)
                    isBlinking = true;
                else if (_playerPreviewData.previewBurnAfter < _playerPreviewData.currentBurn)
                {
                    isBlinking = true;
                    isConsume = true;
                }
            }
            else if (data.shortLabel == "BL")
            {
                if (_playerPreviewData.previewBleedAfter > _playerPreviewData.currentBleed)
                    isBlinking = true;
                else if (_playerPreviewData.previewBleedAfter < _playerPreviewData.currentBleed)
                {
                    isBlinking = true;
                    isConsume = true;
                }
            }
            else if (data.shortLabel == "MK")
            {
                isBlinking = _playerPreviewData.willTriggerMarkShock;
            }

            if (!isBlinking)
                continue;

            Color blinkColor = Color.Lerp(playerHpPreviewDamageColor, Color.white, t);
            if (isConsume && slot.iconImage != null)
                slot.iconImage.color = blinkColor;
            if (slot.valueText != null)
                slot.valueText.color = blinkColor;
        }
    }

    private void BuildPlayerStatusBuffer(StatusController status, int guard)
    {
        _playerStatusBuffer.Clear();
        if (status == null)
            return;

        if (status.frozen)
            AddPlayerStatusVisual(CombatUiStatusIconKind.Freeze, "FR", string.Empty, new Color(0.4f, 0.78f, 1f, 0.96f));
        if (status.chilledTurns > 0)
            AddPlayerStatusVisual(CombatUiStatusIconKind.Chilled, "CH", status.chilledTurns.ToString(), new Color(0.58f, 0.9f, 1f, 0.96f));
        if (status.marked)
            AddPlayerStatusVisual(CombatUiStatusIconKind.Mark, "MK", string.Empty, new Color(1f, 0.88f, 0.28f, 0.96f));
        if (status.burnStacks > 0)
            AddPlayerStatusVisual(CombatUiStatusIconKind.Burn, "BU", status.burnStacks.ToString(), new Color(1f, 0.42f, 0.22f, 0.96f));
        if (status.bleedStacks > 0)
            AddPlayerStatusVisual(CombatUiStatusIconKind.Bleed, "BL", status.bleedStacks.ToString(), new Color(0.82f, 0.14f, 0.2f, 0.96f));
        if (status.HasAilment(out AilmentType ailment, out int turnsLeft))
            AddPlayerStatusVisual(CombatUiStatusIconKind.Ailment, GetPlayerAilmentShortLabel(ailment), Mathf.Max(1, turnsLeft).ToString(), new Color(0.72f, 0.5f, 1f, 0.96f));
    }

    private void BuildPlayerPreviewStatusBuffer(TargetPreviewData data)
    {
        _playerStatusBuffer.Clear();
        if (data.previewFrozenAfter)
            AddPlayerStatusVisual(CombatUiStatusIconKind.Freeze, "FR", string.Empty, new Color(0.4f, 0.78f, 1f, 0.96f));
        if (player != null && player.status != null && !data.previewFrozenAfter && player.status.chilledTurns > 0)
            AddPlayerStatusVisual(CombatUiStatusIconKind.Chilled, "CH", player.status.chilledTurns.ToString(), new Color(0.58f, 0.9f, 1f, 0.96f));
        if (data.previewMarkedAfter)
            AddPlayerStatusVisual(CombatUiStatusIconKind.Mark, "MK", string.Empty, new Color(1f, 0.88f, 0.28f, 0.96f));
        if (data.previewBurnAfter > 0 || data.currentBurn > 0)
            AddPlayerStatusVisual(CombatUiStatusIconKind.Burn, "BU", data.previewBurnAfter.ToString(), new Color(1f, 0.42f, 0.22f, 0.96f));
        if (data.previewBleedAfter > 0 || data.currentBleed > 0)
            AddPlayerStatusVisual(CombatUiStatusIconKind.Bleed, "BL", data.previewBleedAfter.ToString(), new Color(0.82f, 0.14f, 0.2f, 0.96f));
        if (player != null && player.status != null && player.status.HasAilment(out AilmentType ailment, out int turnsLeft))
            AddPlayerStatusVisual(CombatUiStatusIconKind.Ailment, GetPlayerAilmentShortLabel(ailment), Mathf.Max(1, turnsLeft).ToString(), new Color(0.72f, 0.5f, 1f, 0.96f));
    }

    private void ApplyPlayerStatusBuffer()
    {
        ResolvePlayerStatusSlots();
        if (playerStatusSlots == null)
            return;

        if (playerStatusSlotTemplateRoot != null)
            playerStatusSlotTemplateRoot.gameObject.SetActive(false);

        for (int i = 0; i < playerStatusSlots.Length; i++)
        {
            ActorWorldUI.StatusIconSlot slot = playerStatusSlots[i];
            if (slot == null || slot.root == null)
                continue;

            bool show = i < _playerStatusBuffer.Count;
            slot.root.gameObject.SetActive(show);
            if (!show)
                continue;

            PlayerStatusVisualData data = _playerStatusBuffer[i];
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

    private void ResolvePlayerStatusSlots()
    {
        if (playerStatusRowRoot == null)
            return;

        if (playerStatusSlots == null || playerStatusSlots.Length != DefaultPlayerStatusSlotCount)
            playerStatusSlots = new ActorWorldUI.StatusIconSlot[DefaultPlayerStatusSlotCount];

        if (playerStatusSlotTemplateRoot == null)
            playerStatusSlotTemplateRoot = playerStatusRowRoot.Find("Status_1") as RectTransform;

        for (int i = 0; i < playerStatusSlots.Length; i++)
        {
            if (playerStatusSlots[i] != null && playerStatusSlots[i].root != null)
                continue;

            RectTransform root = playerStatusRowRoot.Find($"Status_{i + 1}") as RectTransform;
            if (root == null && playerStatusSlotTemplateRoot != null)
            {
                root = Instantiate(playerStatusSlotTemplateRoot, playerStatusRowRoot);
                root.name = $"Status_{i + 1}";
            }
            if (root == null)
                root = CreatePlayerStatusSlotRoot(playerStatusRowRoot, i);
            if (root == null)
                continue;

            root.sizeDelta = playerStatusIconSize;
            playerStatusSlots[i] = CreatePlayerStatusSlot(root);
        }
    }

    private RectTransform CreatePlayerStatusSlotRoot(RectTransform parent, int index)
    {
        GameObject rootObject = new GameObject($"Status_{index + 1}", typeof(RectTransform), typeof(Image));
        RectTransform root = rootObject.GetComponent<RectTransform>();
        root.SetParent(parent, false);
        root.sizeDelta = playerStatusIconSize;
        root.anchorMin = new Vector2(0f, 0.5f);
        root.anchorMax = new Vector2(0f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.anchoredPosition = new Vector2(index * (playerStatusIconSize.x + 8f) + playerStatusIconSize.x * 0.5f, 0f);

        Image background = rootObject.GetComponent<Image>();
        background.color = new Color(0.08f, 0.1f, 0.15f, 0.95f);
        background.raycastTarget = false;

        GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.SetParent(root, false);
        iconRect.anchorMin = new Vector2(0.12f, 0.12f);
        iconRect.anchorMax = new Vector2(0.62f, 0.88f);
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;
        Image icon = iconObject.GetComponent<Image>();
        icon.preserveAspect = true;
        icon.raycastTarget = false;

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.SetParent(root, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        TMP_Text label = labelObject.GetComponent<TMP_Text>();
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 14f;
        label.color = Color.white;
        label.raycastTarget = false;

        GameObject valueObject = new GameObject("Value", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform valueRect = valueObject.GetComponent<RectTransform>();
        valueRect.SetParent(root, false);
        valueRect.anchorMin = new Vector2(0.5f, 0f);
        valueRect.anchorMax = new Vector2(1f, 0.55f);
        valueRect.offsetMin = Vector2.zero;
        valueRect.offsetMax = Vector2.zero;
        TMP_Text value = valueObject.GetComponent<TMP_Text>();
        value.alignment = TextAlignmentOptions.Center;
        value.fontSize = 13f;
        value.color = Color.white;
        value.raycastTarget = false;
        return root;
    }

    private ActorWorldUI.StatusIconSlot CreatePlayerStatusSlot(RectTransform root)
    {
        return new ActorWorldUI.StatusIconSlot
        {
            root = root,
            background = root.GetComponent<Image>(),
            iconImage = FindChildComponent<Image>(root, "Icon"),
            shortLabelText = FindChildComponent<TMP_Text>(root, "Label"),
            valueText = FindChildComponent<TMP_Text>(root, "Value")
        };
    }

    private void AddPlayerStatusVisual(CombatUiStatusIconKind kind, string shortLabel, string valueText, Color fallbackBackground)
    {
        if (playerIconLibrary != null && playerIconLibrary.TryGetStatusIcon(kind, out Sprite sprite, out Color backgroundColor, out _))
            _playerStatusBuffer.Add(new PlayerStatusVisualData(sprite, shortLabel, valueText, backgroundColor));
        else
            _playerStatusBuffer.Add(new PlayerStatusVisualData(null, shortLabel, valueText, fallbackBackground));
    }

    private int BuildStatusSignature(StatusController status)
    {
        if (status == null)
            return 0;

        unchecked
        {
            int hash = 17;
            hash = hash * 31 + (status.frozen ? 1 : 0);
            hash = hash * 31 + status.chilledTurns;
            hash = hash * 31 + (status.marked ? 1 : 0);
            hash = hash * 31 + status.burnStacks;
            hash = hash * 31 + status.bleedStacks;
            hash = hash * 31 + (status.staggered ? 1 : 0);
            if (status.HasAilment(out AilmentType ailment, out int turnsLeft))
            {
                hash = hash * 31 + (int)ailment;
                hash = hash * 31 + turnsLeft;
            }
            return hash;
        }
    }

    private static T FindChildComponent<T>(Transform root, string childName) where T : Component
    {
        if (root == null)
            return null;

        Transform child = root.Find(childName);
        if (child != null && child.TryGetComponent(out T component))
            return component;

        for (int i = 0; i < root.childCount; i++)
        {
            component = root.GetChild(i).GetComponentInChildren<T>(true);
            if (component != null && (string.IsNullOrEmpty(childName) || component.name == childName))
                return component;
        }

        return null;
    }

    private static string GetPlayerAilmentShortLabel(AilmentType ailment)
    {
        string text = ailment.ToString();
        return text.Length <= 2 ? text.ToUpperInvariant() : text.Substring(0, 2).ToUpperInvariant();
    }
}
