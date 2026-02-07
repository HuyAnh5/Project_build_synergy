using System.Collections;
using UnityEngine;
using DG.Tweening;

public class SkillExecutor : MonoBehaviour
{
    public float delayBetweenActions = 0.25f;

    [Header("Melee Lunge (DOTween)")]
    [Tooltip("Khoảng nhích theo trục X. Nếu bạn muốn kiểu 120-150 'pixel' thì set 120/150 tại đây (tuỳ scale world).")]
    public float meleeLungeDistanceX = 1.3f;   // mặc định theo unit; bạn chỉnh 120-150 nếu world của bạn dùng pixel-unit
    public float meleeLungeTime = 0.10f;
    public float meleeReturnTime = 0.08f;
    public bool lungeIgnoreTimeScale = false;

    /// <summary>
    /// Back-compat: execute using SkillSO base values.
    /// </summary>
    public IEnumerator ExecuteSkill(SkillSO skill, CombatActor caster, CombatActor target, int dieValue, bool skipCost = false)
    {
        var rt = SkillRuntime.FromSkill(skill);
        yield return ExecuteSkill(rt, caster, target, dieValue, skipCost);
    }

    /// <summary>
    /// Execute using a resolved SkillRuntime (after condition overrides).
    /// </summary>
    public IEnumerator ExecuteSkill(SkillRuntime rt, CombatActor caster, CombatActor clickedTarget, int dieValue, bool skipCost = false)
    {
        if (rt == null || caster == null) yield break;

        // Resolve target rule
        CombatActor target = ResolveTarget(rt, caster, clickedTarget);
        if (target == null) yield break;

        // Spend focus (planning usually reserves => skipCost true)
        if (!skipCost && rt.focusCost > 0)
        {
            if (!caster.TrySpendFocus(rt.focusCost))
                yield break;
        }

        // Guard
        if (rt.kind == SkillKind.Guard)
        {
            int g = rt.CalculateGuard(dieValue);
            caster.SetGuard(g);
            caster.GainFocus(rt.focusGainOnCast);
            yield return new WaitForSeconds(delayBetweenActions);
            yield break;
        }

        // Attack
        if (rt.kind == SkillKind.Attack)
        {
            // MELEE: nhích lên rồi mới hit
            if (rt.range == RangeType.Melee)
            {
                yield return MeleeLungeDOTween(caster, target);
                ApplyAttack(rt, caster, target, dieValue);

                caster.GainFocus(rt.focusGainOnCast);
                yield return new WaitForSeconds(delayBetweenActions);
                yield break;
            }

            // RANGED: projectile nếu có
            if (rt.range == RangeType.Ranged && rt.projectilePrefab != null && caster.firePoint != null)
            {
                bool done = false;

                var proj = Instantiate(rt.projectilePrefab, caster.firePoint.position, caster.firePoint.rotation);
                proj.Launch(target.transform, () =>
                {
                    ApplyAttack(rt, caster, target, dieValue);
                    done = true;

                    // Nếu projectile không tự destroy, destroy ở đây để tránh kẹt.
                    if (proj != null) Destroy(proj.gameObject);
                });

                while (!done) yield return null;

                caster.GainFocus(rt.focusGainOnCast);
                yield return new WaitForSeconds(delayBetweenActions);
                yield break;
            }

            // Fallback: ranged nhưng không có projectile
            ApplyAttack(rt, caster, target, dieValue);
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

        // tránh stacking tween
        t.DOKill(false);

        Sequence seq = DOTween.Sequence();
        seq.Append(t.DOMoveX(midX, Mathf.Max(0.01f, meleeLungeTime)).SetEase(Ease.OutQuad));
        seq.Append(t.DOMoveX(startX, Mathf.Max(0.01f, meleeReturnTime)).SetEase(Ease.InQuad));

        if (lungeIgnoreTimeScale) seq.SetUpdate(true);

        yield return seq.WaitForCompletion();

        // safety snap
        if (t != null)
        {
            Vector3 p = t.position;
            p.x = startX;
            t.position = p;
        }
    }

    private void ApplyAttack(SkillRuntime rt, CombatActor caster, CombatActor target, int dieValue)
    {
        if (target == null) return;

        bool hadGuardBeforeHit = target.guardPool > 0;

        // DamageInfo
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

        // Mark multiplier (before damage)
        if (rt.canUseMarkMultiplier && target.status != null && target.status.marked)
        {
            // Mark: all hits x1.25, Lightning x2
            float mul = (rt.element == ElementType.Lightning) ? 2f : 1.25f;
            dmg = Mathf.RoundToInt(dmg * mul);
        }

        // Sunder guard bonus
        if (rt.group == DamageGroup.Sunder && rt.sunderBonusIfTargetHasGuard && hadGuardBeforeHit)
        {
            dmg = Mathf.RoundToInt(dmg * Mathf.Max(0f, rt.sunderGuardDamageMultiplier));
        }

        // Burn spender: consume existing burn BEFORE this hit is processed.
        // (Important: any burn applied by this skill will be applied AFTER the hit,
        // so it won't be consumed by the same fire hit.)
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

        // Deal damage
        target.TakeDamage(dmg, bypassGuard: info.bypassGuard);

        if (info.clearsGuard) target.guardPool = 0;

        // Consume-on-hit logic (Mark/Burn/Freeze consumption rules)
        if (target.status != null)
        {
            target.status.OnHitByDamage(ref info);
        }

        // Apply statuses AFTER hit so that they are NOT consumed by this same hit.
        ApplyStatusesAfterHit(rt, caster, target);
    }

    private static void ApplyStatusesAfterHit(SkillRuntime rt, CombatActor caster, CombatActor target)
    {
        if (target == null || target.status == null) return;

        // Apply Burn
        if (rt.applyBurn)
            target.status.ApplyBurn(rt.burnAddStacks, rt.burnRefreshTurns);

        // Apply Mark
        if (rt.applyMark)
            target.status.ApplyMark();

        // Apply Bleed
        if (rt.applyBleed)
            target.status.ApplyBleed(rt.bleedTurns);

        // Apply Freeze
        if (rt.applyFreeze)
            target.status.TryApplyFreeze(rt.freezeChance);
    }
}
