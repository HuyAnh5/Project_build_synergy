using UnityEngine;

public static class AttackPreviewCalculator
{
    private const int DefaultBurnConsumeDamagePerStack = 2;
    private const int MarkDirectBonusDamage = 4;

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
            preview.bonusDamage += 1;
            dmg += 1;
        }

        ApplyBehaviorPreviewBonuses(rt, caster, target, ref preview, ref dmg);

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
                int damagePerStack = GetBurnConsumeDamagePerStack(rt);
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

    public static int GetBurnConsumeDamagePerStack(SkillRuntime rt)
    {
        if (rt == null) return DefaultBurnConsumeDamagePerStack;
        return Mathf.Max(DefaultBurnConsumeDamagePerStack, rt.burnDamagePerStack);
    }

    private static bool IsBasicStrike(SkillRuntime rt)
    {
        if (rt == null)
            return false;
        return rt.coreAction == CoreAction.BasicStrike;
    }
}
