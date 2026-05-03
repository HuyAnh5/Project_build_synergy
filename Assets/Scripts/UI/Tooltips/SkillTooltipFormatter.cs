using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class SkillTooltipFormatter
{
    private const string AddedValueColor = "#5CCBFF";
    private const string LabelColor = "#AAB6C8";
    private const string MutedColor = "#8F9AAD";

    public static string GetTitle(ScriptableObject asset)
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

    public static string BuildBody(ScriptableObject asset, SkillRuntime runtime = null)
    {
        if (asset == null)
            return string.Empty;

        switch (asset)
        {
            case SkillDamageSO damage:
                return BuildDamageBody(damage, runtime);
            case SkillBuffDebuffSO buffDebuff:
                return BuildBuffDebuffBody(buffDebuff, runtime);
            case SkillPassiveSO passive:
                return BuildPassiveBody(passive);
            default:
                return string.Empty;
        }
    }

    private static string BuildDamageBody(SkillDamageSO skill, SkillRuntime runtime)
    {
        SkillRuntime view = runtime ?? SkillRuntime.FromDamage(skill);
        StringBuilder sb = new StringBuilder(320);
        AppendMeta(
            sb,
            Mathf.Max(0, view.focusCost),
            Mathf.Clamp(view.slotsRequired, 1, 3),
            view.targetRuleV2,
            FormatElement(view.element),
            BuildDamageTags(view));

        List<string> rules = new List<string>();
        if (!string.IsNullOrWhiteSpace(skill.description))
            rules.Add(ColorQuotedAddedValues(skill.description.Trim()));

        AppendDamageRules(rules, view);

        AppendRules(sb, rules);
        AppendAddedValueHint(sb, rules);
        return sb.ToString();
    }

    private static string BuildBuffDebuffBody(SkillBuffDebuffSO skill, SkillRuntime runtime)
    {
        StringBuilder sb = new StringBuilder(320);
        SkillRuntime view = runtime ?? BuildBuffRuntime(skill);
        AppendMeta(
            sb,
            Mathf.Max(0, view != null ? view.focusCost : skill.focusCost),
            Mathf.Clamp(view != null ? view.slotsRequired : skill.slotsRequired, 1, 3),
            view != null ? view.targetRuleV2 : skill.target,
            InferBuffElement(skill),
            BuildBuffTags(skill));

        List<string> rules = new List<string>();
        AppendBuffFireModuleRules(rules, skill);
        AppendBuffEffectRules(rules, skill, view);
        AppendAilmentRules(rules, skill);

        if (!string.IsNullOrWhiteSpace(skill.description))
            rules.Insert(0, ColorQuotedAddedValues(skill.description.Trim()));

        AppendRules(sb, rules);
        AppendAddedValueHint(sb, rules);
        return sb.ToString();
    }

    private static string BuildPassiveBody(SkillPassiveSO passive)
    {
        StringBuilder sb = new StringBuilder(220);
        sb.Append(ColorLabel("Type")).Append(": Passive");
        if (passive.spec != null && passive.spec.rarity != ContentRarity.Pending)
            sb.Append("   ").Append(ColorLabel("Rarity")).Append(": ").Append(passive.spec.rarity);

        if (!string.IsNullOrWhiteSpace(passive.description))
            sb.Append("\n\n").Append(ColorQuotedAddedValues(passive.description.Trim()));
        return sb.ToString();
    }

    private static void AppendMeta(StringBuilder sb, int focusCost, int slotsRequired, SkillTargetRule target, string element, string tags)
    {
        sb.Append(ColorLabel("Focus")).Append(": ").Append(focusCost);
        sb.Append("   ").Append(ColorLabel("Dice")).Append(": ").Append(slotsRequired);
        sb.Append('\n').Append(ColorLabel("Target")).Append(": ").Append(FormatTarget(target));
        if (!string.IsNullOrWhiteSpace(element))
            sb.Append('\n').Append(ColorLabel("Element")).Append(": ").Append(element);
        if (!string.IsNullOrWhiteSpace(tags))
            sb.Append('\n').Append(ColorLabel("Tags")).Append(": ").Append(tags);
    }

    private static void AppendRules(StringBuilder sb, List<string> rules)
    {
        if (rules == null || rules.Count <= 0)
            return;

        sb.Append("\n\n");
        for (int i = 0; i < rules.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(rules[i]))
                continue;

            if (i > 0)
                sb.Append('\n');
            sb.Append(rules[i]);
        }
    }

    private static void AppendAddedValueHint(StringBuilder sb, List<string> rules)
    {
        if (!ContainsAddedValueMarkup(rules))
            return;

        sb.Append("\n\n").Append(ColorMuted("Blue numbers use Added Value when the assigned dice provide it."));
    }

    private static void AppendDamageRules(List<string> rules, SkillRuntime rt)
    {
        if (rt == null)
            return;

        if (rt.coreAction == CoreAction.BasicGuard || rt.kind == SkillKind.Guard)
        {
            if (SkillBehaviorRuntimeUtility.IsBehavior(rt, BleedDamageBehaviorId.BloodWard))
                rules.Add("Gain " + Blue("Guard") + " equal to total Bleed on all enemies.");
            else if (rt.guardValueMode == BaseEffectValueMode.X)
                rules.Add("Gain " + Blue(HasRuntimeValues(rt) ? ResolveX(rt).ToString() : "die value") + " Guard.");
            else if (rt.guardFlat > 0)
                rules.Add("Gain " + Blue(AddAction(rt.guardFlat, rt).ToString()) + " Guard.");

            if (rt.focusGainOnCast > 0)
                rules.Add("Gain " + rt.focusGainOnCast + " Focus.");
            return;
        }

        if (rt.coreAction == CoreAction.BasicStrike)
        {
            int baseDamage = rt.flatDamage > 0 ? rt.flatDamage : 4;
            rules.Add("Deal " + Blue(AddAction(baseDamage, rt).ToString()) + " damage.");
            if (rt.focusGainOnCast > 0)
                rules.Add("Gain " + rt.focusGainOnCast + " Focus.");
            return;
        }

        AppendPrimaryDamageRule(rules, rt);
        AppendBurnRules(rules, rt);
        AppendOtherStatusRules(rules, rt);
        AppendBehaviorRules(rules, rt);
        AppendConditionalRules(rules, rt);
        AppendSplitRoleRules(rules, rt);
    }

    private static void AppendPrimaryDamageRule(List<string> rules, SkillRuntime rt)
    {
        if (rt.kind != SkillKind.Attack)
            return;

        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, PhysicalDamageBehaviorId.HeavyCleave))
        {
            string value = HasRuntimeValues(rt)
                ? SkillOutputValueUtility.AddActionAddedValue(SkillBehaviorRuntimeUtility.GetHighestBaseValue(rt), rt).ToString()
                : "X";
            rules.Add("Deal " + Blue(value) + " damage. X is the highest Base Value in this group.");
            return;
        }

        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, IceDamageBehaviorId.WintersBite))
        {
            rules.Add("Deal " + Blue(AddAction(6, rt).ToString()) + " damage.");
            return;
        }

        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, IceDamageBehaviorId.ColdSnap))
        {
            string value = HasRuntimeValues(rt)
                ? SkillBehaviorRuntimeUtility.GetPerDieResolvedOutput(rt, 0).ToString()
                : "X";
            rules.Add("Left slot: deal " + Blue(value) + " damage.");
            return;
        }

        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, BleedDamageBehaviorId.BloodWard) ||
            SkillBehaviorRuntimeUtility.IsBehavior(rt, BleedDamageBehaviorId.Lacerate))
            return;

        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, PhysicalDamageBehaviorId.NoName) && !rt.conditionMet)
            return;

        if (IsXDamageFormula(rt))
        {
            rules.Add("Deal " + Blue(HasRuntimeValues(rt) ? ResolveX(rt).ToString() : "X") + " damage.");
            return;
        }

        if (rt.flatDamage > 0)
        {
            if (ShouldApplyStandardAddedValue(rt))
                rules.Add("Deal " + Blue(AddAction(rt.flatDamage, rt).ToString()) + " damage.");
            else
                rules.Add("Deal " + rt.flatDamage + " damage.");
        }
    }

    private static void AppendBurnRules(List<string> rules, SkillRuntime rt)
    {
        if (rt.consumesBurn && SkillBehaviorRuntimeUtility.IsBehavior(rt, FireDamageBehaviorId.Hellfire))
            rules.Add("Consume all Burn. Deal " + Mathf.Max(0, rt.burnDamagePerStack) + " damage per Burn.");

        if (rt.applyBurn)
        {
            if (rt.baseBurnValueMode == BaseEffectValueMode.X || rt.fireApplyBurnFromResolvedValue || SkillBehaviorRuntimeUtility.IsBehavior(rt, FireDamageBehaviorId.Ignite))
            {
                string burn = HasRuntimeValues(rt) ? ResolveX(rt).ToString() : "X";
                if (SkillBehaviorRuntimeUtility.IsBehavior(rt, FireDamageBehaviorId.Ignite))
                    rules.Add("Apply " + Blue(burn) + " Burn. If Base Value is odd, apply +" + rt.fireOddBaseBonusBurn + " Burn and Burn consumed on this target deals +1 damage for 2 turns.");
                else
                    rules.Add("Apply " + Blue(burn) + " Burn.");
            }
            else if (rt.burnAddStacks > 0)
            {
                rules.Add("Apply " + Blue(AddAction(rt.burnAddStacks, rt).ToString()) + " Burn.");
            }
        }

        if (rt.fireReapplyBurnPerExactBase)
        {
            rules.Add("For each die with Base " + rt.fireExactBaseForReapply + ", apply " + rt.fireBurnPerExactMatch + " Burn.");
        }
        else if (SkillBehaviorRuntimeUtility.IsBehavior(rt, FireDamageBehaviorId.Hellfire))
        {
            rules.Add("For each die with Base 7, apply 7 Burn.");
        }

        if (rt.fireApplyBurnFromLowestBase)
        {
            string burn = Blue(HasRuntimeValues(rt)
                ? SkillBehaviorRuntimeUtility.GetPerDieResolvedOutput(rt, SkillBehaviorRuntimeUtility.GetLowestBaseValueIndex(rt)).ToString()
                : "X");
            rules.Add("Lowest die: apply " + burn + " Burn.");
        }

        if (rt.fireGainGuardFromHighestBase)
        {
            string guard = Blue(HasRuntimeValues(rt)
                ? SkillBehaviorRuntimeUtility.GetPerDieResolvedOutput(rt, SkillBehaviorRuntimeUtility.GetHighestBaseValueIndex(rt)).ToString()
                : "X");
            rules.Add("Highest die: gain " + guard + " Guard.");
        }

        if (rt.fireApplyConsumeBonusDebuff && !SkillBehaviorRuntimeUtility.IsBehavior(rt, FireDamageBehaviorId.Ignite))
            rules.Add("For " + rt.fireConsumeBonusDebuffTurns + " turns, Burn consumed on this target deals +" + rt.fireConsumeBonusPerBurn + " extra damage per stack.");
    }

    private static void AppendOtherStatusRules(List<string> rules, SkillRuntime rt)
    {
        if (rt.applyMark)
            rules.Add("Apply Mark.");

        if (rt.applyBleed &&
            !SkillBehaviorRuntimeUtility.IsBehavior(rt, BleedDamageBehaviorId.Lacerate) &&
            !SkillBehaviorRuntimeUtility.IsBehavior(rt, BleedDamageBehaviorId.Hemorrhage))
            rules.Add("Apply " + Blue(AddAction(rt.bleedTurns, rt).ToString()) + " Bleed.");

        if (rt.applyFreeze)
        {
            int pct = Mathf.RoundToInt(Mathf.Clamp01(rt.freezeChance) * 100f);
            rules.Add("Apply Freeze (" + pct + "%).");
        }
    }

    private static void AppendBehaviorRules(List<string> rules, SkillRuntime rt)
    {
        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, PhysicalDamageBehaviorId.PrecisionStrike))
            rules.Add("If this hit Crits, gain +2 Added Value for this hit.");

        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, PhysicalDamageBehaviorId.BrutalSmash))
            rules.Add("If the target had Mark before hit, refund 1 Focus.");

        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, PhysicalDamageBehaviorId.HeavyCleave))
            rules.Add("Added Value comes from all dice in this group.");

        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, PhysicalDamageBehaviorId.Execution))
            rules.Add("If this hit kills the target, carry over overkill into the first Attack or Sunder next turn.");

        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, PhysicalDamageBehaviorId.FatedSunder))
            rules.Add("If Base Value equals the Fate Number, clear Guard before damage. This skill does not benefit from Stagger.");

        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, PhysicalDamageBehaviorId.NoName) && rt.conditionMet == false)
            rules.Add("Requires Base Value 1; otherwise this cast has no damage and no Focus cost.");

        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, LightningDamageBehaviorId.StaticConduit))
            rules.Add("If the target has Mark, apply Mark to other enemies in its row.");

        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, LightningDamageBehaviorId.SparkBarrage))
            rules.Add("If Base Value is even, this hit bounces to another target.");

        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, LightningDamageBehaviorId.Overload))
            rules.Add("Add 5 damage to this hit for each Marked enemy.");

        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, LightningDamageBehaviorId.Thunderclap))
            rules.Add("Hits one enemy row.");

        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, IceDamageBehaviorId.Shatter))
            rules.Add("Gain Guard equal to damage dealt.");

        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, IceDamageBehaviorId.WintersBite))
            rules.Add("Extend Chilled by 1 turn.");

        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, IceDamageBehaviorId.ColdSnap))
        {
            string guard = Blue(HasRuntimeValues(rt)
                ? SkillBehaviorRuntimeUtility.GetPerDieResolvedOutput(rt, 1).ToString()
                : "X");
            rules.Add("Right slot: gain " + guard + " Guard.");
        }

        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, BleedDamageBehaviorId.Lacerate))
        {
            string bleed = Blue(AddAction(Mathf.Max(0, rt.bleedTurns), rt).ToString());
            rules.Add("Apply " + bleed + " Bleed. If this hit Crits, apply Bleed again.");
        }

        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, BleedDamageBehaviorId.Hemorrhage))
            rules.Add("Apply " + Blue("Bleed") + " equal to the HP lost last enemy turn, plus Added Value.");
    }

    private static void AppendConditionalRules(List<string> rules, SkillRuntime rt)
    {
        if (rt == null || !rt.conditionalOutcomeEnabled || rt.conditionalOutcomeType == ConditionalOutcomeType.None)
            return;

        string prefix = rt.conditionMet ? "Condition met: " : "If condition is met: ";
        string value = rt.conditionalOutcomeValueMode == ConditionalOutcomeValueMode.X
            ? Blue(HasRuntimeValues(rt) ? ResolveX(rt).ToString() : "X")
            : Blue(AddAction(rt.conditionalOutcomeFlatValue, rt).ToString());

        switch (rt.conditionalOutcomeType)
        {
            case ConditionalOutcomeType.DealDamage:
                rules.Add(prefix + "deal " + value + " damage.");
                break;
            case ConditionalOutcomeType.ApplyBurn:
                rules.Add(prefix + "apply " + value + " Burn.");
                break;
            case ConditionalOutcomeType.GainGuard:
                rules.Add(prefix + "gain " + value + " Guard.");
                break;
            case ConditionalOutcomeType.GainAddedValue:
                rules.Add(prefix + "grant " + value + " Added Value to this action.");
                break;
        }
    }

    private static void AppendSplitRoleRules(List<string> rules, SkillRuntime rt)
    {
        if (rt == null || !rt.splitRoleEnabled)
            return;

        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, FireDamageBehaviorId.Cauterize))
            return;

        AppendSplitRoleBranch(rules, rt, "Lowest die", SkillBehaviorRuntimeUtility.GetLowestBaseValueIndex(rt), rt.splitRoleLowestOutcome);
        AppendSplitRoleBranch(rules, rt, "Highest die", SkillBehaviorRuntimeUtility.GetHighestBaseValueIndex(rt), rt.splitRoleHighestOutcome);
    }

    private static void AppendSplitRoleBranch(List<string> rules, SkillRuntime rt, string label, int index, SplitRoleBranchOutcome outcome)
    {
        if (outcome == SplitRoleBranchOutcome.None)
            return;

        string value = Blue(HasRuntimeValues(rt) ? SkillBehaviorRuntimeUtility.GetPerDieResolvedOutput(rt, index).ToString() : label);
        switch (outcome)
        {
            case SplitRoleBranchOutcome.Burn:
                rules.Add(label + ": apply " + value + " Burn.");
                break;
            case SplitRoleBranchOutcome.Guard:
                rules.Add(label + ": gain " + value + " Guard.");
                break;
        }
    }

    private static void AppendBuffFireModuleRules(List<string> rules, SkillBuffDebuffSO skill)
    {
        if (skill.fireModules == null)
            return;

        if (skill.fireModules.grantEmberWeapon)
        {
            rules.Add("For " + skill.fireModules.emberWeaponTurns + " turns, Basic Attack deals +" + skill.fireModules.basicAttackBonusDamage + " damage.");
            if (skill.fireModules.basicAttackAppliesBurnEqualDamage)
                rules.Add(skill.fireModules.basicAttackBurnOnCritOnly
                    ? "If that Basic Attack Crits, apply Burn equal to damage dealt."
                    : "That Basic Attack applies Burn equal to damage dealt.");
        }

        if (skill.fireModules.applyConsumeBonusDebuff)
        {
            rules.Add("For " + skill.fireModules.consumeBonusDebuffTurns + " turns, Burn consumed on this target deals +" + skill.fireModules.consumeBonusPerBurn + " extra damage per stack.");
        }
    }

    private static void AppendBuffEffectRules(List<string> rules, SkillBuffDebuffSO skill, SkillRuntime runtime)
    {
        if (skill.effects == null)
            return;

        for (int i = 0; i < skill.effects.Count; i++)
        {
            BuffDebuffEffectEntry e = skill.effects[i];
            if (e == null)
                continue;

            switch (e.id)
            {
                case BuffDebuffEffectId.DamageMultiplier:
                    rules.Add("Damage x" + e.GetDamageMultiplier().ToString("0.##") + " for " + Mathf.Max(1, e.durationTurns) + " turns.");
                    break;
                case BuffDebuffEffectId.FocusDelayed:
                    rules.Add("Gain " + e.GetFocusAmount() + " Focus" + FormatDelay(skill.applyDelayTurns) + ".");
                    break;
                case BuffDebuffEffectId.HealFlat:
                    rules.Add("Heal " + e.GetHealAmount() + " HP.");
                    break;
                case BuffDebuffEffectId.HealByDiceSum:
                    rules.Add("Heal " + Blue(HasRuntimeValues(runtime) ? ResolveX(runtime).ToString() : "dice value") + " HP.");
                    break;
                case BuffDebuffEffectId.DiceAllDelta:
                    rules.Add("Modify all dice by " + Signed(e.GetDiceAllDelta()) + " for " + Mathf.Max(1, e.durationTurns) + " turns.");
                    break;
                case BuffDebuffEffectId.ParityFocusDelta:
                    rules.Add("Even dice: " + Signed(e.parityEvenDelta) + " Focus. Odd dice: " + Signed(e.parityOddDelta) + " Focus.");
                    break;
                case BuffDebuffEffectId.SlotCollapse:
                    rules.Add("Collapse slots for " + Mathf.Max(1, e.durationTurns) + " turn.");
                    break;
            }
        }
    }

    private static void AppendAilmentRules(List<string> rules, SkillBuffDebuffSO skill)
    {
        if (!skill.applyAilment || skill.ailment == null)
            return;

        rules.Add("Apply " + skill.ailment.ailment + " for " + Mathf.Max(1, skill.ailment.durationTurns) + " turns.");
    }

    private static SkillRuntime BuildBuffRuntime(SkillBuffDebuffSO skill)
    {
        if (skill == null)
            return null;

        return new SkillRuntime
        {
            sourceAsset = skill,
            useV2Targeting = true,
            targetRuleV2 = skill.target,
            kind = SkillKind.Utility,
            target = SkillTargetRuleUtility.IsEnemySideTarget(skill.target) ? TargetRule.Enemy : TargetRule.Self,
            slotsRequired = Mathf.Clamp(skill.slotsRequired, 1, 3),
            focusCost = Mathf.Max(0, skill.focusCost),
            focusGainOnCast = skill.focusGainOnCast
        };
    }

    private static string BuildDamageTags(SkillRuntime rt)
    {
        if (rt == null)
            return string.Empty;

        if (rt.kind == SkillKind.Guard)
            return "Guard";

        if (rt.kind == SkillKind.Attack)
            return "Attack / " + rt.group + " / " + rt.range;

        return rt.kind.ToString();
    }

    private static string FormatElement(ElementType element)
    {
        return element.ToString();
    }

    private static string InferBuffElement(SkillBuffDebuffSO skill)
    {
        if (skill == null)
            return string.Empty;

        switch (skill.behaviorId)
        {
            case BuffBehaviorId.Fire_EmberWeapon:
            case BuffBehaviorId.Fire_Cinderbrand:
                return ElementType.Fire.ToString();
            case BuffBehaviorId.Bleed_Siphon:
                return "Bleed";
        }

        string key = ((skill.displayName ?? string.Empty) + " " + (skill.name ?? string.Empty)).ToLowerInvariant();
        if (key.Contains("fire") || key.Contains("ember") || key.Contains("cinder"))
            return ElementType.Fire.ToString();
        if (key.Contains("ice") || key.Contains("frost") || key.Contains("permafrost"))
            return ElementType.Ice.ToString();
        if (key.Contains("lightning") || key.Contains("spark") || key.Contains("thunder") || key.Contains("flash"))
            return ElementType.Lightning.ToString();
        if (key.Contains("bleed") || key.Contains("blood") || key.Contains("siphon"))
            return "Bleed";
        if (key.Contains("physical") || key.Contains("sunder") || key.Contains("strike"))
            return ElementType.Physical.ToString();

        return string.Empty;
    }

    private static string BuildBuffTags(SkillBuffDebuffSO skill)
    {
        if (skill == null)
            return string.Empty;

        return skill.identity.ToString();
    }

    private static string FormatTarget(SkillTargetRule target)
    {
        switch (target)
        {
            case SkillTargetRule.SingleEnemy: return "Single Enemy";
            case SkillTargetRule.SingleAlly: return "Single Ally";
            case SkillTargetRule.RowEnemies: return "Enemy Row";
            case SkillTargetRule.RowAllies: return "Ally Row";
            case SkillTargetRule.AllEnemies: return "All Enemies";
            case SkillTargetRule.AllAllies: return "All Allies";
            case SkillTargetRule.Self:
            default:
                return "Self";
        }
    }

    private static bool ShouldApplyStandardAddedValue(SkillRuntime rt)
    {
        if (rt == null || rt.kind != SkillKind.Attack)
            return false;

        if (rt.coreAction == CoreAction.BasicStrike)
            return true;

        return Mathf.Approximately(rt.dieMultiplier, 0f) && rt.flatDamage > 0;
    }

    private static bool IsXDamageFormula(SkillRuntime rt)
    {
        if (rt == null || rt.kind != SkillKind.Attack)
            return false;

        if (rt.baseDamageValueMode == BaseEffectValueMode.X || rt.fireUseXFormula)
            return true;

        return rt.flatDamage == 0 && rt.dieMultiplier > 0f;
    }

    private static bool HasRuntimeValues(SkillRuntime rt)
    {
        if (rt == null || rt.localResolvedValues == null || rt.localResolvedValues.Count <= 0)
            return false;

        for (int i = 0; i < rt.localResolvedValues.Count; i++)
        {
            if (rt.localResolvedValues[i] > 0)
                return true;
        }

        if (rt.localBaseValues == null)
            return false;

        for (int i = 0; i < rt.localBaseValues.Count; i++)
        {
            if (rt.localBaseValues[i] > 0)
                return true;
        }

        return false;
    }

    private static int ResolveX(SkillRuntime rt)
    {
        return SkillOutputValueUtility.ResolveXValue(0, rt);
    }

    private static int AddAction(int baseAmount, SkillRuntime rt)
    {
        if (!HasRuntimeValues(rt))
            return Mathf.Max(0, baseAmount);

        return SkillOutputValueUtility.AddActionAddedValue(baseAmount, rt);
    }

    private static string ColorLabel(string text) => "<color=" + LabelColor + ">" + text + "</color>";
    private static string ColorMuted(string text) => "<color=" + MutedColor + ">" + text + "</color>";
    private static string Blue(string text) => "<color=" + AddedValueColor + ">" + text + "</color>";
    private static string Signed(int value) => value >= 0 ? "+" + value : value.ToString();
    private static string FormatDelay(int delayTurns) => delayTurns > 0 ? " after " + delayTurns + " turn" + (delayTurns > 1 ? "s" : "") : string.Empty;

    private static string ColorQuotedAddedValues(string text)
    {
        if (string.IsNullOrEmpty(text) || !text.Contains("\""))
            return text;

        StringBuilder sb = new StringBuilder(text.Length + 32);
        bool inQuote = false;
        int segmentStart = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] != '"')
                continue;

            if (!inQuote)
            {
                sb.Append(text, segmentStart, i - segmentStart);
                segmentStart = i + 1;
                inQuote = true;
            }
            else
            {
                string value = text.Substring(segmentStart, i - segmentStart);
                sb.Append(Blue(value));
                segmentStart = i + 1;
                inQuote = false;
            }
        }

        if (segmentStart < text.Length)
        {
            if (inQuote)
                sb.Append('"');
            sb.Append(text, segmentStart, text.Length - segmentStart);
        }

        return sb.ToString();
    }

    private static bool ContainsAddedValueMarkup(List<string> rules)
    {
        if (rules == null)
            return false;

        for (int i = 0; i < rules.Count; i++)
        {
            if (!string.IsNullOrEmpty(rules[i]) && rules[i].Contains(AddedValueColor))
                return true;
        }

        return false;
    }
}
