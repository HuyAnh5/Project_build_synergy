using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class SkillTooltipKeywordTooltipTemplate : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Image iconImage;

    public RectTransform RectTransform => transform as RectTransform;
    public Image Background => background;
    public TMP_Text TitleText => titleText;
    public TMP_Text BodyText => bodyText;
    public Image IconImage => iconImage;
}
