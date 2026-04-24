using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class SkillExecutor : MonoBehaviour
{
    private BattlePartyManager2D _cachedParty;
    private readonly SkillTargetSelectionService _targetSelectionService = new SkillTargetSelectionService();

    internal struct AttackApplyResult
    {
        public CombatActor.DamageResult damageResult;
        public int lightningShockProcCount;
        public int lightningShockDamage;
        public bool consumedStagger;
    }

    [System.Serializable]
    public struct AttackPreview
    {
        public int effectiveDieValue;
        public int baseDamage;
        public int bonusDamage;
        public int finalDamage;
        public bool canDealDamage;
        public bool consumesStagger;
    }

    public float delayBetweenActions = 0.25f;

    [Header("Projectile Safety")]
    public float projectileMaxWaitSeconds = 2.5f;

    [Header("Melee Lunge (DOTween)")]
    public float meleeLungeDistanceX = 1.3f;
    public float meleeLungeTime = 0.10f;
    public float meleeReturnTime = 0.08f;
    public bool lungeIgnoreTimeScale = false;

    [Header("Damage Popup")]
    public DamagePopupSystem damagePopups;

    [Header("Lightning Mark")]
    public float lightningMarkShockInterval = 0.2f;

    private DamagePopupSystem GetPopups()
    {
        if (damagePopups != null) return damagePopups;
        damagePopups = FindObjectOfType<DamagePopupSystem>();
        return damagePopups;
    }

    private BattlePartyManager2D GetParty()
    {
        if (_cachedParty != null)
            return _cachedParty;

        _cachedParty = FindObjectOfType<BattlePartyManager2D>(true);
        return _cachedParty;
    }

    public IEnumerator ExecuteSkill(SkillDamageSO skill, CombatActor caster, CombatActor target, int dieValue, bool skipCost = false)
    {
        var rt = SkillRuntime.FromDamage(skill);
        yield return ExecuteSkill(rt, caster, target, dieValue, skipCost);
    }

    public IEnumerator ExecuteSkill(SkillDamageSO skill, CombatActor caster, CombatActor target, int dieValue, bool skipCost = false, IReadOnlyList<CombatActor> aoeTargets = null)
    {
        var rt = SkillRuntime.FromDamage(skill);
        yield return ExecuteSkill(rt, caster, target, dieValue, skipCost, aoeTargets);
    }

    // NEW: SkillBuffDebuffSO execution (applies via StatusController)
    public IEnumerator ExecuteSkill(SkillBuffDebuffSO skill, CombatActor caster, CombatActor clickedTarget, int rolledValue, int maxFaceValue, bool skipCost = false, IReadOnlyList<CombatActor> aoeTargets = null)
    {
        if (skill == null || caster == null) yield break;

        Debug.Log($"[EXEC] BuffDebuff cast={skill.name} caster={caster.name} clicked={(clickedTarget ? clickedTarget.name : "NULL")} rolled={rolledValue} maxFace={maxFaceValue} skipCost={skipCost} focusCost={skill.focusCost} applyDelay={skill.applyDelayTurns} effects={(skill.effects != null ? skill.effects.Count : 0)} applyAilment={skill.applyAilment}", this);

        if (!skipCost && skill.focusCost > 0)
        {
            int before = caster.focus;
            if (!caster.TrySpendFocus(skill.focusCost))
            {
                Debug.LogWarning($"[EXEC] SpendFocus FAILED ({before} < {skill.focusCost}) -> abort", this);
                yield break;
            }
            Debug.Log($"[EXEC] SpendFocus OK: {before}->{caster.focus}", this);
        }

        SkillTargetSelection selection = _targetSelectionService.SelectExecutionTargets(skill.target, caster, clickedTarget, aoeTargets);
        IReadOnlyList<CombatActor> targets = selection.Targets;

        Debug.Log($"[EXEC] Targets resolved count={targets.Count}", this);

        for (int i = 0; i < targets.Count; i++)
        {
            var t = targets[i];
            if (t == null || t.IsDead) continue;

            int hpBefore = t.hp;
            int focusBefore = t.focus;

            if (t.status != null)
            {
                t.status.ApplyBuffDebuffSkill(skill, caster, rolledValue, maxFaceValue);
                bool hasAil = t.status.HasAilment(out var at, out var left);
                Debug.Log($"[EXEC] Apply -> target={t.name} hp:{hpBefore}->{t.hp} focus:{focusBefore}->{t.focus} hasAilment={hasAil} {(hasAil ? ($"type={at} left={left}") : "")}", this);
            }
            else
            {
                Debug.LogWarning($"[EXEC] target={t.name} has NO StatusController", this);
            }
        }

        if (skill.focusGainOnCast != 0)
        {
            int before = caster.focus;
            caster.GainFocus(skill.focusGainOnCast);
            Debug.Log($"[EXEC] focusGainOnCast {skill.focusGainOnCast}: {before}->{caster.focus}", this);
        }

        yield return new WaitForSeconds(delayBetweenActions);
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

        bool wantsAoe =
            rt.kind == SkillKind.Attack &&
            ((rt.useV2Targeting && SkillTargetRuleUtility.IsMultiTarget(rt.targetRuleV2))
             || (!rt.useV2Targeting && (rt.hitAllEnemies || rt.hitAllAllies)));

        bool useAoe = wantsAoe && aoeTargets != null && aoeTargets.Count > 0;

        CombatActor primaryTarget = SkillTargetResolver.ResolveTarget(rt, caster, clickedTarget, useAoe ? aoeTargets : null);
        // For AoE skills, we can proceed even without a clicked target, as long as we have aoeTargets.
        if (primaryTarget == null && !(useAoe && aoeTargets != null && aoeTargets.Count > 0)) yield break;

        if (!skipCost && rt.focusCost > 0)
        {
            if (!caster.TrySpendFocus(rt.focusCost))
                yield break;
        }

        if (rt.kind == SkillKind.Guard)
        {
            int baseGuard = rt.CalculateGuard(dieValue);
            if (rt.guardValueMode == BaseEffectValueMode.Flat && rt.guardFlat > 0)
                baseGuard = SkillOutputValueUtility.AddActionAddedValue(rt.guardFlat, rt);
            baseGuard = ApplyCustomGuardBehavior(rt, caster, baseGuard);

            // apply GuardGainPercent passive
            float pct = 0f;
            var ps = caster.GetComponent<PassiveSystem>();
            if (ps != null) pct = ps.GetGuardGainPercent();

            float mult = 1f + Mathf.Max(-0.99f, pct);
            int scaledGuard = Mathf.FloorToInt(baseGuard * mult);

            caster.AddGuard(scaledGuard);
            caster.GainFocus(rt.focusGainOnCast);

            yield return new WaitForSeconds(delayBetweenActions);
            yield break;
        }

        if (rt.kind == SkillKind.Attack)
        {
            // MELEE (visual 1 lần)
            if (rt.range == RangeType.Melee)
            {
                AttackApplyResult singleResult = default;
                int aoeShockProcCount = 0;
                int aoeShockDamage = 0;

                yield return MeleeLungeDOTween_HitMoment(caster, primaryTarget, () =>
                {
                    // Apply đúng 1 lần tại hit moment
                    if (useAoe)
                    {
                        aoeShockProcCount = ApplyAttackToTargets(rt, caster, aoeTargets, dieValue, out aoeShockDamage);
                    }
                    else
                    {
                        singleResult = ApplyAttack(rt, caster, primaryTarget, dieValue);
                    }
                });

                if (useAoe)
                {
                    if (aoeShockProcCount > 0 && aoeShockDamage > 0)
                        yield return ApplyLightningMarkShockSequence(caster, aoeShockDamage, aoeShockProcCount);
                }
                else if (singleResult.lightningShockProcCount > 0 && singleResult.lightningShockDamage > 0)
                {
                    yield return ApplyLightningMarkShockSequence(caster, singleResult.lightningShockDamage, singleResult.lightningShockProcCount);
                }

                caster.GainFocus(rt.focusGainOnCast);
                yield return new WaitForSeconds(delayBetweenActions);
                yield break;
            }

            // RANGED projectile (visual 1 lần)
            if (rt.range == RangeType.Ranged && rt.projectilePrefab != null && caster.firePoint != null)
            {
                bool done = false;
                float elapsed = 0f;
                AttackApplyResult singleResult = default;
                int aoeShockProcCount = 0;
                int aoeShockDamage = 0;

                var proj = Instantiate(rt.projectilePrefab, caster.firePoint.position, caster.firePoint.rotation);
                proj.Launch(primaryTarget.transform, () =>
                {
                    if (useAoe)
                    {
                        aoeShockProcCount = ApplyAttackToTargets(rt, caster, aoeTargets, dieValue, out aoeShockDamage);
                    }
                    else
                    {
                        singleResult = ApplyAttack(rt, caster, primaryTarget, dieValue);
                    }

                    done = true;

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

                    if (useAoe) aoeShockProcCount = ApplyAttackToTargets(rt, caster, aoeTargets, dieValue, out aoeShockDamage);
                    else singleResult = ApplyAttack(rt, caster, primaryTarget, dieValue);
                }

                if (useAoe)
                {
                    if (aoeShockProcCount > 0 && aoeShockDamage > 0)
                        yield return ApplyLightningMarkShockSequence(caster, aoeShockDamage, aoeShockProcCount);
                }
                else if (singleResult.lightningShockProcCount > 0 && singleResult.lightningShockDamage > 0)
                {
                    yield return ApplyLightningMarkShockSequence(caster, singleResult.lightningShockDamage, singleResult.lightningShockProcCount);
                }

                caster.GainFocus(rt.focusGainOnCast);
                yield return new WaitForSeconds(delayBetweenActions);
                yield break;
            }

            // fallback (no projectile)
            if (useAoe)
            {
                int aoeShockProcCount = ApplyAttackToTargets(rt, caster, aoeTargets, dieValue, out int aoeShockDamage);
                if (aoeShockProcCount > 0 && aoeShockDamage > 0)
                    yield return ApplyLightningMarkShockSequence(caster, aoeShockDamage, aoeShockProcCount);
            }
            else
            {
                AttackApplyResult singleResult = ApplyAttack(rt, caster, primaryTarget, dieValue);
                if (singleResult.lightningShockProcCount > 0 && singleResult.lightningShockDamage > 0)
                    yield return ApplyLightningMarkShockSequence(caster, singleResult.lightningShockDamage, singleResult.lightningShockProcCount);
            }

            caster.GainFocus(rt.focusGainOnCast);
            yield return new WaitForSeconds(delayBetweenActions);
        }
    }

    private IEnumerator MeleeLungeDOTween_HitMoment(CombatActor caster, CombatActor target, System.Action onHit)
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

        bool hitFired = false;

        Sequence seq = DOTween.Sequence();

        // đi tới apex
        seq.Append(t.DOMoveX(midX, Mathf.Max(0.01f, meleeLungeTime)).SetEase(Ease.OutQuad));

        // HIT MOMENT: ngay khi tới apex
        seq.AppendCallback(() =>
        {
            if (hitFired) return;
            hitFired = true;
            onHit?.Invoke();
        });

        // lùi về
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

    public int PreviewDamage(SkillRuntime rt, CombatActor caster, CombatActor target, int dieValue)
    {
        return BuildAttackPreview(rt, caster, target, dieValue).finalDamage;
    }

    public int PreviewDamage(SkillDamageSO skill, CombatActor caster, CombatActor target, int dieValue)
    {
        if (skill == null) return 0;
        return PreviewDamage(SkillRuntime.FromDamage(skill), caster, target, dieValue);
    }

    public AttackPreview BuildAttackPreview(SkillRuntime rt, CombatActor caster, CombatActor target, int dieValue)
    {
        return AttackPreviewCalculator.BuildAttackPreview(rt, caster, target, dieValue);
    }

    private int ApplyAttackToTargets(SkillRuntime rt, CombatActor caster, IReadOnlyList<CombatActor> targets, int dieValue, out int lightningShockDamage)
        => SkillAttackResolutionUtility.ApplyAttackToTargets(rt, caster, targets, dieValue, GetPopups(), this, BuildAttackPreview, out lightningShockDamage);

    private AttackApplyResult ApplyAttack(SkillRuntime rt, CombatActor caster, CombatActor target, int dieValue)
        => SkillAttackResolutionUtility.ApplyAttack(rt, caster, target, dieValue, GetPopups(), this, BuildAttackPreview);

    private IEnumerator ApplyLightningMarkShockSequence(CombatActor caster, int damage, int procCount)
    {
        if (caster == null || damage <= 0 || procCount <= 0) yield break;

        for (int i = 0; i < procCount; i++)
        {
            ApplyLightningMarkShock(caster, damage);
            if (i < procCount - 1 && lightningMarkShockInterval > 0f)
                yield return new WaitForSeconds(lightningMarkShockInterval);
        }
    }

    private void ApplyLightningMarkShock(CombatActor caster, int damage)
    {
        if (caster == null || damage <= 0) return;

        var popups = GetPopups();
        BattlePartyManager2D party = GetParty();
        if (party == null)
            return;

        IReadOnlyList<CombatActor> targets = caster.team == CombatActor.TeamSide.Enemy
            ? party.GetAliveAllies(includePlayer: true)
            : party.Enemies;

        if (targets == null)
            return;

        for (int i = 0; i < targets.Count; i++)
        {
            var t = targets[i];
            if (t == null || t.IsDead) continue;
            if (t == caster) continue;
            if (t.team == caster.team) continue;

            var res = t.TakeDamageDetailed(damage, bypassGuard: false);
            if (popups != null)
                popups.SpawnDamageSplit(caster, t, res.blocked, res.hpLost);
        }
    }

    private int ApplyCustomGuardBehavior(SkillRuntime rt, CombatActor caster, int baseGuard)
    {
        if (rt == null || caster == null)
            return baseGuard;

        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, BleedDamageBehaviorId.BloodWard))
            return SkillOutputValueUtility.AddActionAddedValue(
                Mathf.Max(0, SkillBehaviorRuntimeUtility.CountBleedOnEnemyTeam(caster)),
                rt);

        if (SkillBehaviorRuntimeUtility.IsBehavior(rt, IceDamageBehaviorId.ColdSnap))
            return SkillOutputValueUtility.AddActionAddedValue(
                Mathf.Max(0, SkillBehaviorRuntimeUtility.GetHighestBaseValue(rt)),
                rt);

        return baseGuard;
    }

}
