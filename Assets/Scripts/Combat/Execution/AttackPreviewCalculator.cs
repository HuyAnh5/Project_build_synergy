using UnityEngine;

public static class AttackPreviewCalculator
{
    private const int DefaultBurnConsumeDamagePerStack = 2;
    private const int MarkDirectBonusDamage = 3;

    public static SkillExecutor.AttackPreview BuildAttackPreview(SkillRuntime rt, CombatActor caster, CombatActor target, int dieValue)
    {
        SkillExecutor.AttackPreview preview = new SkillExecutor.AttackPreview
        {
            effectiveDieValue = Mathf.Max(0, dieValue),
            baseDamage = 0,
            bonusDamage = 0,
            finalDamage = 0,
            canDealDamage = false,
            consumesStagger = false
        };

        if (rt == null || rt.kind != SkillKind.Attack)
            return preview;

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

        if (IsBasicStrike(rt) && caster != null && caster.status != null && caster.status.emberWeaponTurns > 0)
        {
            int emberBonus = Mathf.Max(0, caster.status.emberWeaponBonusDamage);
            preview.bonusDamage += emberBonus;
            dmg += emberBonus;
        }

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

        if (rt.element == ElementType.Fire && rt.consumesBurn && target != null && target.status != null)
        {
            int burnStacks = target.status.burnStacks;
            if (burnStacks > 0)
            {
                float burnMul = (ps != null) ? ps.GetBurnConsumeMultiplier() : 1f;
                int damagePerStack = GetBurnConsumeDamagePerStack(rt, target);
                int add = Mathf.FloorToInt(burnStacks * damagePerStack * Mathf.Max(0f, burnMul));
                preview.bonusDamage += add;
                dmg += add;
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
        preview.canDealDamage = preview.finalDamage > 0;
        return preview;
    }

    private static void ApplyBehaviorPreviewBonuses(SkillRuntime rt, CombatActor caster, CombatActor target, ref SkillExecutor.AttackPreview preview, ref int dmg)
    {
        if (ApplyBaseEffectBehaviorPreview(rt, ref preview, ref dmg))
            return;

        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, PhysicalDamageBehaviorId.PrecisionStrike))
        {
            if (SkillBehaviorRuntimeUtility.TryGetSingleBaseValue(rt, out int baseValue) &&
                (baseValue % 2) == 0 &&
                !rt.localCritAny)
            {
                int critAdd = Mathf.FloorToInt(baseValue * DiceSlotRig.PhysicalCritPercent);
                preview.bonusDamage += critAdd;
                dmg += critAdd;
            }
            return;
        }

        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, PhysicalDamageBehaviorId.HeavyCleave))
        {
            int add = SkillBehaviorRuntimeUtility.GetHighestBaseValue(rt);
            if (add > 0)
            {
                preview.bonusDamage += add;
                dmg += add;
            }
            return;
        }

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

        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, IceDamageBehaviorId.ColdSnap))
        {
            int low = SkillBehaviorRuntimeUtility.GetLowestBaseValue(rt);
            preview.baseDamage = Mathf.Max(0, low);
            dmg = preview.baseDamage + preview.bonusDamage;
            return;
        }

        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, LightningDamageBehaviorId.Overload))
        {
            int add = 4 * SkillBehaviorRuntimeUtility.CountMarkedEnemies(caster);
            if (add > 0)
            {
                preview.bonusDamage += add;
                dmg += add;
            }
            return;
        }

        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, LightningDamageBehaviorId.Thunderclap))
        {
            int high = SkillBehaviorRuntimeUtility.GetHighestResolvedValue(rt);
            preview.baseDamage = Mathf.Max(0, high);
            dmg = preview.baseDamage + preview.bonusDamage;

            int add = 4 * SkillBehaviorRuntimeUtility.CountMarkedEnemies(caster);
            if (add > 0)
            {
                preview.bonusDamage += add;
                dmg += add;
            }
            return;
        }

        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, BleedDamageBehaviorId.BloodWard))
        {
            preview.baseDamage = 0;
            preview.bonusDamage = 0;
            dmg = 0;
        }
    }

    private static void ApplyBaseEffectPreview(SkillRuntime rt, ref SkillExecutor.AttackPreview preview)
    {
        if (rt == null || rt.kind != SkillKind.Attack)
            return;

        if (rt.baseDamageValueMode == BaseEffectValueMode.X || rt.fireUseXFormula)
        {
            preview.baseDamage = Mathf.Max(0, preview.effectiveDieValue);
        }
    }

    private static bool ApplyBaseEffectBehaviorPreview(SkillRuntime rt, ref SkillExecutor.AttackPreview preview, ref int dmg)
    {
        if (rt == null || rt.kind != SkillKind.Attack)
            return false;

        if (rt.baseDamageValueMode == BaseEffectValueMode.X || rt.fireUseXFormula)
        {
            preview.baseDamage = Mathf.Max(0, preview.effectiveDieValue);
            dmg = preview.baseDamage + preview.bonusDamage;
        }

        return rt.baseDamageValueMode == BaseEffectValueMode.X || rt.fireUseXFormula;
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

        int perStack = Mathf.Max(DefaultBurnConsumeDamagePerStack, rt.burnDamagePerStack);
        if (target != null && target.status != null && target.status.cinderbrandTurns > 0)
            perStack += Mathf.Max(0, target.status.cinderbrandBonusPerBurn);
        return perStack;
    }

    private static bool IsBasicStrike(SkillRuntime rt)
    {
        if (rt == null)
            return false;
        return rt.coreAction == CoreAction.BasicStrike;
    }

    private static bool ShouldApplyStandardAddedValue(SkillRuntime rt)
    {
        if (rt == null || rt.kind != SkillKind.Attack)
            return false;

        if (IsBasicStrike(rt))
            return true;

        return Mathf.Approximately(rt.dieMultiplier, 0f) && rt.flatDamage > 0;
    }

    // Fail is an action-level penalty.
    // Even if a 2-slot/3-slot action contains multiple failing dice,
    // the action only halves its base damage once.
    private static bool ShouldApplyFailPenaltyOnce(SkillRuntime rt)
    {
        return rt != null && rt.localFailPenaltyAny;
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
