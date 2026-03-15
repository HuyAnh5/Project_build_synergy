using System.Collections.Generic;
using UnityEngine;

internal static class SkillAttackResolutionUtility
{
    public static int ApplyAttackToTargets(
        SkillRuntime rt,
        CombatActor caster,
        IReadOnlyList<CombatActor> targets,
        int dieValue,
        DamagePopupSystem popups,
        MonoBehaviour context,
        System.Func<SkillRuntime, CombatActor, CombatActor, int, SkillExecutor.AttackPreview> buildAttackPreview,
        out int lightningShockDamage)
    {
        int totalShockProcCount = 0;
        lightningShockDamage = 0;

        if (targets == null)
            return 0;

        for (int i = 0; i < targets.Count; i++)
        {
            CombatActor t = targets[i];
            if (t == null || t.IsDead)
                continue;

            SkillExecutor.AttackApplyResult result = ApplyAttack(rt, caster, t, dieValue, popups, context, buildAttackPreview);
            totalShockProcCount += result.lightningShockProcCount;
            if (result.lightningShockDamage > 0)
                lightningShockDamage = result.lightningShockDamage;
        }

        return totalShockProcCount;
    }

    public static SkillExecutor.AttackApplyResult ApplyAttack(
        SkillRuntime rt,
        CombatActor caster,
        CombatActor target,
        int dieValue,
        DamagePopupSystem popups,
        MonoBehaviour context,
        System.Func<SkillRuntime, CombatActor, CombatActor, int, SkillExecutor.AttackPreview> buildAttackPreview)
    {
        if (rt == null || target == null || buildAttackPreview == null)
            return default;

        PassiveSystem ps = caster != null ? caster.GetComponent<PassiveSystem>() : null;
        bool hadGuardBeforeHit = target.guardPool > 0;
        SkillExecutor.AttackPreview preview = buildAttackPreview(rt, caster, target, dieValue);

        var info = new DamageInfo
        {
            group = rt.group,
            element = rt.element,
            bypassGuard = rt.bypassGuard,
            clearsGuard = rt.clearsGuard,
            canUseMarkMultiplier = rt.canUseMarkMultiplier,
            isDamage = preview.finalDamage > 0
        };

        bool triggerMarkPayoff = AttackPreviewCalculator.CanUseMarkPayoff(rt, target);
        int lightningShockDamage = 0;
        if (triggerMarkPayoff && rt.element == ElementType.Lightning)
        {
            float shockMul = 1f;
            if (ps != null)
                shockMul += Mathf.Max(0f, ps.GetLightningVsMarkMultiplierAdd());
            lightningShockDamage = Mathf.FloorToInt(4 * shockMul);
        }

        if (rt.element == ElementType.Fire && rt.consumesBurn && target.status != null && target.status.burnStacks > 0)
        {
            target.status.burnStacks = 0;
            target.status.burnTurns = 0;
        }

        if (Debug.isDebugBuild)
        {
            bool hasPS = ps != null;
            Debug.Log($"[EXEC] ApplyAttack rt={rt.kind}/{rt.group}/{rt.element} die={dieValue} base={preview.baseDamage} bonus={preview.bonusDamage} finalDmg={preview.finalDamage} hadGuard={hadGuardBeforeHit} hasPassiveSystem={hasPS}", context);
        }

        CombatActor.DamageResult dmgResult = target.TakeDamageDetailed(preview.finalDamage, bypassGuard: info.bypassGuard);

        if (info.isDamage && rt.element == ElementType.Ice && target.status != null && caster != null)
        {
            bool isFrozen = target.status.frozen;
            bool isChilled = target.status.chilledTurns > 0;
            if (isFrozen || isChilled)
            {
                caster.AddGuard(3);
                caster.GainFocus(1);
            }
        }

        if (info.clearsGuard)
            target.guardPool = 0;

        if (target.status != null && caster != null)
        {
            int reward = target.status.OnHitByDamageReturnFocusReward(ref info);
            if (reward > 0 && ps != null)
                reward += ps.GetFreezeBreakFocusBonusAdd();
            if (reward != 0)
                caster.GainFocus(reward);
        }

        if (triggerMarkPayoff && target.status != null)
            target.status.marked = false;

        if (preview.consumesStagger && target.status != null)
            target.status.ClearStagger();

        if (dmgResult.guardBroken && target.status != null)
            target.status.ApplyStagger();

        ApplyStatusesAfterHit(rt, target);

        if (popups != null)
            popups.SpawnDamageSplit(caster, target, dmgResult.blocked, dmgResult.hpLost);

        return new SkillExecutor.AttackApplyResult
        {
            damageResult = dmgResult,
            lightningShockProcCount = lightningShockDamage > 0 ? 1 : 0,
            lightningShockDamage = lightningShockDamage,
            consumedStagger = preview.consumesStagger
        };
    }

    public static void ApplyStatusesAfterHit(SkillRuntime rt, CombatActor target)
    {
        if (target == null || target.status == null || rt == null)
            return;

        if (rt.applyBurn) target.status.ApplyBurn(rt.burnAddStacks, rt.burnRefreshTurns);
        if (rt.applyMark) target.status.ApplyMark();
        if (rt.applyBleed) target.status.ApplyBleed(rt.bleedTurns);
        if (rt.applyFreeze) target.status.TryApplyFreeze(rt.freezeChance);
    }
}
