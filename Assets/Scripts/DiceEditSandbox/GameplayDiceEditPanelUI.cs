using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
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

    private GameplayDiceEditController _controller;
    private Image _modalBlockerImage;

    private void OnEnable()
    {
        WireButtons();
        Refresh();
    }

    public void Initialize(GameplayDiceEditController controller)
    {
        _controller = controller;
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
        string title = isOpen ? _controller.GetDisplayName() : "No Zodiac";
        string body = isOpen ? _controller.GetEffectText() : "Choose a Zodiac consumable, then click a dice to edit it.";

        if (zodiacNameText != null)
            zodiacNameText.text = title;

        if (effectText != null)
            effectText.text = body;

        if (useButton != null)
            useButton.interactable = isOpen && _controller.CanUseCurrentConsumable();

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
               IsPointerOverButton(rollButton, screenPosition);
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
        _controller?.UseCurrentConsumable();
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
}
