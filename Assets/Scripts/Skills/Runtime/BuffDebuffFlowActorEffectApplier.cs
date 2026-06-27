using UnityEngine;

internal static class BuffDebuffFlowActorEffectApplier
{
    public static void Apply(BuffDebuffFlowEffectData effect, CombatActor caster, CombatActor selectedTarget)
    {
        if (effect == null || caster == null)
            return;

        CombatActor target = BuffDebuffFlowTargetResolver.Resolve(effect.target, caster, selectedTarget);
        switch (effect.type)
        {
            case BuffDebuffFlowEffectType.RepeatFirstSkillNextTurn:
                GrantRepeatFirstSkill(effect, caster);
                break;
            case BuffDebuffFlowEffectType.NextSkillAddValue:
                GrantNextSkillAddedValue(effect, caster);
                break;
            case BuffDebuffFlowEffectType.EmberWeapon:
                GrantEmberWeapon(effect, caster);
                break;
            case BuffDebuffFlowEffectType.GainAP:
                GainFocus(effect, caster);
                break;
            case BuffDebuffFlowEffectType.GainGuard:
                GainGuard(effect, target);
                break;
            case BuffDebuffFlowEffectType.Heal:
                target?.Heal(Mathf.Max(0, effect.amount));
                break;
            case BuffDebuffFlowEffectType.ApplyStatus:
                ApplyStatus(effect, target);
                break;
        }
    }

    private static void GrantRepeatFirstSkill(BuffDebuffFlowEffectData effect, CombatActor caster)
    {
        if (caster.status != null)
            caster.status.GrantRepeatFirstSkillNextTurn(Mathf.Max(1, effect.amount));
    }

    private static void GrantNextSkillAddedValue(BuffDebuffFlowEffectData effect, CombatActor caster)
    {
        if (caster.status != null)
            caster.status.GrantNextSkillAddedValue(Mathf.Max(0, effect.amount));
    }

    private static void GrantEmberWeapon(BuffDebuffFlowEffectData effect, CombatActor caster)
    {
        if (caster.status == null)
            return;

        caster.status.GrantEmberWeapon(
            Mathf.Max(1, effect.durationTurns),
            Mathf.Max(0, effect.amount),
            effect.emberBurnEqualsFinalDamage,
            effect.emberBurnOnCritOnly,
            Mathf.Max(1, effect.emberBurnTurns));
    }

    private static void GainFocus(BuffDebuffFlowEffectData effect, CombatActor caster)
    {
        int amount = Mathf.Max(0, effect.amount);
        caster.GainFocus(amount);
        if (amount > 0)
            CombatHitFeedback.Play(caster, CombatHitFeedback.FeedbackKind.Focus);
    }

    private static void GainGuard(BuffDebuffFlowEffectData effect, CombatActor target)
    {
        if (target == null)
            return;

        int amount = Mathf.Max(0, effect.amount);
        target.AddGuard(amount);
        if (amount > 0)
            CombatHitFeedback.Play(target, CombatHitFeedback.FeedbackKind.Guard);
    }

    private static void ApplyStatus(BuffDebuffFlowEffectData effect, CombatActor target)
    {
        if (target == null || target.status == null)
            return;

        int amount = Mathf.Max(0, effect.amount);
        if (amount <= 0 && effect.status != StatusKind.Mark && effect.status != StatusKind.Freeze)
            return;

        switch (effect.status)
        {
            case StatusKind.Burn:
                target.status.ApplyBurn(amount, Mathf.Max(1, effect.durationTurns));
                break;
            case StatusKind.Mark:
                target.status.ApplyMark();
                break;
            case StatusKind.Bleed:
                target.status.ApplyBleed(amount);
                break;
            case StatusKind.Freeze:
                target.status.ApplyFreeze();
                break;
            case StatusKind.Chilled:
                target.status.chilledTurns = Mathf.Max(target.status.chilledTurns, Mathf.Max(1, effect.durationTurns));
                break;
        }
    }
}
