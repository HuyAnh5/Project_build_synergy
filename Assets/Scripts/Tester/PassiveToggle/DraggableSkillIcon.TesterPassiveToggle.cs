using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class DraggableSkillIcon
{
    private const string TesterPassiveBadgeName = "TesterPassiveToggleBadge";

    private static readonly Color TesterPassiveOnBadgeColor = new Color(0.12f, 0.58f, 0.24f, 0.92f);
    private static readonly Color TesterPassiveOffBadgeColor = new Color(0.58f, 0.14f, 0.14f, 0.92f);
    private static readonly Color TesterPassiveOffIconTint = new Color(0.72f, 0.72f, 0.72f, 1f);
    private static readonly Color TesterPassiveOffBackgroundTint = new Color(0.9f, 0.9f, 0.9f, 1f);
    private static readonly Color TesterPassiveOffLabelTint = new Color(0.82f, 0.82f, 0.82f, 1f);
    private const float TesterPassiveOffIconAlphaMultiplier = 0.62f;
    private const float TesterPassiveOffBackgroundAlphaMultiplier = 0.82f;

    private Image _testerPassiveBadgeBackground;
    private TMP_Text _testerPassiveBadgeText;
    private Color _defaultIconColor = Color.white;
    private Color _defaultNameTextColor = Color.white;
    private Color _defaultBackgroundColor = Color.white;

    partial void HandleTesterPassivePointerClick(SkillPassiveSO passive)
    {
        if (!TesterPassiveToggleState.IsControlledPassive(passive))
            return;

        TesterPassiveToggleState.Toggle(passive);
        RefreshPassiveToggleIcons(passive);
    }

    partial void RefreshTesterPassiveVisualState()
    {
        CacheTesterPassiveDefaultVisuals();

        SkillPassiveSO passive = GetSkillAsset() as SkillPassiveSO;
        bool isTesterPassive = TesterPassiveToggleState.IsControlledPassive(passive);
        EnsureTesterPassiveBadgeUi();

        if (_testerPassiveBadgeBackground != null)
            _testerPassiveBadgeBackground.gameObject.SetActive(isTesterPassive);
        if (_testerPassiveBadgeText != null)
            _testerPassiveBadgeText.gameObject.SetActive(isTesterPassive);

        if (!isTesterPassive)
        {
            RestoreTesterPassiveDefaultVisuals();
            return;
        }

        bool isEnabled = TesterPassiveToggleState.IsEnabled(passive);
        if (_testerPassiveBadgeBackground != null)
            _testerPassiveBadgeBackground.color = isEnabled ? TesterPassiveOnBadgeColor : TesterPassiveOffBadgeColor;
        if (_testerPassiveBadgeText != null)
            _testerPassiveBadgeText.text = isEnabled ? "ON" : "OFF";

        if (nameText != null)
            nameText.color = isEnabled ? _defaultNameTextColor : TesterPassiveOffLabelTint;
        if (skillBackgroundImage != null)
        {
            if (isEnabled)
            {
                skillBackgroundImage.color = _defaultBackgroundColor;
            }
            else
            {
                Color backgroundColor = Color.Lerp(_defaultBackgroundColor, TesterPassiveOffBackgroundTint, 0.55f);
                backgroundColor.a *= TesterPassiveOffBackgroundAlphaMultiplier;
                skillBackgroundImage.color = backgroundColor;
            }
        }
    }

    partial void ApplyTesterPassiveVisualState()
    {
        SkillPassiveSO passive = GetSkillAsset() as SkillPassiveSO;
        if (!TesterPassiveToggleState.IsControlledPassive(passive))
            return;

        bool isEnabled = TesterPassiveToggleState.IsEnabled(passive);
        if (_img != null)
        {
            Color iconColor = _defaultIconColor;
            iconColor.a = _img.color.a;
            if (!isEnabled)
            {
                iconColor = Color.Lerp(iconColor, TesterPassiveOffIconTint, 0.7f);
                iconColor.a *= TesterPassiveOffIconAlphaMultiplier;
            }

            _img.color = iconColor;
        }
    }

    private void CacheTesterPassiveDefaultVisuals()
    {
        SkillPassiveSO passive = GetSkillAsset() as SkillPassiveSO;
        bool canRefreshDefaults = !TesterPassiveToggleState.IsControlledPassive(passive) || TesterPassiveToggleState.IsEnabled(passive);

        if (_img != null && canRefreshDefaults)
            _defaultIconColor = _img.color;
        if (nameText != null && canRefreshDefaults)
            _defaultNameTextColor = nameText.color;
        if (skillBackgroundImage != null && canRefreshDefaults)
            _defaultBackgroundColor = skillBackgroundImage.color;
    }

    private void RestoreTesterPassiveDefaultVisuals()
    {
        if (_img != null)
            _img.color = _defaultIconColor;
        if (nameText != null)
            nameText.color = _defaultNameTextColor;
        if (skillBackgroundImage != null)
            skillBackgroundImage.color = _defaultBackgroundColor;
    }

    private void EnsureTesterPassiveBadgeUi()
    {
        _testerPassiveBadgeBackground = EnsureBadge(
            ref _testerPassiveBadgeBackground,
            ref _testerPassiveBadgeText,
            TesterPassiveBadgeName,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 6f),
            TesterPassiveOffBadgeColor);

        if (_testerPassiveBadgeBackground != null)
            _testerPassiveBadgeBackground.raycastTarget = false;
        if (_testerPassiveBadgeText != null)
        {
            _testerPassiveBadgeText.fontSize = 13f;
            _testerPassiveBadgeText.fontStyle = FontStyles.Bold;
            _testerPassiveBadgeText.raycastTarget = false;
        }
    }

    private static void RefreshPassiveToggleIcons(SkillPassiveSO passive)
    {
        if (passive == null)
            return;

        DraggableSkillIcon[] icons = DraggableSkillIconRegistry.GetAllSnapshot();
        for (int i = 0; i < icons.Length; i++)
        {
            DraggableSkillIcon icon = icons[i];
            if (icon == null)
                continue;
            if (icon.GetSkillAsset() != passive)
                continue;

            icon.Refresh();
        }
    }
}
