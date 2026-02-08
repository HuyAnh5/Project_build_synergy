using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class SkillExecutor : MonoBehaviour
{
    public float delayBetweenActions = 0.25f;

    [Header("Projectile Safety")]
    public float projectileMaxWaitSeconds = 2.5f;

    [Header("Melee Lunge (DOTween)")]
    public float meleeLungeDistanceX = 1.3f;
    public float meleeLungeTime = 0.10f;
    public float meleeReturnTime = 0.08f;
    public bool lungeIgnoreTimeScale = false;

    public IEnumerator ExecuteSkill(SkillSO skill, CombatActor caster, CombatActor target, int dieValue, bool skipCost = false)
    {
        var rt = SkillRuntime.FromSkill(skill);
        yield return ExecuteSkill(rt, caster, target, dieValue, skipCost);
    }

    // ✅ Back-compat (calls the AoE-capable overload with aoeTargets=null)
    public IEnumerator ExecuteSkill(SkillRuntime rt, CombatActor caster, CombatActor clickedTarget, int dieValue, bool skipCost = false)
    {
        yield return ExecuteSkill(rt, caster, clickedTarget, dieValue, skipCost, aoeTargets: null);
    }

    // ✅ NEW overload: supports named parameter aoeTargets:
    public IEnumerator ExecuteSkill(
        SkillRuntime rt,
        CombatActor caster,
        CombatActor clickedTarget,
        int dieValue,
        bool skipCost = false,
        IReadOnlyList<CombatActor> aoeTargets = null)
    {
        if (rt == null || caster == null) yield break;

        CombatActor primaryTarget = ResolveTarget(rt, caster, clickedTarget);
        if (primaryTarget == null) yield break;

        bool useAoe =
            rt.kind == SkillKind.Attack &&
            (rt.hitAllEnemies || rt.hitAllAllies) &&
            aoeTargets != null &&
            aoeTargets.Count > 0;

        if (!skipCost && rt.focusCost > 0)
        {
            if (!caster.TrySpendFocus(rt.focusCost))
                yield break;
        }

        if (rt.kind == SkillKind.Guard)
        {
            int g = rt.CalculateGuard(dieValue);
            caster.SetGuard(g);
            caster.GainFocus(rt.focusGainOnCast);
            yield return new WaitForSeconds(delayBetweenActions);
            yield break;
        }

        if (rt.kind == SkillKind.Attack)
        {
            // MELEE (visual 1 lần)
            if (rt.range == RangeType.Melee)
            {
                yield return MeleeLungeDOTween(caster, primaryTarget);

                if (useAoe) ApplyAttackToTargets(rt, caster, aoeTargets, dieValue);
                else ApplyAttack(rt, caster, primaryTarget, dieValue);

                caster.GainFocus(rt.focusGainOnCast);
                yield return new WaitForSeconds(delayBetweenActions);
                yield break;
            }

            // RANGED projectile (visual 1 lần)
            if (rt.range == RangeType.Ranged && rt.projectilePrefab != null && caster.firePoint != null)
            {
                bool done = false;
                float elapsed = 0f;

                var proj = Instantiate(rt.projectilePrefab, caster.firePoint.position, caster.firePoint.rotation);
                proj.Launch(primaryTarget.transform, () =>
                {
                    if (useAoe) ApplyAttackToTargets(rt, caster, aoeTargets, dieValue);
                    else ApplyAttack(rt, caster, primaryTarget, dieValue);

                    done = true;

                    // nếu projectile prefab không tự destroy thì destroy ở đây vẫn an toàn
                    if (proj != null) Destroy(proj.gameObject);
                });

                while (!done && elapsed < projectileMaxWaitSeconds)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                // Safety: không bao giờ để treo turn nếu projectile bug
                if (!done)
                {
                    if (proj != null) Destroy(proj.gameObject);

                    if (useAoe) ApplyAttackToTargets(rt, caster, aoeTargets, dieValue);
                    else ApplyAttack(rt, caster, primaryTarget, dieValue);
                }

                caster.GainFocus(rt.focusGainOnCast);
                yield return new WaitForSeconds(delayBetweenActions);
                yield break;
            }

            // fallback (no projectile)
            if (useAoe) ApplyAttackToTargets(rt, caster, aoeTargets, dieValue);
            else ApplyAttack(rt, caster, primaryTarget, dieValue);

            caster.GainFocus(rt.focusGainOnCast);
            yield return new WaitForSeconds(delayBetweenActions);
        }
    }

    private static CombatActor ResolveTarget(SkillRuntime rt, CombatActor caster, CombatActor clicked)
    {
        if (rt.target == TargetRule.Self) return caster;
        return clicked;
    }

    private IEnumerator MeleeLungeDOTween(CombatActor caster, CombatActor target)
    {
        if (caster == null) yield break;

        Transform t = caster.transform;
        float startX = t.position.x;

        float dirX = 1f;
        if (target != null)
        {
            float dx = target.transform.position.x - startX;
            if (!Mathf.Approximately(dx, 0f)) dirX = Mathf.Sign(dx);
        }

        float midX = startX + dirX * meleeLungeDistanceX;

        t.DOKill(false);

        Sequence seq = DOTween.Sequence();
        seq.Append(t.DOMoveX(midX, Mathf.Max(0.01f, meleeLungeTime)).SetEase(Ease.OutQuad));
        seq.Append(t.DOMoveX(startX, Mathf.Max(0.01f, meleeReturnTime)).SetEase(Ease.InQuad));

        if (lungeIgnoreTimeScale) seq.SetUpdate(true);

        yield return seq.WaitForCompletion();

        if (t != null)
        {
            Vector3 p = t.position;
            p.x = startX;
            t.position = p;
        }
    }

    private void ApplyAttackToTargets(SkillRuntime rt, CombatActor caster, IReadOnlyList<CombatActor> targets, int dieValue)
    {
        for (int i = 0; i < targets.Count; i++)
        {
            var t = targets[i];
            if (t == null || t.IsDead) continue;
            ApplyAttack(rt, caster, t, dieValue);
        }
    }

    private void ApplyAttack(SkillRuntime rt, CombatActor caster, CombatActor target, int dieValue)
    {
        if (target == null) return;

        bool hadGuardBeforeHit = target.guardPool > 0;

        var info = new DamageInfo
        {
            group = rt.group,
            element = rt.element,
            bypassGuard = rt.bypassGuard,
            clearsGuard = rt.clearsGuard,
            canUseMarkMultiplier = rt.canUseMarkMultiplier,
            isDamage = false
        };

        int dmg = rt.CalculateDamage(dieValue);

        // Mark multiplier
        if (rt.canUseMarkMultiplier && target.status != null && target.status.marked)
        {
            float mul = (rt.element == ElementType.Lightning) ? 2f : 1.25f;
            dmg = Mathf.RoundToInt(dmg * mul);
        }

        // Sunder bonus
        if (rt.group == DamageGroup.Sunder && rt.sunderBonusIfTargetHasGuard && hadGuardBeforeHit)
        {
            dmg = Mathf.RoundToInt(dmg * Mathf.Max(0f, rt.sunderGuardDamageMultiplier));
        }

        // Burn spender
        if (rt.element == ElementType.Fire && rt.consumesBurn && target.status != null)
        {
            int b = target.status.burnStacks;
            if (b > 0)
            {
                dmg += b * Mathf.Max(0, rt.burnDamagePerStack);
                target.status.burnStacks = 0;
                target.status.burnTurns = 0;
            }
        }

        info.isDamage = (dmg > 0);

        target.TakeDamage(dmg, bypassGuard: info.bypassGuard);
        if (info.clearsGuard) target.guardPool = 0;

        // ✅ Consume + reward focus (Ice phá Freeze -> +1 Focus)
        if (target.status != null)
        {
            int reward = target.status.OnHitByDamageReturnFocusReward(ref info);
            if (reward != 0) caster.GainFocus(reward);
        }

        // Apply statuses AFTER hit
        ApplyStatusesAfterHit(rt, target);
    }

    private static void ApplyStatusesAfterHit(SkillRuntime rt, CombatActor target)
    {
        if (target == null || target.status == null) return;

        if (rt.applyBurn) target.status.ApplyBurn(rt.burnAddStacks, rt.burnRefreshTurns);
        if (rt.applyMark) target.status.ApplyMark();
        if (rt.applyBleed) target.status.ApplyBleed(rt.bleedTurns);

        // AoE freeze: roll riêng từng target vì gọi per-target ở đây
        if (rt.applyFreeze) target.status.TryApplyFreeze(rt.freezeChance);
    }
}
