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

    [Header("Player Focus")]
    public TMP_Text playerFocusText;
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
    [Range(1f, 6f)] public float focusPreviewBlinkSpeed = 3f;

    [Header("Dice Consume Preview")]
    [Range(0f, 10f)] public float consumePreviewBlinkSpeed = 3f;
    [Range(0f, 1f)] public float consumePreviewMinAlpha = 0.5f;
    [Range(0f, 1f)] public float consumePreviewInvalidMinAlpha = 0.6f;

    private int _lastFocus = int.MinValue;
    private int _lastMaxFocus = int.MinValue;
    private int _previewFocusCost;
    private int _previewFocusGain;
    private bool _previewFocusActive;
    private bool _previewFocusInvalid;
    private Image _focusBarBackgroundImage;
    private Color _focusBarBackgroundOriginalColor;
    private Color _playerFocusTextOriginalColor = Color.white;

    private void Awake()
    {
        TryResolveRefs();
        EnsurePlayerFocusBarUi();
        CacheOriginalColors();
    }

    private void Update()
    {
        if (player == null)
            TryResolveRefs();
        if (player == null)
            return;

        EnsurePlayerFocusBarUi();
        CacheOriginalColors();

        if (_previewFocusActive)
        {
            UpdateFocusPreviewBlink();
            return;
        }

        RefreshFocusIfChanged();
    }

    public void SetupPlayerFocusBarUi()
    {
        EnsurePlayerFocusBarUi();
        CacheOriginalColors();
        ForceRefreshFocus();
    }

    public void ShowFocusPreview(int cost, int gain, bool isInvalid)
    {
        _previewFocusCost = Mathf.Max(0, cost);
        _previewFocusGain = Mathf.Max(0, gain);
        _previewFocusActive = _previewFocusCost > 0 || _previewFocusGain > 0 || isInvalid;
        _previewFocusInvalid = isInvalid;

        EnsurePlayerFocusBarUi();
        if (_focusBarBackgroundImage == null && playerFocusBarRoot != null)
        {
            _focusBarBackgroundImage = playerFocusBarRoot.GetComponent<Image>();
            if (_focusBarBackgroundImage != null)
                _focusBarBackgroundOriginalColor = _focusBarBackgroundImage.color;
        }

        RefreshPlayerFocusSegments();
    }

    public void ClearFocusPreview()
    {
        _previewFocusActive = false;
        _previewFocusInvalid = false;
        _previewFocusCost = 0;
        _previewFocusGain = 0;

        if (_focusBarBackgroundImage != null)
            _focusBarBackgroundImage.color = _focusBarBackgroundOriginalColor;

        ForceRefreshFocus();
    }

    private void TryResolveRefs()
    {
        if (party == null)
            party = FindObjectOfType<BattlePartyManager2D>(true);
        if (turnManager == null)
            turnManager = FindObjectOfType<TurnManager>(true);
        if (player == null && party != null)
            player = party.Player;
        if (player == null && turnManager != null)
            player = turnManager.player;
    }

    private void CacheOriginalColors()
    {
        if (playerFocusText != null && !_previewFocusActive)
            _playerFocusTextOriginalColor = playerFocusText.color;
    }

    private void RefreshFocusIfChanged()
    {
        if (_lastFocus == player.focus && _lastMaxFocus == player.maxFocus)
            return;

        ForceRefreshFocus();
    }

    private void ForceRefreshFocus()
    {
        if (player == null)
            return;

        _lastFocus = player.focus;
        _lastMaxFocus = player.maxFocus;
        if (playerFocusText != null)
        {
            playerFocusText.text = player.focus.ToString();
            playerFocusText.color = _playerFocusTextOriginalColor;
        }

        RefreshPlayerFocusSegments();
    }

    private void EnsurePlayerFocusBarUi()
    {
        RectTransform labelRect = playerFocusText != null ? playerFocusText.rectTransform : null;
        RectTransform parentRect = labelRect != null ? labelRect.parent as RectTransform : transform as RectTransform;
        if (parentRect == null)
            return;

        if (playerFocusBarRoot == null)
            playerFocusBarRoot = parentRect.Find("PlayerFocusBarRoot") as RectTransform;
        if (playerFocusBarRoot == null)
            playerFocusBarRoot = CreateFocusBarRoot(parentRect);

        if (_focusBarBackgroundImage == null)
        {
            _focusBarBackgroundImage = playerFocusBarRoot.GetComponent<Image>();
            if (_focusBarBackgroundImage != null)
                _focusBarBackgroundOriginalColor = _focusBarBackgroundImage.color;
        }

        if (playerFocusSegmentsRoot == null)
            playerFocusSegmentsRoot = playerFocusBarRoot.Find("Segments") as RectTransform;
        if (playerFocusSegmentsRoot == null)
            playerFocusSegmentsRoot = CreateSegmentsRoot(playerFocusBarRoot);

        EnsureFocusSegments();
    }

    private RectTransform CreateFocusBarRoot(RectTransform parent)
    {
        GameObject root = new GameObject("PlayerFocusBarRoot", typeof(RectTransform), typeof(Image));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.sizeDelta = focusBarSize;
        Image image = root.GetComponent<Image>();
        image.color = playerFocusEmptyColor;
        return rect;
    }

    private RectTransform CreateSegmentsRoot(RectTransform parent)
    {
        GameObject root = new GameObject("Segments", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        HorizontalLayoutGroup layout = root.GetComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlHeight = false;
        layout.childControlWidth = false;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;
        layout.spacing = focusSegmentSpacing;
        return rect;
    }

    private void EnsureFocusSegments()
    {
        if (playerFocusSegments == null || playerFocusSegments.Length != DefaultFocusSegmentCount)
            playerFocusSegments = new Image[DefaultFocusSegmentCount];

        for (int i = 0; i < playerFocusSegments.Length; i++)
        {
            if (playerFocusSegments[i] != null)
                continue;

            Transform existing = playerFocusSegmentsRoot.Find($"FocusSegment_{i + 1}");
            if (existing != null)
            {
                playerFocusSegments[i] = existing.GetComponent<Image>();
                continue;
            }

            GameObject segment = new GameObject($"FocusSegment_{i + 1}", typeof(RectTransform), typeof(Image));
            RectTransform rect = segment.GetComponent<RectTransform>();
            rect.SetParent(playerFocusSegmentsRoot, false);
            rect.sizeDelta = focusSegmentSize;
            playerFocusSegments[i] = segment.GetComponent<Image>();
        }
    }

    private void RefreshPlayerFocusSegments()
    {
        if (playerFocusSegments == null || player == null)
            return;

        int current = Mathf.Clamp(player.focus, 0, playerFocusSegments.Length);
        int max = Mathf.Clamp(player.maxFocus, 0, playerFocusSegments.Length);
        int previewFinalFocus = Mathf.Clamp(current - _previewFocusCost + _previewFocusGain, 0, max);
        float blinkAlpha = _previewFocusActive ? 0.55f + 0.45f * Mathf.PingPong(Time.unscaledTime * focusPreviewBlinkSpeed, 1f) : 1f;

        if (playerFocusText != null)
        {
            playerFocusText.text = _previewFocusActive ? previewFinalFocus.ToString() : current.ToString();
            playerFocusText.color = _previewFocusInvalid ? playerFocusInvalidColor : (_previewFocusActive ? playerFocusPreviewColor : _playerFocusTextOriginalColor);
        }

        int previewStart = current - _previewFocusCost;
        int gainStart = current;
        int gainEnd = current + _previewFocusGain - 1;
        Color focusGainColor = ResolveFocusGainColor();

        for (int i = 0; i < playerFocusSegments.Length; i++)
        {
            Image segment = playerFocusSegments[i];
            if (segment == null)
                continue;

            bool withinMax = i < max;
            bool filled = i < current;
            if (!withinMax)
            {
                segment.color = WithAlpha(playerFocusEmptyColor, 0.25f);
                continue;
            }

            bool consumed = _previewFocusActive && filled && i >= previewStart && i < current;
            bool gained = _previewFocusActive && !filled && i >= gainStart && i <= gainEnd;
            if (consumed)
                segment.color = WithAlpha(_previewFocusInvalid ? playerFocusInvalidColor : playerFocusPreviewColor, blinkAlpha);
            else if (gained)
                segment.color = WithAlpha(focusGainColor, blinkAlpha);
            else
                segment.color = filled ? playerFocusFilledColor : playerFocusEmptyColor;
        }
    }

    private void UpdateFocusPreviewBlink()
    {
        RefreshPlayerFocusSegments();
        if (_previewFocusInvalid && _focusBarBackgroundImage != null)
            _focusBarBackgroundImage.color = WithAlpha(playerFocusInvalidColor, 0.55f + 0.45f * Mathf.PingPong(Time.unscaledTime * focusPreviewBlinkSpeed, 1f));
    }

    private Color ResolveFocusGainColor()
    {
        DamagePopupSystem popups = FindObjectOfType<DamagePopupSystem>();
        return popups != null ? popups.FocusGainColor : new Color(0.22f, 0.74f, 1f, 1f);
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }
}

