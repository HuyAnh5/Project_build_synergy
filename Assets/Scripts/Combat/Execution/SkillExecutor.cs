using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using DG.Tweening;

public partial class SkillExecutor : MonoBehaviour
{
    public const float GlobalDelayedSecondaryStep = 0.3f;

    private BattlePartyManager2D _cachedParty;
    private readonly SkillTargetSelectionService _targetSelectionService = new SkillTargetSelectionService();
    public float delayBetweenActions = 0.25f;
    [Header("Projectile Safety")]
    public float projectileMaxWaitSeconds = 2.5f;

    [Header("Melee Lunge (DOTween)")]
    public float meleeLungeDistanceX = 1.3f;
    public float meleeLungeTime = 0.10f;
    public float meleeReturnTime = 0.08f;
    public bool lungeIgnoreTimeScale = false;
    [Min(0f)] public float meleeTargetStopDistance = 0.35f;

    [Header("Damage Popup")]
    public DamagePopupSystem damagePopups;

    private DamagePopupSystem GetPopups()
    {
        if (damagePopups != null) return damagePopups;
        damagePopups = FindObjectOfType<DamagePopupSystem>();
        return damagePopups;
    }

    private TurnManager GetTurnManager()
    {
        TurnManager fromComponent = GetComponent<TurnManager>();
        if (fromComponent != null)
            return fromComponent;

        return FindObjectOfType<TurnManager>(true);
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

    public IEnumerator ExecuteSkill(SkillDamageSO skill, CombatActor caster, CombatActor target, int dieValue, bool skipCost = false, IReadOnlyList<CombatActor> aoeTargets = null, DiceSlotRig castDiceRig = null, int castStart0 = -1, int castSpan = 0, int castPaymentMask = -1)
    {
        var rt = SkillRuntime.FromDamage(skill);
        yield return ExecuteSkill(rt, caster, target, dieValue, skipCost, aoeTargets, castDiceRig, castStart0, castSpan, castPaymentMask);
    }

    // NEW: SkillBuffDebuffSO execution (applies via StatusController)
    public IEnumerator ExecuteSkill(SkillBuffDebuffSO skill, CombatActor caster, CombatActor clickedTarget, int rolledValue, int maxFaceValue, bool skipCost = false, IReadOnlyList<CombatActor> aoeTargets = null, DiceSlotRig castDiceRig = null, int castStart0 = -1, int castSpan = 0, int castPaymentMask = -1)
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

        System.Action applyBuffEffects = () =>
        {
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
        };

        if (ShouldUsePlayerDiceCastAnimation(caster, castDiceRig, castStart0, castSpan))
        {
            PlayerDiceCastAnimator.CastMode mode = ResolveCastMode(caster, clickedTarget, targets);
            TurnManager turnManager = GetTurnManager();
            if (turnManager != null)
                turnManager.SetDiceReorderLocked(true);

            try
            {
                yield return GetPlayerDiceCastAnimator().PlayCast(castDiceRig, castStart0, castSpan, castPaymentMask, caster, clickedTarget, targets, mode, applyBuffEffects);
            }
            finally
            {
                if (turnManager != null)
                    turnManager.SetDiceReorderLocked(false);
            }
        }
        else
        {
            applyBuffEffects();
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
        IReadOnlyList<CombatActor> aoeTargets = null,
        DiceSlotRig castDiceRig = null,
        int castStart0 = -1,
        int castSpan = 0,
        int castPaymentMask = -1)
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

        if (!useAoe && !SkillUsageRequirementUtility.IsTargetRequirementMet(rt, primaryTarget))
            yield break;

        SkillDamageSO sourceSkill = SkillGameplayResolver.GetSourceSkill(rt);
        if (SkillGameplayResolver.CanResolveWithNewPipeline(sourceSkill))
        {
            SkillResolvedResult resolved = SkillGameplayResolver.Resolve(rt, caster, primaryTarget);
            if (resolved == null || !resolved.canCast)
                yield break;
        }

        if (!skipCost && rt.focusCost > 0)
        {
            if (!caster.TrySpendFocus(rt.focusCost))
                yield break;
        }

        if (rt.kind == SkillKind.Guard)
        {
            System.Action applyGuard = () =>
            {
                if (SkillGameplayResolver.CanResolveWithNewPipeline(sourceSkill))
                {
                    SkillAttackResolutionUtility.ApplyResolvedGameplayNonAttack(rt, caster, primaryTarget);
                    caster.GainFocus(rt.focusGainOnCast);
                    if (rt.focusGainOnCast > 0)
                        CombatHitFeedback.Play(caster, CombatHitFeedback.FeedbackKind.Focus);
                    return;
                }

                int baseGuard = rt.CalculateGuard(dieValue);
                if (rt.guardValueMode == BaseEffectValueMode.Flat && rt.guardFlat > 0)
                    baseGuard = SkillOutputValueUtility.AddActionAddedValue(rt.guardFlat, rt);
                baseGuard = ApplyCustomGuardBehavior(rt, caster, baseGuard);

                float pct = 0f;
                var ps = caster.GetComponent<PassiveSystem>();
                if (ps != null) pct = ps.GetGuardGainPercent();

                float mult = 1f + Mathf.Max(-0.99f, pct);
                int scaledGuard = Mathf.FloorToInt(baseGuard * mult);

                caster.AddGuard(scaledGuard);
                if (scaledGuard > 0)
                    CombatHitFeedback.Play(caster, CombatHitFeedback.FeedbackKind.Guard);

                caster.GainFocus(rt.focusGainOnCast);
                if (rt.focusGainOnCast > 0)
                    CombatHitFeedback.Play(caster, CombatHitFeedback.FeedbackKind.Focus);
            };

            if (ShouldUsePlayerDiceCastAnimation(caster, castDiceRig, castStart0, castSpan))
            {
                PlayerDiceCastAnimator.CastMode mode = ResolveCastMode(caster, primaryTarget, null);
                TurnManager turnManager = GetTurnManager();
                if (turnManager != null)
                    turnManager.SetDiceReorderLocked(true);

                try
                {
                    yield return GetPlayerDiceCastAnimator().PlayCast(castDiceRig, castStart0, castSpan, castPaymentMask, caster, primaryTarget, null, mode, applyGuard);
                }
                finally
                {
                    if (turnManager != null)
                        turnManager.SetDiceReorderLocked(false);
                }
            }
            else
            {
                applyGuard();
            }

            yield return new WaitForSeconds(delayBetweenActions);
            yield break;
        }

        if (rt.kind == SkillKind.Attack)
        {
            bool usePlayerDiceCastAnimation = ShouldUsePlayerDiceCastAnimation(caster, castDiceRig, castStart0, castSpan);
            if (usePlayerDiceCastAnimation)
            {
                AttackApplyResult singleResult = default;
                AttackApplyResult aoeResult = default;
                bool followUpCompleted = false;
                bool actionVisualResolved = false;

                System.Action applyAttackAtImpact = () =>
                {
                    if (useAoe)
                    {
                        aoeResult = ApplyAttackToTargets(rt, caster, aoeTargets, dieValue);
                        StartCoroutine(ResolveDelayedAttackFollowUpsAtImpact(rt, caster, primaryTarget, aoeResult, () => followUpCompleted = true));
                    }
                    else
                    {
                        singleResult = ApplyAttack(rt, caster, primaryTarget, dieValue);
                        StartCoroutine(ResolveDelayedAttackFollowUpsAtImpact(rt, caster, primaryTarget, singleResult, () => followUpCompleted = true));
                    }
                };

                IEnumerator ResolveAttackVisualAfterDice()
                {
                    if (rt.range == RangeType.Melee)
                    {
                        yield return MeleeLungeDOTween_HitMoment(caster, primaryTarget, applyAttackAtImpact);
                    }
                    else if (rt.range == RangeType.Ranged && rt.projectilePrefab != null && caster.firePoint != null)
                    {
                        yield return RangedProjectileHitMoment(rt, caster, primaryTarget, applyAttackAtImpact);
                    }
                    else
                    {
                        applyAttackAtImpact();
                    }

                    actionVisualResolved = true;
                }

                PlayerDiceCastAnimator.CastMode mode = ResolveCastMode(caster, primaryTarget, aoeTargets);
                TurnManager turnManager = GetTurnManager();
                if (turnManager != null)
                    turnManager.SetDiceReorderLocked(true);

                try
                {
                    yield return GetPlayerDiceCastAnimator().PlayCast(
                        castDiceRig,
                        castStart0,
                        castSpan,
                        castPaymentMask,
                        caster,
                        primaryTarget,
                        aoeTargets,
                        mode,
                        () => StartCoroutine(ResolveAttackVisualAfterDice()));
                }
                finally
                {
                    if (turnManager != null)
                        turnManager.SetDiceReorderLocked(false);
                }

                while (!actionVisualResolved || !followUpCompleted)
                    yield return null;

                caster.GainFocus(rt.focusGainOnCast);
                yield return new WaitForSeconds(delayBetweenActions);
                yield break;
            }

            // MELEE (visual 1 lần)
            if (rt.range == RangeType.Melee)
            {
                AttackApplyResult singleResult = default;
                AttackApplyResult aoeResult = default;

                yield return MeleeLungeDOTween_HitMoment(caster, primaryTarget, () =>
                {
                    // Apply đúng 1 lần tại hit moment
                    if (useAoe)
                    {
                        aoeResult = ApplyAttackToTargets(rt, caster, aoeTargets, dieValue);
                    }
                    else
                    {
                        singleResult = ApplyAttack(rt, caster, primaryTarget, dieValue);
                    }
                });

                if (useAoe)
                    yield return ResolveDelayedAttackFollowUps(rt, caster, primaryTarget, aoeResult);
                else
                    yield return ResolveDelayedAttackFollowUps(rt, caster, primaryTarget, singleResult);

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
                AttackApplyResult aoeResult = default;

                var proj = Instantiate(rt.projectilePrefab, caster.firePoint.position, caster.firePoint.rotation);
                proj.Launch(primaryTarget.transform, () =>
                {
                    if (useAoe)
                    {
                        aoeResult = ApplyAttackToTargets(rt, caster, aoeTargets, dieValue);
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

                    if (useAoe) aoeResult = ApplyAttackToTargets(rt, caster, aoeTargets, dieValue);
                    else singleResult = ApplyAttack(rt, caster, primaryTarget, dieValue);
                }

                if (useAoe)
                    yield return ResolveDelayedAttackFollowUps(rt, caster, primaryTarget, aoeResult);
                else
                    yield return ResolveDelayedAttackFollowUps(rt, caster, primaryTarget, singleResult);

                caster.GainFocus(rt.focusGainOnCast);
                yield return new WaitForSeconds(delayBetweenActions);
                yield break;
            }

            // fallback (no projectile)
            if (useAoe)
                yield return ResolveDelayedAttackFollowUps(rt, caster, primaryTarget, ApplyAttackToTargets(rt, caster, aoeTargets, dieValue));
            else
                yield return ResolveDelayedAttackFollowUps(rt, caster, primaryTarget, ApplyAttack(rt, caster, primaryTarget, dieValue));

            caster.GainFocus(rt.focusGainOnCast);
            yield return new WaitForSeconds(delayBetweenActions);
        }
    }

    private IEnumerator RangedProjectileHitMoment(SkillRuntime rt, CombatActor caster, CombatActor primaryTarget, System.Action onHit)
    {
        if (rt == null || caster == null)
            yield break;

        if (rt.projectilePrefab == null || caster.firePoint == null || primaryTarget == null)
        {
            onHit?.Invoke();
            yield break;
        }

        bool done = false;
        float elapsed = 0f;

        Projectile2D proj = Instantiate(rt.projectilePrefab, caster.firePoint.position, caster.firePoint.rotation);
        proj.Launch(primaryTarget.transform, () =>
        {
            onHit?.Invoke();
            done = true;

            if (proj != null)
                Destroy(proj.gameObject);
        });

        while (!done && elapsed < projectileMaxWaitSeconds)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!done)
        {
            if (proj != null)
                Destroy(proj.gameObject);

            onHit?.Invoke();
        }
    }

    private IEnumerator MeleeLungeDOTween_HitMoment(CombatActor caster, CombatActor target, System.Action onHit)
    {
        if (caster == null) yield break;

        Transform t = caster.transform;
        Vector3 startPosition = t.position;
        Vector3 midPosition = startPosition;

        if (target != null)
        {
            Vector3 toTarget = target.transform.position - startPosition;
            float distanceToTarget = toTarget.magnitude;
            if (distanceToTarget > 0.001f)
            {
                Vector3 direction = toTarget / distanceToTarget;
                float travelDistance = Mathf.Min(
                    Mathf.Max(0f, meleeLungeDistanceX),
                    Mathf.Max(0f, distanceToTarget - meleeTargetStopDistance));
                midPosition = startPosition + direction * travelDistance;
            }
        }
        else
        {
            midPosition.x += Mathf.Max(0f, meleeLungeDistanceX);
        }

        t.DOKill(false);

        bool hitFired = false;

        Sequence seq = DOTween.Sequence();

        // đi tới apex
        seq.Append(t.DOMove(midPosition, Mathf.Max(0.01f, meleeLungeTime)).SetEase(Ease.OutQuad));

        // HIT MOMENT: ngay khi tới apex
        seq.AppendCallback(() =>
        {
            if (hitFired) return;
            hitFired = true;
            onHit?.Invoke();
        });

        // lùi về
        seq.Append(t.DOMove(startPosition, Mathf.Max(0.01f, meleeReturnTime)).SetEase(Ease.InQuad));

        if (lungeIgnoreTimeScale) seq.SetUpdate(true);

        yield return seq.WaitForCompletion();

        if (t != null)
            t.position = startPosition;
    }
}
