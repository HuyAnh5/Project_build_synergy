using System.Collections.Generic;
using UnityEngine;

public static class AttackPreviewCalculator
{
    private const int DefaultBurnConsumeDamagePerStack = 0;
    private const int MarkDirectBonusDamage = 3;

    public static SkillExecutor.AttackPreview BuildAttackPreview(SkillRuntime rt, CombatActor caster, CombatActor target, int dieValue)
    {
        SkillExecutor.AttackPreview preview = new SkillExecutor.AttackPreview
        {
            effectiveDieValue = Mathf.Max(0, dieValue),
            baseDamage = 0,
            bonusDamage = 0,
            finalDamage = 0,
            primaryDamage = 0,
            burnConsumeDamage = 0,
            canDealDamage = false,
            consumesStagger = false
        };

        if (rt == null || rt.kind != SkillKind.Attack)
            return preview;

        SkillDamageSO sourceSkill = SkillGameplayResolver.GetSourceSkill(rt);
        if (SkillGameplayResolver.CanResolveWithNewPipeline(sourceSkill))
        {
            SkillResolvedResult resolved = SkillGameplayResolver.Resolve(rt, caster, target);
            int immediatePrimaryDamage = 0;
            int immediateSecondaryDamage = 0;
            int followUpDamage = 0;
            if (resolved != null && resolved.effects != null)
            {
                for (int i = 0; i < resolved.effects.Count; i++)
                {
                    ResolvedEffect effect = resolved.effects[i];
                    if (effect == null || (effect.type != SkillEffectType.DealDamage && effect.type != SkillEffectType.DealSecondaryDamage))
                        continue;
                    if (effect.targetActor != null && effect.targetActor != target)
                        continue;

                    if (effect.sameActionFollowUp)
                    {
                        followUpDamage += Mathf.Max(0, effect.value);
                    }
                    else if (effect.type == SkillEffectType.DealDamage)
                    {
                        immediatePrimaryDamage += Mathf.Max(0, effect.value);
                    }
                    else
                    {
                        immediateSecondaryDamage += Mathf.Max(0, effect.value);
                    }
                }
            }

            if (CanUseMarkPayoff(rt, target) && rt.element != ElementType.Lightning)
                immediatePrimaryDamage += MarkDirectBonusDamage;

            PassiveSystem resolvedPassiveSystem = caster != null ? caster.GetComponent<PassiveSystem>() : null;
            if (resolvedPassiveSystem != null)
            {
                if (immediatePrimaryDamage > 0)
                    immediatePrimaryDamage = resolvedPassiveSystem.PreviewOutgoingDamageAgainstTarget(target, immediatePrimaryDamage);
                if (immediateSecondaryDamage > 0)
                    immediateSecondaryDamage = resolvedPassiveSystem.PreviewOutgoingDamageAgainstTarget(target, immediateSecondaryDamage);
                if (followUpDamage > 0)
                    followUpDamage = resolvedPassiveSystem.PreviewOutgoingDamageAgainstTarget(target, followUpDamage);

            }

            preview.baseDamage = Mathf.Max(0, immediatePrimaryDamage + immediateSecondaryDamage + followUpDamage);
            bool resolvedAttemptedPrimaryDamage = immediatePrimaryDamage > 0;
            if (resolvedAttemptedPrimaryDamage && CanConsumeStagger(rt, target))
            {
                immediatePrimaryDamage = Mathf.FloorToInt(immediatePrimaryDamage * 1.2f);
                if (immediatePrimaryDamage < 1)
                    immediatePrimaryDamage = 1;
                preview.consumesStagger = true;
            }
            preview.primaryDamage = Mathf.Max(0, immediatePrimaryDamage + immediateSecondaryDamage);
            preview.finalDamage = Mathf.Max(0, preview.primaryDamage + followUpDamage);
            preview.canDealDamage = preview.finalDamage > 0;
            return preview;
        }

        preview.baseDamage = rt.CalculateDamage(preview.effectiveDieValue);
        ApplyBaseEffectPreview(rt, ref preview);

        float statusOutMul = 1f;
        if (caster != null && caster.status != null)
            statusOutMul = caster.status.GetOutgoingDamageMultiplier();

        PassiveSystem ps = null;
        if (caster != null) ps = caster.GetComponent<PassiveSystem>();

        float passiveOutMul = 1f;
        if (ps != null)
            passiveOutMul = ps.GetOutgoingDamageMultiplier(rt, target);

        float sleepMul = 1f;
        if (target != null && target.status != null && target.status.HasAilmentType(AilmentType.Sleep) && rt.group != DamageGroup.Effect)
            sleepMul = 1.5f;

        bool targetHasGuard = target != null && target.guardPool > 0;
        int dmg = preview.baseDamage;

        ApplyBehaviorPreviewBonuses(rt, caster, target, ref preview, ref dmg);

        if (rt.conditionMet && rt.conditionalOutcomeEnabled)
        {
            int conditionalDamage = GetConditionalDamageBonus(rt, preview.effectiveDieValue);
            if (conditionalDamage > 0)
            {
                preview.bonusDamage += conditionalDamage;
                dmg += conditionalDamage;
            }
        }

        if (ShouldApplyFailPenaltyOnce(rt) && preview.baseDamage > 0)
        {
            int reducedBaseDamage = Mathf.FloorToInt(preview.baseDamage * 0.5f);
            dmg -= preview.baseDamage;
            preview.baseDamage = Mathf.Max(0, reducedBaseDamage);
            dmg += preview.baseDamage;
        }

        if (ShouldApplyStandardAddedValue(rt))
        {
            int actionAddedValue = SkillOutputValueUtility.GetTotalActionAddedValue(rt);
            if (actionAddedValue > 0)
            {
                preview.bonusDamage += actionAddedValue;
                dmg += actionAddedValue;
            }
        }

        if (!Mathf.Approximately(statusOutMul, 1f))
            dmg = Mathf.FloorToInt(dmg * statusOutMul);

        if (!Mathf.Approximately(passiveOutMul, 1f))
            dmg = Mathf.FloorToInt(dmg * passiveOutMul);

        if (!Mathf.Approximately(sleepMul, 1f))
            dmg = Mathf.FloorToInt(dmg * sleepMul);

        if (rt.group == DamageGroup.Sunder && rt.sunderBonusIfTargetHasGuard && targetHasGuard)
            dmg = Mathf.FloorToInt(dmg * Mathf.Max(0f, rt.sunderGuardDamageMultiplier));

        int burnConsumeDamage = 0;
        if (rt.element == ElementType.Fire && rt.consumesBurn && target != null && target.status != null)
        {
            int burnStacks = target.status.burnStacks;
            if (burnStacks > 0)
            {
                float burnMul = (ps != null) ? ps.GetBurnConsumeMultiplier() : 1f;
                int damagePerStack = GetBurnConsumeDamagePerStack(rt, target);
                burnConsumeDamage = Mathf.FloorToInt(burnStacks * damagePerStack * Mathf.Max(0f, burnMul));
                preview.bonusDamage += burnConsumeDamage;
                dmg += burnConsumeDamage;
            }
        }

        if (CanUseMarkPayoff(rt, target) && rt.element != ElementType.Lightning)
        {
            preview.bonusDamage += MarkDirectBonusDamage;
            dmg += MarkDirectBonusDamage;
        }

        bool attemptedDamage = preview.baseDamage > 0 || preview.bonusDamage > 0;
        if (attemptedDamage && dmg < 1)
            dmg = 1;

        if (attemptedDamage && CanConsumeStagger(rt, target))
        {
            dmg = Mathf.FloorToInt(dmg * 1.2f);
            if (dmg < 1)
                dmg = 1;
            preview.consumesStagger = true;
        }

        preview.finalDamage = Mathf.Max(0, dmg);
        if (ps != null)
        {
            preview.finalDamage = ps.PreviewOutgoingDamageAgainstTarget(target, preview.finalDamage);
        }
        preview.burnConsumeDamage = Mathf.Max(0, burnConsumeDamage);
        preview.primaryDamage = Mathf.Max(0, preview.finalDamage - preview.burnConsumeDamage);
        if (preview.primaryDamage <= 0 && preview.finalDamage > 0 && preview.burnConsumeDamage > 0)
            preview.burnConsumeDamage = preview.finalDamage;
        else if (preview.burnConsumeDamage > preview.finalDamage)
            preview.burnConsumeDamage = preview.finalDamage;

        preview.canDealDamage = preview.finalDamage > 0;
        return preview;
    }

    private static void ApplyBehaviorPreviewBonuses(SkillRuntime rt, CombatActor caster, CombatActor target, ref SkillExecutor.AttackPreview preview, ref int dmg)
    {
        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, PhysicalDamageBehaviorId.NoName) && !rt.conditionMet)
        {
            preview.baseDamage = 0;
            preview.bonusDamage = 0;
            dmg = 0;
            return;
        }

        ApplyBaseEffectBehaviorPreview(rt, ref preview, ref dmg);

        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, PhysicalDamageBehaviorId.Execution))
        {
            SkillCombatState state = caster != null ? caster.GetComponent<SkillCombatState>() : null;
            if (state != null && state.ExecutionCarryActive > 0)
            {
                preview.bonusDamage += state.ExecutionCarryActive;
                dmg += state.ExecutionCarryActive;
            }
            return;
        }

        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, IceDamageBehaviorId.WintersBite))
        {
            preview.baseDamage = 6;
            dmg = 6 + preview.bonusDamage;
            return;
        }

        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, LightningDamageBehaviorId.Overload))
        {
            int add = 5 * SkillBehaviorRuntimeUtility.CountMarkedEnemies(caster);
            if (add > 0)
            {
                preview.bonusDamage += add;
                dmg += add;
            }
            return;
        }

    }

    private static void ApplyBaseEffectPreview(SkillRuntime rt, ref SkillExecutor.AttackPreview preview)
    {
        if (rt == null || rt.kind != SkillKind.Attack)
            return;

        if (IsXDamageFormula(rt))
        {
            preview.baseDamage = GetActionBaseValue(rt, preview.effectiveDieValue);
        }
    }

    private static bool ApplyBaseEffectBehaviorPreview(SkillRuntime rt, ref SkillExecutor.AttackPreview preview, ref int dmg)
    {
        if (rt == null || rt.kind != SkillKind.Attack)
            return false;

        if (IsXDamageFormula(rt))
        {
            preview.baseDamage = GetActionBaseValue(rt, preview.effectiveDieValue);
            dmg = preview.baseDamage + preview.bonusDamage;
        }

        return IsXDamageFormula(rt);
    }

    public static bool CanUseMarkPayoff(SkillRuntime rt, CombatActor target)
    {
        return rt != null &&
               rt.kind == SkillKind.Attack &&
               rt.group != DamageGroup.Effect &&
               rt.canUseMarkMultiplier &&
               target != null &&
               target.status != null &&
               target.status.marked;
    }

    public static bool CanConsumeStagger(SkillRuntime rt, CombatActor target)
    {
        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, PhysicalDamageBehaviorId.FatedSunder))
            return false;

        return rt != null &&
               target != null &&
               target.status != null &&
               target.status.staggered &&
               rt.kind == SkillKind.Attack &&
               rt.group != DamageGroup.Effect;
    }

    public static int GetBurnConsumeDamagePerStack(SkillRuntime rt, CombatActor target = null)
    {
        if (rt == null) return DefaultBurnConsumeDamagePerStack;

        int perStack = rt.burnDamagePerStack > 0 ? rt.burnDamagePerStack : DefaultBurnConsumeDamagePerStack;
        if (target != null && target.status != null && target.status.cinderbrandTurns > 0)
            perStack += Mathf.Max(0, target.status.cinderbrandBonusPerBurn);
        return perStack;
    }

    private static bool ShouldApplyStandardAddedValue(SkillRuntime rt)
    {
        if (rt == null || rt.kind != SkillKind.Attack)
            return false;

        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, PhysicalDamageBehaviorId.NoName) && !rt.conditionMet)
            return false;

        if (SkillOutputValueUtility.IsMeleeAttack(rt))
            return true;

        if (IsXDamageFormula(rt))
            return true;

        return Mathf.Approximately(rt.dieMultiplier, 0f) && rt.flatDamage > 0;
    }

    // Fail is an action-level penalty.
    // Even if a 2-slot/3-slot action contains multiple failing dice,
    // the action only halves its base damage once.
    private static bool ShouldApplyFailPenaltyOnce(SkillRuntime rt)
    {
        if (rt == null || !rt.localFailPenaltyAny)
            return false;

        return true;
    }

    private static bool IsXDamageFormula(SkillRuntime rt)
    {
        if (rt == null || rt.kind != SkillKind.Attack)
            return false;

        if (rt.baseDamageValueMode == BaseEffectValueMode.X || rt.fireUseXFormula)
            return true;

        return rt.flatDamage == 0 && rt.dieMultiplier > 0f;
    }

    private static int GetActionBaseValue(SkillRuntime rt, int fallbackResolvedValue)
    {
        if (rt == null)
            return Mathf.Max(0, fallbackResolvedValue);

        IReadOnlyList<int> outputBaseValues = rt.localOutputBaseValues != null && rt.localOutputBaseValues.Count > 0
            ? rt.localOutputBaseValues
            : rt.localBaseValues;
        if (outputBaseValues == null || outputBaseValues.Count <= 0)
            return Mathf.Max(0, fallbackResolvedValue);

        int baseOutput = 0;
        for (int i = 0; i < outputBaseValues.Count; i++)
            baseOutput += Mathf.Max(0, outputBaseValues[i]);

        return Mathf.Max(0, baseOutput);
    }

    private static int GetConditionalDamageBonus(SkillRuntime rt, int effectiveDieValue)
    {
        if (rt == null || !rt.conditionalOutcomeEnabled || rt.conditionalOutcomeType != ConditionalOutcomeType.DealDamage)
            return 0;

        switch (rt.conditionalOutcomeValueMode)
        {
            case ConditionalOutcomeValueMode.X:
                return SkillOutputValueUtility.ResolveXValue(effectiveDieValue, rt);

            case ConditionalOutcomeValueMode.Flat:
            default:
                return SkillOutputValueUtility.AddActionAddedValue(rt.conditionalOutcomeFlatValue, rt);
        }
    }

}
