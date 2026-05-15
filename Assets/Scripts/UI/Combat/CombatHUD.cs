using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CombatHUD : MonoBehaviour
{
    private const int DefaultFocusSegmentCount = 9;

    [Header("Auto Resolve")]
    public BattlePartyManager2D party;
    public TurnManager turnManager;
    public CombatActor player;

    [Header("Player UI")]
    public TMP_Text playerHpText;
    public TMP_Text playerFocusText;
    public TMP_Text playerGuardText;
    public TMP_Text playerStatusText;
    public bool hideLegacyPlayerStats = true;

    [Header("Player Focus Bar")]
    public RectTransform playerFocusBarRoot;
    public RectTransform playerFocusSegmentsRoot;
    public Image[] playerFocusSegments = new Image[DefaultFocusSegmentCount];
    public Color playerFocusFilledColor = new Color(0.14f, 0.66f, 0.95f, 1f);
    public Color playerFocusEmptyColor = new Color(0.1f, 0.16f, 0.24f, 0.95f);
    public Color playerFocusPreviewColor = new Color(1f, 0.85f, 0.2f, 1f);
    public Color playerFocusInvalidColor = new Color(0.8f, 0.2f, 0.2f, 0.85f);
    public Vector2 focusBarSize = new Vector2(152f, 18f);
    public Vector2 focusSegmentSize = new Vector2(14f, 14f);
    public float focusSegmentSpacing = 2f;
    public float focusNumberWidth = 28f;
    [Range(1f, 6f)]
    public float focusPreviewBlinkSpeed = 3f;
    
    [Header("Dice Consume Preview")]
    [Range(0f, 10f)]
    public float consumePreviewBlinkSpeed = 3f;
    [Range(0f, 1f)]
    public float consumePreviewMinAlpha = 0.5f;
    [Range(0f, 1f)]
    public float consumePreviewInvalidMinAlpha = 0.6f;

    private int _lastHp = int.MinValue;
    private int _lastMaxHp = int.MinValue;
    private int _lastFocus = int.MinValue;
    private int _lastMaxFocus = int.MinValue;
    private int _lastGuard = int.MinValue;
    private string _lastStatusText;

    // --- Focus preview state ---
    private int _previewFocusCost;
    private int _previewFocusGain;
    private bool _previewFocusActive;
    private bool _previewFocusInvalid;
    private Image _focusBarBackgroundImage;
    private Color _focusBarBackgroundOriginalColor;
    private Color _playerFocusTextOriginalColor = Color.white;

    void Awake()
    {
        TryResolveRefs();
        HideLegacyPlayerStatTexts();
        EnsurePlayerFocusBarUi();

        if (playerFocusText != null && !_previewFocusActive)
            _playerFocusTextOriginalColor = playerFocusText.color;
    }

    void Update()
    {
        if (!player)
            TryResolveRefs();
        if (!player)
            return;

        HideLegacyPlayerStatTexts();
        EnsurePlayerFocusBarUi();

        if (playerFocusText != null && !_previewFocusActive)
            _playerFocusTextOriginalColor = playerFocusText.color;

        if (playerFocusText && (_lastFocus != player.focus || _lastMaxFocus != player.maxFocus))
        {
            _lastFocus = player.focus;
            _lastMaxFocus = player.maxFocus;
            playerFocusText.text = player.focus.ToString();
            RefreshPlayerFocusSegments();
        }

        if (_previewFocusActive)
            UpdateFocusPreviewBlink();
    }

    public void SetupPlayerFocusBarUi()
    {
        EnsurePlayerFocusBarUi();
        if (playerFocusText != null && player != null)
        {
            playerFocusText.text = player.focus.ToString();
            playerFocusText.color = _playerFocusTextOriginalColor;
        }

        RefreshPlayerFocusSegments();
    }

    void TryResolveRefs()
    {
        if (!party)
            party = FindObjectOfType<BattlePartyManager2D>(true);
        if (!turnManager)
            turnManager = FindObjectOfType<TurnManager>(true);

        if (!player && party != null && party.Player != null)
            player = party.Player;

        if (!player && turnManager != null && turnManager.player != null)
            player = turnManager.player;

        if (!player)
        {
            var all = FindObjectsOfType<CombatActor>(true);
            foreach (var a in all)
            {
                if (!a)
                    continue;
                if (a.name.ToLower().Contains("player"))
                {
                    player = a;
                    break;
                }
            }
        }
    }

    private void EnsurePlayerFocusBarUi()
    {
        RectTransform labelRect = playerFocusText != null ? playerFocusText.rectTransform : null;
        RectTransform parentRect = labelRect != null ? labelRect.parent as RectTransform : transform as RectTransform;
        if (parentRect == null)
            return;

        bool createdRoot = false;
        if (playerFocusBarRoot == null)
        {
            Transform existingRoot = parentRect.Find("PlayerFocusBarRoot");
            if (existingRoot != null)
                playerFocusBarRoot = existingRoot as RectTransform;
        }

        if (playerFocusBarRoot == null)
        {
            GameObject rootGo = new GameObject("PlayerFocusBarRoot", typeof(RectTransform));
            playerFocusBarRoot = rootGo.GetComponent<RectTransform>();
            playerFocusBarRoot.SetParent(parentRect, false);
            createdRoot = true;
        }

        Image bgImg = playerFocusBarRoot.GetComponent<Image>();
        if (bgImg == null)
        {
            bgImg = playerFocusBarRoot.gameObject.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.4f);
            bgImg.raycastTarget = false;
        }

        if (createdRoot && labelRect != null)
        {
            playerFocusBarRoot.anchorMin = labelRect.anchorMin;
            playerFocusBarRoot.anchorMax = labelRect.anchorMax;
            playerFocusBarRoot.pivot = labelRect.pivot;
            playerFocusBarRoot.anchoredPosition = labelRect.anchoredPosition;
        }
        else if (createdRoot)
        {
            playerFocusBarRoot.anchorMin = new Vector2(0f, 1f);
            playerFocusBarRoot.anchorMax = new Vector2(0f, 1f);
            playerFocusBarRoot.pivot = new Vector2(0f, 1f);
            playerFocusBarRoot.anchoredPosition = new Vector2(24f, -24f);
        }

        if (createdRoot)
            playerFocusBarRoot.sizeDelta = new Vector2(focusNumberWidth + 6f + focusBarSize.x, Mathf.Max(24f, focusBarSize.y));

        bool createdValueText = false;
        if (playerFocusText == null)
        {
            playerFocusText = CreateHudText("PlayerFocusValue", playerFocusBarRoot, "0", 18f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            createdValueText = true;
        }

        playerFocusText.raycastTarget = false;
        playerFocusText.enableWordWrapping = false;
        RectTransform valueRect = playerFocusText.rectTransform;
        if (createdValueText)
        {
            valueRect.anchorMin = new Vector2(0f, 0.5f);
            valueRect.anchorMax = new Vector2(0f, 0.5f);
            valueRect.pivot = new Vector2(0f, 0.5f);
            valueRect.anchoredPosition = Vector2.zero;
            valueRect.sizeDelta = new Vector2(focusNumberWidth, 24f);
        }

        bool createdSegmentsRoot = false;
        if (playerFocusSegmentsRoot == null)
        {
            Transform existingSegments = playerFocusBarRoot.Find("Segments");
            if (existingSegments != null)
                playerFocusSegmentsRoot = existingSegments as RectTransform;
        }

        if (playerFocusSegmentsRoot == null)
        {
            GameObject segmentsGo = new GameObject("Segments", typeof(RectTransform));
            playerFocusSegmentsRoot = segmentsGo.GetComponent<RectTransform>();
            playerFocusSegmentsRoot.SetParent(playerFocusBarRoot, false);
            createdSegmentsRoot = true;
        }

        if (createdSegmentsRoot)
        {
            playerFocusSegmentsRoot.anchorMin = new Vector2(0f, 0.5f);
            playerFocusSegmentsRoot.anchorMax = new Vector2(0f, 0.5f);
            playerFocusSegmentsRoot.pivot = new Vector2(0f, 0.5f);
            playerFocusSegmentsRoot.anchoredPosition = new Vector2(focusNumberWidth + 6f, 0f);
            playerFocusSegmentsRoot.sizeDelta = focusBarSize;
        }

        int segmentCount = Mathf.Max(1, DefaultFocusSegmentCount);
        if (playerFocusSegments == null || playerFocusSegments.Length != segmentCount)
            playerFocusSegments = new Image[segmentCount];

        float totalSpacing = focusSegmentSpacing * (segmentCount - 1);
        float availableWidth = Mathf.Max(1f, focusBarSize.x - totalSpacing);
        float segmentWidth = Mathf.Min(focusSegmentSize.x, availableWidth / segmentCount);

        for (int i = 0; i < segmentCount; i++)
        {
            Image segment = playerFocusSegments[i];
            if (segment == null)
            {
                Transform existingSegment = playerFocusSegmentsRoot.Find($"Segment_{i + 1}");
                if (existingSegment != null)
                    segment = existingSegment.GetComponent<Image>();
            }

            if (segment == null)
            {
                GameObject segmentGo = new GameObject($"Segment_{i + 1}", typeof(RectTransform), typeof(Image));
                RectTransform segmentRect = segmentGo.GetComponent<RectTransform>();
                segmentRect.SetParent(playerFocusSegmentsRoot, false);
                segment = segmentGo.GetComponent<Image>();
                RectTransform rect = segment.rectTransform;
                rect.anchorMin = new Vector2(0f, 0.5f);
                rect.anchorMax = new Vector2(0f, 0.5f);
                rect.pivot = new Vector2(0f, 0.5f);
                rect.sizeDelta = new Vector2(segmentWidth, focusSegmentSize.y);
                rect.anchoredPosition = new Vector2(i * (segmentWidth + focusSegmentSpacing), 0f);
            }

            playerFocusSegments[i] = segment;

            segment.type = Image.Type.Simple;
            segment.raycastTarget = false;
        }
    }

    private void RefreshPlayerFocusSegments()
    {
        if (playerFocusSegments == null)
            return;

        int current = player != null ? Mathf.Max(0, player.focus) : 0;
        int max = player != null ? Mathf.Clamp(player.maxFocus, 0, playerFocusSegments.Length) : playerFocusSegments.Length;

        for (int i = 0; i < playerFocusSegments.Length; i++)
        {
            Image segment = playerFocusSegments[i];
            if (segment == null)
                continue;

            bool withinMax = i < max;
            bool filled = i < current;
            segment.color = withinMax
                ? (filled ? playerFocusFilledColor : playerFocusEmptyColor)
                : new Color(playerFocusEmptyColor.r, playerFocusEmptyColor.g, playerFocusEmptyColor.b, 0.25f);
        }
    }

    // ---------------------------
    // Focus Preview API
    // ---------------------------

    /// <summary>
    /// Hiển thị preview tiêu hao Focus khi hover/drag skill.
    /// cost = số Focus skill sẽ dùng (đã tính modifier).
    /// isInvalid = true nếu player không đủ Focus.
    /// </summary>
    public void ShowFocusPreview(int cost, int gain, bool isInvalid)
    {
        _previewFocusCost = Mathf.Max(0, cost);
        _previewFocusGain = Mathf.Max(0, gain);
        _previewFocusInvalid = isInvalid;
        _previewFocusActive = true;

        // Cache background để đổi màu đỏ khi invalid
        if (_focusBarBackgroundImage == null && playerFocusBarRoot != null)
        {
            _focusBarBackgroundImage = playerFocusBarRoot.GetComponent<Image>();
            if (_focusBarBackgroundImage != null)
                _focusBarBackgroundOriginalColor = _focusBarBackgroundImage.color;
        }

        if (_previewFocusInvalid && _focusBarBackgroundImage != null)
            _focusBarBackgroundImage.color = playerFocusInvalidColor;
    }

    /// <summary>
    /// Tắt preview Focus, trả về trạng thái bình thường.
    /// </summary>
    public void ClearFocusPreview()
    {
        _previewFocusActive = false;
        _previewFocusCost = 0;
        _previewFocusGain = 0;
        _previewFocusInvalid = false;

        // Restore background color
        if (_focusBarBackgroundImage != null)
            _focusBarBackgroundImage.color = _focusBarBackgroundOriginalColor;

        if (playerFocusText != null && player != null)
        {
            playerFocusText.text = player.focus.ToString();
            playerFocusText.color = _playerFocusTextOriginalColor;
        }

        // Redraw segments bình thường
        RefreshPlayerFocusSegments();
    }

    private void UpdateFocusPreviewBlink()
    {
        if (playerFocusSegments == null || player == null)
            return;

        int current = Mathf.Max(0, player.focus);
        int max = Mathf.Clamp(player.maxFocus, 0, playerFocusSegments.Length);

        // Tính alpha nhấp nháy: dao động 0.4 -> 1.0
        float t = Mathf.PingPong(Time.time * focusPreviewBlinkSpeed, 1f);
        float blinkAlpha = Mathf.Lerp(0.4f, 1f, t);
        int previewFinalFocus = Mathf.Clamp(current - _previewFocusCost + _previewFocusGain, 0, player.maxFocus);

        if (playerFocusText != null)
        {
            bool hasFocusChange = _previewFocusCost != 0 || _previewFocusGain != 0;
            playerFocusText.text = hasFocusChange ? previewFinalFocus.ToString() : current.ToString();

            if (hasFocusChange)
            {
                Color textColor;
                if (_previewFocusInvalid)
                {
                    textColor = playerFocusInvalidColor;
                }
                else if (_previewFocusGain > _previewFocusCost)
                {
                    DamagePopupSystem popups = FindObjectOfType<DamagePopupSystem>();
                    textColor = popups != null ? popups.focusGainColor : new Color(0.22f, 0.74f, 1f, 1f);
                }
                else
                {
                    textColor = playerFocusPreviewColor;
                }

                textColor.a = blinkAlpha;
                playerFocusText.color = textColor;
            }
            else
            {
                playerFocusText.color = _playerFocusTextOriginalColor;
            }
        }

        // Segment nào sẽ bị tiêu: từ (current - cost) đến (current - 1)
        int previewStart = current - _previewFocusCost;

        // Segment nào sẽ được hồi: từ current đến (current + gain - 1)
        int gainStart = current;
        int gainEnd = current + _previewFocusGain - 1;

        for (int i = 0; i < playerFocusSegments.Length; i++)
        {
            Image segment = playerFocusSegments[i];
            if (segment == null)
                continue;

            bool withinMax = i < max;
            bool filled = i < current;

            if (!withinMax)
            {
                segment.color = new Color(playerFocusEmptyColor.r, playerFocusEmptyColor.g, playerFocusEmptyColor.b, 0.25f);
                continue;
            }

            // Segment nằm trong vùng sẽ bị tiêu hao
            bool isPreviewConsumed = filled && i >= previewStart && i < current;
            
            // Segment nằm trong vùng sẽ được hồi thêm
            bool isPreviewGained = !filled && i >= gainStart && i <= gainEnd;

            if (isPreviewConsumed)
            {
                Color preview = playerFocusPreviewColor;
                preview.a = blinkAlpha;
                segment.color = preview;
            }
            else if (isPreviewGained)
            {
                // Thay vì dùng màu FocusGain của DamagePopupSystem (màu cyan), ta có thể dùng màu đó.
                Color gainColor = new Color(0.22f, 0.74f, 1f, 1f); // default xanh biển
                DamagePopupSystem popups = FindObjectOfType<DamagePopupSystem>();
                if (popups != null) gainColor = popups.focusGainColor;

                gainColor.a = blinkAlpha;
                segment.color = gainColor;
            }
            else
            {
                segment.color = filled ? playerFocusFilledColor : playerFocusEmptyColor;
            }
        }

        if (_previewFocusInvalid && _focusBarBackgroundImage != null)
        {
            Color bg = playerFocusInvalidColor;
            bg.a = blinkAlpha * playerFocusInvalidColor.a;
            _focusBarBackgroundImage.color = bg;
        }
    }

    private static TMP_Text CreateHudText(string name, RectTransform parent, string initialText, float fontSize, FontStyles style, TextAlignmentOptions alignment)
    {
        GameObject textGo = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rect = textGo.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;

        TextMeshProUGUI text = textGo.GetComponent<TextMeshProUGUI>();
        text.text = initialText;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        text.enableWordWrapping = false;
        return text;
    }

    private void HideLegacyPlayerStatTexts()
    {
        if (!hideLegacyPlayerStats)
            return;

        if (playerHpText != null)
            playerHpText.gameObject.SetActive(false);
        if (playerGuardText != null)
            playerGuardText.gameObject.SetActive(false);
        if (playerStatusText != null)
            playerStatusText.gameObject.SetActive(false);
    }

    string BuildStatusString(StatusController st)
    {
        if (!st)
            return string.Empty;

        var sb = new StringBuilder(64);
        bool first = true;

        void Add(string s)
        {
            if (!first)
                sb.Append("  ");
            sb.Append(s);
            first = false;
        }

        if (st.burnStacks > 0) Add($"Burn:{st.burnStacks}");
        if (st.bleedStacks > 0) Add($"Bleed:{st.bleedStacks}");
        if (st.marked) Add("Mark");
        if (st.staggered) Add("Stagger");
        if (st.frozen) Add("Freeze");

        return sb.ToString();
    }
}
