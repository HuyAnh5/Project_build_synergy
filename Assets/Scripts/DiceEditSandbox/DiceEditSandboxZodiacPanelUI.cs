using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class DiceEditSandboxZodiacPanelUI : MonoBehaviour
{
    [SerializeField] private Object zodiacSourceFolder;
    [SerializeField] private List<ConsumableDataSO> zodiacOptions = new List<ConsumableDataSO>();
    [SerializeField] private ConsumableDataSO selectedZodiac;
    [SerializeField] private TMP_Text zodiacNameText;
    [SerializeField] private TMP_Text targetStatusText;
    [SerializeField] private TMP_Text selectionRuleText;
    [SerializeField] private Button useButton;
    [SerializeField] private TMP_Text useButtonText;
    [SerializeField] private Image useButtonBackground;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TMP_Text cancelButtonText;
    [SerializeField] private Image cancelButtonBackground;
    [SerializeField] private Button autoUprightButton;
    [SerializeField] private Button rollButton;
    [SerializeField] private Color useEnabledColor = new Color(0.83f, 0.83f, 0.83f, 1f);
    [SerializeField] private Color useDisabledColor = new Color(0.42f, 0.42f, 0.42f, 1f);

    private DiceEditSandboxController _controller;

    private void OnEnable()
    {
        RebindIfNeeded();
        WireButtons();
        PushInspectorZodiacToController();
        Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
            return;

        PushInspectorZodiacToController();
        Refresh();
    }

    public void Initialize(DiceEditSandboxController controller)
    {
        if (_controller == controller)
        {
            _controller?.RegisterExternalZodiacPanel(this);
            PushInspectorZodiacToController();
            Refresh();
            return;
        }

        Unsubscribe();
        _controller = controller;
        _controller?.RegisterExternalZodiacPanel(this);
        Subscribe();
        WireButtons();
        PushInspectorZodiacToController();
        Refresh();
    }

    public void Refresh()
    {
        ConsumableDataSO data = ResolveCurrentZodiac();
        bool hasZodiac = data != null;
        bool canUse = hasZodiac && _controller != null && _controller.CanUseSelectedConsumableFromUi();
        bool canAutoUpright = _controller != null && _controller.CanAutoUprightFocusedDie();
        bool canRoll = _controller != null && _controller.CanRollFocusedDie();
        int limit = _controller != null ? _controller.GetSandboxFaceSelectionLimit() : 0;
        int selectedCount = _controller != null ? _controller.GetSelectedFaceCount() : 0;

        if (zodiacNameText != null)
            zodiacNameText.text = hasZodiac ? data.displayName : "No Zodiac";

        if (targetStatusText != null)
            targetStatusText.text = hasZodiac && _controller != null
                ? _controller.BuildResolvedTargetLabel()
                : "Target: no zodiac selected";

        if (selectionRuleText != null)
            selectionRuleText.text = BuildSelectionRuleText(data, limit, selectedCount);

        if (useButtonText != null)
            useButtonText.text = "USE";

        if (cancelButtonText != null)
            cancelButtonText.text = "CANCEL";

        if (useButton != null)
            useButton.interactable = canUse;

        if (cancelButton != null)
            cancelButton.interactable = hasZodiac;

        if (autoUprightButton != null)
            autoUprightButton.interactable = canAutoUpright;

        if (rollButton != null)
            rollButton.interactable = canRoll;

        if (useButtonBackground != null)
            useButtonBackground.color = canUse ? useEnabledColor : useDisabledColor;

        if (cancelButtonBackground != null)
            cancelButtonBackground.color = hasZodiac
                ? new Color(0.92f, 0.92f, 0.92f, 1f)
                : new Color(0.42f, 0.42f, 0.42f, 1f);
    }

    private void RebindIfNeeded()
    {
        if (_controller != null)
            return;

        DiceEditSandboxController controller = FindFirstObjectByType<DiceEditSandboxController>(FindObjectsInactive.Include);
        if (controller != null)
            Initialize(controller);
    }

    private void Subscribe()
    {
        if (_controller != null)
            _controller.UiStateChanged += Refresh;
    }

    private void Unsubscribe()
    {
        if (_controller != null)
            _controller.UiStateChanged -= Refresh;
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

    private void PushInspectorZodiacToController()
    {
        if (_controller == null)
            return;

        _controller.SetSandboxSelectedConsumable(selectedZodiac);
    }

    private ConsumableDataSO ResolveCurrentZodiac()
    {
        if (_controller != null)
        {
            ConsumableDataSO selected = _controller.GetSelectedConsumableData();
            if (selected != null && selected.family == ConsumableFamily.Zodiac)
                return selected;
        }

        return selectedZodiac;
    }

    private void HandleUseClicked()
    {
        _controller?.TryUseSelectedConsumableFromUi();
    }

    private void HandleCancelClicked()
    {
        _controller?.ClearSelection();
    }

    private void HandleAutoUprightClicked()
    {
        _controller?.AutoUprightFocusedDie();
    }

    private void HandleRollClicked()
    {
        _controller?.RollFocusedDie();
    }

    private static string BuildSelectionRuleText(ConsumableDataSO data, int limit, int selectedCount)
    {
        if (data == null)
            return "Assign one Zodiac asset on this panel in the inspector.";

        if (data.effectId != ConsumableEffectId.AdjustBaseValue &&
            data.effectId != ConsumableEffectId.ApplyFaceEnchant &&
            data.effectId != ConsumableEffectId.CopyPasteFace)
            return "This Zodiac is assigned, but its sandbox effect is not wired yet.";

        if (data.effectId == ConsumableEffectId.CopyPasteFace)
            return "Pick 1 source face first (yellow), then 1 target face (green).";

        if (data.targetKind == ConsumableTargetKind.DiceFace)
            return $"Select up to {Mathf.Max(1, limit)} face(s). Current: {selectedCount}.";

        if (data.targetKind == ConsumableTargetKind.Dice)
            return "Select one dice.";

        return data.targetKind == ConsumableTargetKind.None || data.targetKind == ConsumableTargetKind.Self
            ? "No dice-face selection required."
            : $"Target kind: {data.targetKind}.";
    }
}
