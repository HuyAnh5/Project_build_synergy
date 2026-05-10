using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SkillSlotLayout : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image skillArt;
    [SerializeField] private TMP_Text titleText;

    [Header("Badges")]
    [SerializeField] private Image focusBadgeBackground;
    [SerializeField] private TMP_Text focusBadgeText;
    [SerializeField] private Image diceBadgeBackground;
    [SerializeField] private Image diceBadgeIcon;
    [SerializeField] private TMP_Text diceBadgeFallbackText;
    [SerializeField] private Image elementBadgeBackground;
    [SerializeField] private Image elementBadgeIcon;

    public Image BackgroundImage => backgroundImage;
    public Image SkillArt => skillArt;
    public TMP_Text TitleText => titleText;
    public Image FocusBadgeBackground => focusBadgeBackground;
    public TMP_Text FocusBadgeText => focusBadgeText;
    public Image DiceBadgeBackground => diceBadgeBackground;
    public Image DiceBadgeIcon => diceBadgeIcon;
    public TMP_Text DiceBadgeFallbackText => diceBadgeFallbackText;
    public Image ElementBadgeBackground => elementBadgeBackground;
    public Image ElementBadgeIcon => elementBadgeIcon;

    public void ApplyTo(DraggableSkillIcon icon)
    {
        if (icon == null)
            return;

        icon.BindLayout(this);
    }
}
