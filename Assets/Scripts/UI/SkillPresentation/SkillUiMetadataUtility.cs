using UnityEngine;

public static class SkillUiMetadataUtility
{
    public static string ResolveDisplayName(ScriptableObject asset)
    {
        switch (asset)
        {
            case SkillDamageSO damage:
                if (damage.coreAction == CoreAction.BasicStrike)
                    return "Basic Attack";
                if (damage.coreAction == CoreAction.BasicGuard)
                    return "Basic Guard";
                return string.IsNullOrWhiteSpace(damage.displayName) ? damage.name : damage.displayName;

            case SkillBuffDebuffSO buffDebuff:
                return string.IsNullOrWhiteSpace(buffDebuff.displayName) ? buffDebuff.name : buffDebuff.displayName;

            case SkillPassiveSO passive:
                return string.IsNullOrWhiteSpace(passive.displayName) ? passive.name : passive.displayName;

            default:
                return asset != null ? asset.name : string.Empty;
        }
    }

    public static bool TryGetSkillCosts(ScriptableObject asset, out int focusCost, out int slotsRequired)
    {
        focusCost = 0;
        slotsRequired = 0;

        switch (asset)
        {
            case SkillDamageSO damage:
                focusCost = Mathf.Max(0, damage.focusCost);
                slotsRequired = Mathf.Clamp(damage.slotsRequired, 1, 3);
                return true;

            case SkillBuffDebuffSO buffDebuff:
                focusCost = Mathf.Max(0, buffDebuff.focusCost);
                slotsRequired = Mathf.Clamp(buffDebuff.slotsRequired, 1, 3);
                return true;

            default:
                return false;
        }
    }

    public static bool TryGetElementType(ScriptableObject asset, out ElementType element)
    {
        element = ElementType.Neutral;

        switch (asset)
        {
            case SkillDamageSO damage:
                element = ToElementType(damage.element);
                return true;

            case SkillBuffDebuffSO buffDebuff:
                return TryInferBuffElement(buffDebuff, out element);

            default:
                return false;
        }
    }

    public static bool TryGetTargetRule(ScriptableObject asset, out SkillTargetRule targetRule)
    {
        targetRule = SkillTargetRule.SingleEnemy;

        switch (asset)
        {
            case SkillDamageSO damage:
                targetRule = damage.target;
                return true;

            case SkillBuffDebuffSO buffDebuff:
                targetRule = buffDebuff.target;
                return true;

            default:
                return false;
        }
    }

    public static bool TryGetRangeType(ScriptableObject asset, SkillRuntime runtime, out RangeType range)
    {
        range = RangeType.Ranged;

        if (runtime != null)
        {
            range = runtime.range;
            return true;
        }

        switch (asset)
        {
            case SkillDamageSO damage:
                range = damage.range;
                return true;

            case SkillBuffDebuffSO:
                range = RangeType.Ranged;
                return true;

            default:
                return false;
        }
    }

    public static string BuildTargetingSummary(ScriptableObject asset, SkillRuntime runtime = null)
    {
        if (!TryGetTargetRule(asset, out SkillTargetRule targetRule))
            return string.Empty;

        if (targetRule == SkillTargetRule.Self)
            return "Self";

        string targetText = FormatTargetType(targetRule);
        if (!TryGetRangeType(asset, runtime, out RangeType range))
            return targetText;

        return FormatRange(range) + " • " + targetText;
    }

    public static string FormatRange(RangeType range)
    {
        switch (range)
        {
            case RangeType.Melee:
                return "Melee";
            case RangeType.Ranged:
            default:
                return "Ranged";
        }
    }

    public static string FormatTargetType(SkillTargetRule targetRule)
    {
        switch (targetRule)
        {
            case SkillTargetRule.SingleEnemy:
                return "Single Enemy";
            case SkillTargetRule.SingleAlly:
                return "Single Ally";
            case SkillTargetRule.RowEnemies:
                return "Enemy Row";
            case SkillTargetRule.RowAllies:
                return "Ally Row";
            case SkillTargetRule.AllEnemies:
                return "All Enemies";
            case SkillTargetRule.AllAllies:
                return "All Allies";
            case SkillTargetRule.Self:
            default:
                return "Self";
        }
    }

    public static string GetDescription(ScriptableObject asset)
    {
        switch (asset)
        {
            case SkillDamageSO damage:
                return damage.GetAuthoringDescription();
            case SkillBuffDebuffSO buffDebuff:
                return buffDebuff.description ?? string.Empty;
            case SkillPassiveSO passive:
                return passive.description ?? string.Empty;
            default:
                return string.Empty;
        }
    }

    private static ElementType ToElementType(ElementTag element)
    {
        switch (element)
        {
            case ElementTag.Fire:
                return ElementType.Fire;
            case ElementTag.Ice:
                return ElementType.Ice;
            case ElementTag.Lightning:
                return ElementType.Lightning;
            case ElementTag.Physical:
                return ElementType.Physical;
            case ElementTag.Neutral:
            default:
                return ElementType.Neutral;
        }
    }

    private static bool TryInferBuffElement(SkillBuffDebuffSO skill, out ElementType element)
    {
        element = ElementType.Neutral;
        if (skill == null)
            return false;

        switch (skill.behaviorId)
        {
            case BuffBehaviorId.Fire_EmberWeapon:
            case BuffBehaviorId.Fire_Cinderbrand:
                element = ElementType.Fire;
                return true;

            case BuffBehaviorId.Bleed_Siphon:
                element = ElementType.Physical;
                return true;
        }

        string key = ((skill.displayName ?? string.Empty) + " " + (skill.name ?? string.Empty)).ToLowerInvariant();
        if (key.Contains("fire") || key.Contains("ember") || key.Contains("cinder"))
        {
            element = ElementType.Fire;
            return true;
        }

        if (key.Contains("ice") || key.Contains("frost") || key.Contains("permafrost"))
        {
            element = ElementType.Ice;
            return true;
        }

        if (key.Contains("lightning") || key.Contains("spark") || key.Contains("thunder") || key.Contains("flash"))
        {
            element = ElementType.Lightning;
            return true;
        }

        if (key.Contains("bleed") || key.Contains("blood") || key.Contains("siphon"))
        {
            element = ElementType.Physical;
            return true;
        }

        if (key.Contains("physical") || key.Contains("sunder") || key.Contains("strike"))
        {
            element = ElementType.Physical;
            return true;
        }

        return false;
    }
}
