using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Builds and refreshes the fallback runtime UI used when the scene does not provide
// an external Zodiac sandbox panel.
public partial class DiceEditSandboxController
{
    /// <summary>Creates the default in-scene sandbox UI for selecting dice faces and consumables.</summary>
    private void BuildRuntimeUi()
    {
        GameObject canvasGo = new GameObject("DiceEditSandboxCanvas");
        canvasGo.transform.SetParent(transform, false);

        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        RectTransform panel = CreatePanel("Panel", canvasGo.transform as RectTransform, new Vector2(16f, 16f), new Vector2(420f, 360f), TextAnchor.LowerLeft);
        Image panelImage = panel.gameObject.AddComponent<Image>();
        panelImage.color = new Color(0.08f, 0.1f, 0.14f, 0.86f);

        CreateText("Title", panel, "Dice Edit Sandbox", 28, new Vector2(18f, -18f), new Vector2(320f, 32f), FontStyles.Bold, TextAlignmentOptions.Left);
        CreateText("Hint", panel, "Drag ngang de xoay. Drag doc de flip/chinh chieu. Vuot nhanh se co quan tinh.", 18, new Vector2(18f, -54f), new Vector2(324f, 44f), FontStyles.Normal, TextAlignmentOptions.Left);
        _selectionLabel = CreateText("Selection", panel, "Selected: none", 20, new Vector2(18f, -108f), new Vector2(320f, 28f), FontStyles.Bold, TextAlignmentOptions.Left);
        _commitLabel = CreateText("Commit", panel, "Committed: none", 18, new Vector2(18f, -138f), new Vector2(320f, 28f), FontStyles.Normal, TextAlignmentOptions.Left);
        _consumableLabel = CreateText("Consumable", panel, "Consumable: none", 18, new Vector2(18f, -168f), new Vector2(384f, 44f), FontStyles.Normal, TextAlignmentOptions.Left);
        _resultLabel = CreateText("Result", panel, "Result: no consumable used yet.", 16, new Vector2(18f, -214f), new Vector2(384f, 40f), FontStyles.Normal, TextAlignmentOptions.Left);

        float slotY = -262f;
        for (int i = 0; i < ConsumableSlotCount; i++)
        {
            int capturedIndex = i;
            Button slotButton = CreateButton($"ConsumableSlot{i + 1}", panel, $"Slot {i + 1}", new Vector2(18f + (132f * i), slotY), new Vector2(120f, 40f), out TMP_Text slotLabel);
            slotButton.onClick.AddListener(() => SelectConsumableSlot(capturedIndex));
            _consumableButtons[i] = slotButton;
            _consumableButtonLabels[i] = slotLabel;
        }

        _useButton = CreateButton("UseButton", panel, "Use", new Vector2(18f, -314f), new Vector2(120f, 32f), out _);
        _useButton.onClick.AddListener(UseSelectedConsumable);

        _clearButton = CreateButton("ClearButton", panel, "Clear", new Vector2(152f, -314f), new Vector2(120f, 32f), out _);
        _clearButton.onClick.AddListener(ClearSelection);

        _flipButton = CreateButton("FlipButton", panel, "Flip", new Vector2(286f, -314f), new Vector2(56f, 32f), out _);
        _flipButton.onClick.AddListener(FlipFocusedDie);
    }

    /// <summary>Synchronizes runtime labels, buttons, and external listeners with current sandbox state.</summary>
    private void RefreshUi()
    {
        string selectionText = "Selected: none";
        if (IsSandboxDropdownMode())
        {
            if (_selectedDie != null && _selectedLogicalFaceIndices.Count > 0)
                selectionText = $"Selected: {_selectedDie.name} | Faces {BuildDisplayFaceList(_selectedDie, _selectedLogicalFaceIndices)}";
            else if (_selectedDie != null)
                selectionText = $"Selected: {_selectedDie.name}";
        }
        else if (_selectedDie != null && _selectedLogicalFaceIndex >= 0)
        {
            selectionText = $"Selected: {_selectedDie.name} face {_selectedLogicalFaceIndex}";
        }

        string commitText = "Committed: none";
        if (_committedDie != null && _committedLogicalFaceIndex >= 0)
            commitText = $"Committed: {_committedDie.name} face {_committedLogicalFaceIndex}";

        if (_selectionLabel != null)
            _selectionLabel.text = selectionText;

        if (_commitLabel != null)
            _commitLabel.text = commitText;

        if (_consumableLabel != null)
            _consumableLabel.text = BuildConsumableStatusText();

        if (_resultLabel != null)
            _resultLabel.text = _lastUseMessage;

        RefreshConsumableButtons();

        if (_useButton != null)
            _useButton.interactable = CanUseSelectedConsumable();

        if (_clearButton != null)
            _clearButton.interactable = _selectedDie != null || _committedLogicalFaceIndex >= 0 || _selectedLogicalFaceIndices.Count > 0;

        if (_flipButton != null)
            _flipButton.interactable = _focusedInteractable != null;

        UiStateChanged?.Invoke();
    }

    private static RectTransform CreatePanel(string name, RectTransform parent, Vector2 anchoredPosition, Vector2 size, TextAnchor anchor)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor == TextAnchor.LowerLeft ? Vector2.zero : new Vector2(0.5f, 0.5f);
        rt.anchorMax = rt.anchorMin;
        rt.pivot = rt.anchorMin;
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = size;
        return rt;
    }

    private static TMP_Text CreateText(string name, RectTransform parent, string text, float fontSize, Vector2 anchoredPosition, Vector2 size, FontStyles style, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = size;

        TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
        label.font = TMP_Settings.defaultFontAsset;
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.alignment = alignment;
        label.color = Color.white;
        return label;
    }

    private static Button CreateButton(string name, RectTransform parent, string label, Vector2 anchoredPosition, Vector2 size, out TMP_Text labelText)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = size;

        Image image = go.GetComponent<Image>();
        image.color = new Color(0.21f, 0.27f, 0.35f, 1f);

        Button button = go.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.28f, 0.35f, 0.45f, 1f);
        colors.pressedColor = new Color(0.16f, 0.22f, 0.3f, 1f);
        colors.disabledColor = new Color(0.16f, 0.16f, 0.16f, 0.7f);
        button.colors = colors;

        labelText = CreateText("Label", rt, label, 20, new Vector2(size.x * 0.5f, -size.y * 0.5f), size, FontStyles.Bold, TextAlignmentOptions.Center);
        labelText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        labelText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        labelText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        labelText.rectTransform.anchoredPosition = Vector2.zero;

        return button;
    }

    private void FlipFocusedDie()
    {
        if (_focusedInteractable == null)
            return;

        _focusedInteractable.FlipInspectOrientation();
    }
}
