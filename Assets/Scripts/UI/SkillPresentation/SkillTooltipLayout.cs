using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SkillTooltipLayout : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text targetingText;
    [SerializeField] private TMP_Text effectText;
    [SerializeField] private TMP_Text requiresHeaderText;
    [SerializeField] private TMP_Text requiresText;
    [SerializeField] private TMP_Text conditionHeaderText;
    [SerializeField] private TMP_Text conditionText;
    [SerializeField] private Image elementIconImage;
    [SerializeField] private VerticalLayoutGroup verticalLayout;
    [SerializeField] private ContentSizeFitter contentSizeFitter;
    [SerializeField] private float minContentWidth = 170f;
    [SerializeField] private float maxContentWidth = 320f;
    [SerializeField] private float minContentHeight = 0f;
    [SerializeField] private float maxContentHeight = 0f;

    public RectTransform RectTransform => transform as RectTransform;
    public Image Background => background;
    public TMP_Text TitleText => titleText;
    public TMP_Text CostText => costText;
    public TMP_Text TargetingText => targetingText;
    public TMP_Text EffectText => effectText;
    public TMP_Text RequiresHeaderText => requiresHeaderText;
    public TMP_Text RequiresText => requiresText;
    public TMP_Text ConditionHeaderText => conditionHeaderText;
    public TMP_Text ConditionText => conditionText;
    public Image ElementIconImage => elementIconImage;
    public VerticalLayoutGroup VerticalLayout => verticalLayout;
    public ContentSizeFitter ContentSizeFitter => contentSizeFitter;
    public float MinContentWidth => minContentWidth > 0f ? minContentWidth : 170f;
    public float MaxContentWidth => maxContentWidth > 0f ? maxContentWidth : 320f;
    public float MinContentHeight => Mathf.Max(0f, minContentHeight);
    public float MaxContentHeight => Mathf.Max(0f, maxContentHeight);
}
