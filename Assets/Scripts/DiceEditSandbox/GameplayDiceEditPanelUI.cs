using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class GameplayDiceEditPanelUI : MonoBehaviour
{
    [Header("Bindings")]
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private RectTransform modalBlockerRoot;
    [SerializeField] private TMP_Text zodiacNameText;
    [SerializeField] private TMP_Text effectText;
    [SerializeField] private Button useButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button autoUprightButton;
    [SerializeField] private Button rollButton;
    [SerializeField] private TMP_Text useButtonText;
    [SerializeField] private RectTransform inspectPaletteRoot;
    [SerializeField] private Button[] inspectColorButtons = new Button[4];

    private static readonly Color[] InspectPaletteColors =
    {
        new Color(1f, 0.43f, 0.43f, 1f),
        new Color(0.35f, 0.72f, 1f, 1f),
        new Color(0.74f, 0.52f, 1f, 1f),
        new Color(1f, 0.78f, 0.26f, 1f),
    };

    private GameplayDiceEditController _controller;
    private Image _modalBlockerImage;
    private readonly List<Image> _inspectColorButtonImages = new List<Image>();

    private void OnEnable()
    {
        WireButtons();
        Refresh();
    }

    public void Initialize(GameplayDiceEditController controller)
    {
        _controller = controller;
        EnsureInspectPalette();
        WireButtons();
        Refresh();
    }

    public void SetVisible(bool visible)
    {
        EnsureModalBlocker();
        if (modalBlockerRoot != null)
            modalBlockerRoot.gameObject.SetActive(visible);
        if (panelRoot != null)
        {
            panelRoot.gameObject.SetActive(visible);
            panelRoot.SetAsLastSibling();
        }
        else
            gameObject.SetActive(visible);
    }

    public void Refresh()
    {
        bool isOpen = _controller != null && _controller.IsPanelOpen;
        bool inspectOnly = isOpen && _controller != null && _controller.IsInspectOnlyMode;
        string title = isOpen ? _controller.GetDisplayName() : "No Zodiac";
        string body = isOpen ? _controller.GetEffectText() : "Choose a Zodiac consumable, then click a dice to edit it.";

        if (zodiacNameText != null)
            zodiacNameText.text = title;

        if (effectText != null)
            effectText.text = body;

        if (useButton != null)
        {
            useButton.gameObject.SetActive(isOpen);
            useButton.interactable = inspectOnly
                ? _controller.CanClearInspectHighlights()
                : isOpen && _controller.CanUseCurrentConsumable();
        }

        if (useButtonText != null)
            useButtonText.text = inspectOnly ? "UNHIGHLIGHT ALL" : "USE";

        EnsureInspectPalette();
        if (inspectPaletteRoot != null)
            inspectPaletteRoot.gameObject.SetActive(inspectOnly);

        RefreshInspectPalette(inspectOnly);

        if (cancelButton != null)
            cancelButton.interactable = isOpen;

        if (autoUprightButton != null)
            autoUprightButton.interactable = isOpen && _controller.CanAutoUprightFocusedDie();

        if (rollButton != null)
            rollButton.interactable = isOpen && _controller.CanRollFocusedDie();
    }

    public bool IsPointerOverInteractiveUi(Vector2 screenPosition)
    {
        return IsPointerOverButton(useButton, screenPosition) ||
               IsPointerOverButton(cancelButton, screenPosition) ||
               IsPointerOverButton(autoUprightButton, screenPosition) ||
               IsPointerOverButton(rollButton, screenPosition) ||
               IsPointerOverAnyInspectColorButton(screenPosition);
    }

    public bool IsPointerOverPanel(Vector2 screenPosition)
    {
        return RectangleContainsScreenPoint(panelRoot, screenPosition);
    }

    public bool IsPointerBlockedByModal(Vector2 screenPosition)
    {
        if (RectangleContainsScreenPoint(panelRoot, screenPosition))
            return true;

        return RectangleContainsScreenPoint(modalBlockerRoot, screenPosition);
    }

    private void WireButtons()
    {
        EnsureInspectPalette();

        if (useButton != null)
        {
            useButton.onClick.RemoveListener(HandleUseClicked);
            useButton.onClick.AddListener(HandleUseClicked);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveListener(HandleCancelClicked);
            cancelButton.onClick.AddListener(HandleCancelClicked);
        }

        if (autoUprightButton != null)
        {
            autoUprightButton.onClick.RemoveListener(HandleAutoUprightClicked);
            autoUprightButton.onClick.AddListener(HandleAutoUprightClicked);
        }

        if (rollButton != null)
        {
            rollButton.onClick.RemoveListener(HandleRollClicked);
            rollButton.onClick.AddListener(HandleRollClicked);
        }

        for (int i = 0; i < inspectColorButtons.Length; i++)
        {
            Button button = inspectColorButtons[i];
            if (button == null)
                continue;

            int capturedIndex = i;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => HandleInspectColorClicked(capturedIndex));
        }
    }

    private void EnsureInspectPalette()
    {
        if (inspectPaletteRoot == null)
        {
            RectTransform anchor = useButton != null ? useButton.transform.parent as RectTransform : panelRoot;
            if (anchor != null)
            {
                Transform existing = anchor.Find("InspectPalette");
                inspectPaletteRoot = existing as RectTransform;
                if (inspectPaletteRoot == null)
                {
                    GameObject paletteGo = new GameObject("InspectPalette", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                    inspectPaletteRoot = paletteGo.GetComponent<RectTransform>();
                    inspectPaletteRoot.SetParent(anchor, false);
                    inspectPaletteRoot.anchorMin = new Vector2(0.5f, 0f);
                    inspectPaletteRoot.anchorMax = new Vector2(0.5f, 0f);
                    inspectPaletteRoot.pivot = new Vector2(0.5f, 0f);
                    inspectPaletteRoot.anchoredPosition = new Vector2(0f, 52f);
                    inspectPaletteRoot.sizeDelta = new Vector2(200f, 28f);

                    HorizontalLayoutGroup layout = inspectPaletteRoot.GetComponent<HorizontalLayoutGroup>();
                    layout.spacing = 8f;
                    layout.childAlignment = TextAnchor.MiddleCenter;
                    layout.childControlHeight = false;
                    layout.childControlWidth = false;
                    layout.childForceExpandHeight = false;
                    layout.childForceExpandWidth = false;
                }
            }
        }

        if (inspectPaletteRoot == null)
            return;

        if (inspectColorButtons == null || inspectColorButtons.Length != 4)
            inspectColorButtons = new Button[4];

        _inspectColorButtonImages.Clear();
        for (int i = 0; i < inspectColorButtons.Length; i++)
        {
            if (inspectColorButtons[i] == null)
                inspectColorButtons[i] = FindOrCreateInspectColorButton(i);

            if (inspectColorButtons[i] != null)
            {
                Image image = inspectColorButtons[i].GetComponent<Image>();
                if (image != null)
                    _inspectColorButtonImages.Add(image);
            }
        }
    }

    private Button FindOrCreateInspectColorButton(int index)
    {
        if (inspectPaletteRoot == null)
            return null;

        string buttonName = $"InspectColorButton_{index}";
        Transform existing = inspectPaletteRoot.Find(buttonName);
        if (existing != null)
            return existing.GetComponent<Button>();

        GameObject buttonGo = new GameObject(buttonName, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        RectTransform rect = buttonGo.GetComponent<RectTransform>();
        rect.SetParent(inspectPaletteRoot, false);
        rect.sizeDelta = new Vector2(34f, 24f);

        LayoutElement layout = buttonGo.GetComponent<LayoutElement>();
        layout.preferredWidth = 34f;
        layout.preferredHeight = 24f;

        Image image = buttonGo.GetComponent<Image>();
        image.color = InspectPaletteColors[Mathf.Clamp(index, 0, InspectPaletteColors.Length - 1)];

        return buttonGo.GetComponent<Button>();
    }

    private void RefreshInspectPalette(bool inspectOnly)
    {
        if (!inspectOnly || _controller == null)
            return;

        int activeIndex = _controller.GetInspectActiveMarkColorIndex();
        for (int i = 0; i < inspectColorButtons.Length; i++)
        {
            Button button = inspectColorButtons[i];
            if (button == null)
                continue;

            button.interactable = true;
            Image image = button.GetComponent<Image>();
            if (image == null)
                continue;

            image.color = InspectPaletteColors[i];
            image.transform.localScale = i == activeIndex ? new Vector3(1.15f, 1.15f, 1f) : Vector3.one;
        }
    }

    private void EnsureModalBlocker()
    {
        if (modalBlockerRoot == null)
        {
            RectTransform host = panelRoot != null && panelRoot.parent is RectTransform parentRect
                ? parentRect
                : transform as RectTransform;
            if (host != null)
            {
                Transform existing = host.Find("GameplayDiceEditModalBlocker");
                if (existing is RectTransform existingRect)
                {
                    modalBlockerRoot = existingRect;
                }
                else
                {
                    GameObject blockerGo = new GameObject("GameplayDiceEditModalBlocker", typeof(RectTransform), typeof(Image));
                    blockerGo.transform.SetParent(host, false);
                    modalBlockerRoot = blockerGo.GetComponent<RectTransform>();
                    modalBlockerRoot.anchorMin = Vector2.zero;
                    modalBlockerRoot.anchorMax = Vector2.one;
                    modalBlockerRoot.offsetMin = Vector2.zero;
                    modalBlockerRoot.offsetMax = Vector2.zero;
                    modalBlockerRoot.SetAsFirstSibling();
                }
            }
        }

        if (modalBlockerRoot == null)
            return;

        if (_modalBlockerImage == null)
            _modalBlockerImage = modalBlockerRoot.GetComponent<Image>() ?? modalBlockerRoot.gameObject.AddComponent<Image>();

        _modalBlockerImage.color = new Color(0f, 0f, 0f, 0.001f);
        _modalBlockerImage.raycastTarget = true;
    }

    private static bool RectangleContainsScreenPoint(RectTransform rectTransform, Vector2 screenPosition)
    {
        if (rectTransform == null || !rectTransform.gameObject.activeInHierarchy)
            return false;

        Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
        Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPosition, cam);
    }

    private void HandleUseClicked()
    {
        if (_controller == null)
            return;

        if (_controller.IsInspectOnlyMode)
            _controller.ClearInspectHighlights();
        else
            _controller.UseCurrentConsumable();
    }

    private void HandleCancelClicked()
    {
        _controller?.CancelAndClose();
    }

    private void HandleAutoUprightClicked()
    {
        _controller?.AutoUprightFocusedDie();
    }

    private void HandleRollClicked()
    {
        _controller?.RollFocusedDie();
    }

    private void HandleInspectColorClicked(int colorIndex)
    {
        _controller?.SetInspectActiveMarkColorIndex(colorIndex);
    }

    private static bool IsPointerOverButton(Button button, Vector2 screenPosition)
    {
        if (button == null || !button.gameObject.activeInHierarchy)
            return false;

        RectTransform rt = button.transform as RectTransform;
        if (rt == null)
            return false;

        Canvas canvas = button.GetComponentInParent<Canvas>();
        Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        return RectTransformUtility.RectangleContainsScreenPoint(rt, screenPosition, cam);
    }

    private bool IsPointerOverAnyInspectColorButton(Vector2 screenPosition)
    {
        if (inspectColorButtons == null)
            return false;

        for (int i = 0; i < inspectColorButtons.Length; i++)
        {
            if (IsPointerOverButton(inspectColorButtons[i], screenPosition))
                return true;
        }

        return false;
    }
}
