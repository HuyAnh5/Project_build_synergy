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
                int add = Mathf.FloorToInt(burnStacks * GetBurnConsumeDamagePerStack(rt) * Mathf.Max(0f, burnMul));
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
}
