using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SkillTooltipLayout : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private VerticalLayoutGroup verticalLayout;
    [SerializeField] private ContentSizeFitter contentSizeFitter;
    [SerializeField] private float minContentWidth = 170f;
    [SerializeField] private float maxContentWidth = 320f;

    public RectTransform RectTransform => transform as RectTransform;
    public Image Background => background;
    public TMP_Text TitleText => titleText;
    public TMP_Text BodyText => bodyText;
    public VerticalLayoutGroup VerticalLayout => verticalLayout;
    public ContentSizeFitter ContentSizeFitter => contentSizeFitter;
    public float MinContentWidth => minContentWidth > 0f ? minContentWidth : 170f;
    public float MaxContentWidth => maxContentWidth > 0f ? maxContentWidth : 320f;
}
