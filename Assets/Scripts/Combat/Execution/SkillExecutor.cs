using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using DG.Tweening;

public class SkillExecutor : MonoBehaviour
{
    public const float GlobalDelayedSecondaryStep = 0.3f;

    private BattlePartyManager2D _cachedParty;
    private readonly SkillTargetSelectionService _targetSelectionService = new SkillTargetSelectionService();
    private readonly Dictionary<DiceSpinnerGeneric, DiceDraggableUI> _diceUiBySpinner = new Dictionary<DiceSpinnerGeneric, DiceDraggableUI>();

    internal struct AttackApplyResult
    {
        public CombatActor.DamageResult damageResult;
        public int lightningShockProcCount;
        public int lightningShockDamage;
        public bool consumedStagger;
        public bool hadPrimaryDamageStep;
        public int delayedBurnConsumeDamage;
        public List<ResolvedEffect> delayedFollowUpEffects;

        public bool HasDelayedFollowUpEffects =>
            delayedFollowUpEffects != null && delayedFollowUpEffects.Count > 0;
    }

    [System.Serializable]
    public struct AttackPreview
    {
        public int effectiveDieValue;
        public int baseDamage;
        public int bonusDamage;
        public int finalDamage;
        public int primaryDamage;
        public int burnConsumeDamage;
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

    [Header("Player Dice Cast")]
    public PlayerDiceCastAnimator playerDiceCastAnimator;

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

    public IEnumerator ExecuteSkill(SkillDamageSO skill, CombatActor caster, CombatActor target, int dieValue, bool skipCost = false, IReadOnlyList<CombatActor> aoeTargets = null, DiceSlotRig castDiceRig = null, int castStart0 = -1, int castSpan = 0)
    {
        var rt = SkillRuntime.FromDamage(skill);
        yield return ExecuteSkill(rt, caster, target, dieValue, skipCost, aoeTargets, castDiceRig, castStart0, castSpan);
    }

    // NEW: SkillBuffDebuffSO execution (applies via StatusController)
    public IEnumerator ExecuteSkill(SkillBuffDebuffSO skill, CombatActor caster, CombatActor clickedTarget, int rolledValue, int maxFaceValue, bool skipCost = false, IReadOnlyList<CombatActor> aoeTargets = null, DiceSlotRig castDiceRig = null, int castStart0 = -1, int castSpan = 0)
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
            yield return GetPlayerDiceCastAnimator().PlayCast(castDiceRig, castStart0, castSpan, caster, clickedTarget, targets, mode, applyBuffEffects);
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
        int castSpan = 0)
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
                yield return GetPlayerDiceCastAnimator().PlayCast(castDiceRig, castStart0, castSpan, caster, primaryTarget, null, mode, applyGuard);
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
            if (ShouldUsePlayerDiceCastAnimation(caster, castDiceRig, castStart0, castSpan))
            {
                AttackApplyResult singleResult = default;
                AttackApplyResult aoeResult = default;
                bool impactResolved = false;
                bool followUpCompleted = false;

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

                    impactResolved = true;
                };

                PlayerDiceCastAnimator.CastMode mode = ResolveCastMode(caster, primaryTarget, aoeTargets);
                yield return GetPlayerDiceCastAnimator().PlayCast(castDiceRig, castStart0, castSpan, caster, primaryTarget, aoeTargets, mode, applyAttackAtImpact);

                if (impactResolved)
                {
                    while (!followUpCompleted)
                        yield return null;
                }
                else if (useAoe)
                {
                    aoeResult = ApplyAttackToTargets(rt, caster, aoeTargets, dieValue);
                    yield return ResolveDelayedAttackFollowUps(rt, caster, primaryTarget, aoeResult);
                }
                else
                {
                    singleResult = ApplyAttack(rt, caster, primaryTarget, dieValue);
                    yield return ResolveDelayedAttackFollowUps(rt, caster, primaryTarget, singleResult);
                }

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

    private AttackApplyResult ApplyAttackToTargets(SkillRuntime rt, CombatActor caster, IReadOnlyList<CombatActor> targets, int dieValue)
        => SkillAttackResolutionUtility.ApplyAttackToTargets(rt, caster, targets, dieValue, GetPopups(), this, BuildAttackPreview);

    private AttackApplyResult ApplyAttack(SkillRuntime rt, CombatActor caster, CombatActor target, int dieValue)
        => SkillAttackResolutionUtility.ApplyAttack(rt, caster, target, dieValue, GetPopups(), this, BuildAttackPreview);

    private IEnumerator ResolveDelayedAttackFollowUpsAtImpact(
        SkillRuntime rt,
        CombatActor caster,
        CombatActor primaryTarget,
        AttackApplyResult result,
        Action onComplete)
    {
        yield return ResolveDelayedAttackFollowUps(rt, caster, primaryTarget, result);
        onComplete?.Invoke();
    }

    private IEnumerator ResolveDelayedAttackFollowUps(SkillRuntime rt, CombatActor caster, CombatActor primaryTarget, AttackApplyResult result)
    {
        if (result.hadPrimaryDamageStep && result.HasDelayedFollowUpEffects && GlobalDelayedSecondaryStep > 0f)
            yield return new WaitForSeconds(GlobalDelayedSecondaryStep);

        if (result.HasDelayedFollowUpEffects)
            SkillAttackResolutionUtility.ApplyResolvedGameplayFollowUpEffects(rt, caster, primaryTarget, result.delayedFollowUpEffects, GetPopups());

        if (result.hadPrimaryDamageStep && result.delayedBurnConsumeDamage > 0 && GlobalDelayedSecondaryStep > 0f)
            yield return new WaitForSeconds(GlobalDelayedSecondaryStep);

        if (result.delayedBurnConsumeDamage > 0)
            ApplyDelayedBurnConsumeDamage(caster, primaryTarget, result.delayedBurnConsumeDamage);

        if (result.lightningShockProcCount > 0 && result.lightningShockDamage > 0)
        {
            if (result.hadPrimaryDamageStep && GlobalDelayedSecondaryStep > 0f)
                yield return new WaitForSeconds(GlobalDelayedSecondaryStep);

            yield return ApplyLightningMarkShockSequence(caster, result.lightningShockDamage, result.lightningShockProcCount);
        }
    }

    private IEnumerator ApplyLightningMarkShockSequence(CombatActor caster, int damage, int procCount)
    {
        if (caster == null || damage <= 0 || procCount <= 0) yield break;

        for (int i = 0; i < procCount; i++)
        {
            ApplyLightningMarkShock(caster, damage);
            if (i < procCount - 1 && GlobalDelayedSecondaryStep > 0f)
                yield return new WaitForSeconds(GlobalDelayedSecondaryStep);
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
            CombatHitFeedback.Play(t, CombatHitFeedback.FeedbackKind.Hit);
            if (popups != null)
                popups.SpawnDamageSplit(caster, t, res.blocked, res.hpLost);
        }
    }

    private void ApplyDelayedBurnConsumeDamage(CombatActor caster, CombatActor target, int damage)
    {
        if (caster == null || target == null || target.IsDead || damage <= 0)
            return;

        var popups = GetPopups();
        var result = target.TakeDamageDetailed(damage, bypassGuard: false);
        CombatHitFeedback.Play(target, CombatHitFeedback.FeedbackKind.BurnConsume);
        if (popups != null)
            popups.SpawnDamageSplit(caster, target, result.blocked, result.hpLost);
    }

    private int ApplyCustomGuardBehavior(SkillRuntime rt, CombatActor caster, int baseGuard)
    {
        if (rt == null || caster == null)
            return baseGuard;

        return baseGuard;
    }

    public void ResetPlayerCastVisualState()
    {
        if (playerDiceCastAnimator == null)
            playerDiceCastAnimator = GetComponent<PlayerDiceCastAnimator>();

        if (playerDiceCastAnimator != null)
            playerDiceCastAnimator.ResetAllVisualState();
    }

    private bool ShouldUsePlayerDiceCastAnimation(CombatActor caster, DiceSlotRig castDiceRig, int castStart0, int castSpan)
    {
        if (caster == null || castDiceRig == null)
            return false;
        if (!caster.isPlayer)
            return false;
        if (castStart0 < 0 || castSpan <= 0)
            return false;
        return GetPlayerDiceCastAnimator() != null;
    }

    private PlayerDiceCastAnimator GetPlayerDiceCastAnimator()
    {
        if (playerDiceCastAnimator != null)
            return playerDiceCastAnimator;

        playerDiceCastAnimator = GetComponent<PlayerDiceCastAnimator>();
        if (playerDiceCastAnimator == null)
            playerDiceCastAnimator = gameObject.AddComponent<PlayerDiceCastAnimator>();
        return playerDiceCastAnimator;
    }

    private static PlayerDiceCastAnimator.CastMode ResolveCastMode(
        CombatActor caster,
        CombatActor primaryTarget,
        IReadOnlyList<CombatActor> aoeTargets)
    {
        if (caster == null || primaryTarget == null)
            return PlayerDiceCastAnimator.CastMode.Self;

        if (primaryTarget.team == caster.team)
            return PlayerDiceCastAnimator.CastMode.Self;

        return aoeTargets != null && aoeTargets.Count > 1
            ? PlayerDiceCastAnimator.CastMode.EnemyAoeAnchor
            : PlayerDiceCastAnimator.CastMode.EnemySingle;
    }

}

public class PlayerDiceCastAnimator : MonoBehaviour
{
    public enum CastMode
    {
        Self,
        EnemySingle,
        EnemyAoeAnchor
    }

    [Header("Shared Timing")]
    [Min(0f)] public float interDieDelay = 0.12f;
    [Min(0.01f)] public float launchPrepDuration = 0.10f;
    [Min(0f)] public float launchPrepLift = 0.18f;
    [Min(0f)] public float usedDropDistance = 0.24f;
    [Min(0.01f)] public float usedDropDuration = 0.10f;
    [Range(0f, 1f)] public float liftCommitPortionBeforeThrow = 0.1f;
    [Range(0f, 1f)] public float returnCatchOverlapPortion = 0.22f;

    [Header("Enemy Throw")]
    [Min(0.01f)] public float throwDuration = 0.24f;
    [Min(0.01f)] public float returnDuration = 0.41f;
    [Min(0f)] public float outwardArcHeight = 0.38f;
    [Min(0f)] public float returnArcHeight = 4.8f;
    [Min(0f)] public float impactHoldDuration = 0.03f;
    [Min(0.01f)] public float enemyImpactScale = 0.5f;
    [Min(0.01f)] public float enemyReturnPeakScale = 1.5f;

    [Header("Self Slam")]
    [Min(0.01f)] public float selfHopDuration = 0.10f;
    [Min(0.01f)] public float selfSlamDuration = 0.16f;
    [Min(0f)] public float selfHopHeight = 1.5f;

    [Header("Impact Feel")]
    [Min(0f)] public float impactPunchScale = 0.08f;
    [Min(0.01f)] public float impactPunchDuration = 0.16f;
    [Min(0f)] public float plateCatchPunchScale = 0.12f;
    [Min(0.01f)] public float plateCatchPunchDuration = 0.18f;

    private struct TransformState
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
    }

    private sealed class SlotVisualRefs
    {
        public Transform dieRoot;
        public Transform plateRoot;
        public DiceSpinnerGeneric spinner;
        public Transform spinnerTransform;
    }

    private sealed class ProxyVisualRefs
    {
        public GameObject rootObject;
        public Transform root;
        public DiceSpinnerGeneric spinner;
        public Transform spinnerTransform;
        public Vector3 baseScale;
    }

    private struct DetachedTransformState
    {
        public Transform parent;
        public int siblingIndex;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 localScale;
        public Vector3 worldScale;
    }

    private readonly Dictionary<Transform, TransformState> _baselineStates = new Dictionary<Transform, TransformState>();
    private readonly Dictionary<Transform, DetachedTransformState> _detachedStates = new Dictionary<Transform, DetachedTransformState>();
    private readonly Dictionary<DiceSpinnerGeneric, DiceDraggableUI> _diceUiBySpinner = new Dictionary<DiceSpinnerGeneric, DiceDraggableUI>();
    private readonly List<Tween> _activeTweens = new List<Tween>();
    private readonly List<GameObject> _activeProxyObjects = new List<GameObject>();
    private readonly HashSet<Transform> _hiddenOriginalRoots = new HashSet<Transform>();
    private readonly HashSet<Transform> _temporarilyReleasedWorldSyncRoots = new HashSet<Transform>();

    public IEnumerator PlayCast(
        DiceSlotRig diceRig,
        int start0,
        int span,
        CombatActor caster,
        CombatActor primaryTarget,
        IReadOnlyList<CombatActor> aoeTargets,
        CastMode mode,
        Action onFinalImpact)
    {
        List<SlotVisualRefs> slots = CollectSlotVisuals(diceRig, start0, span);
        if (slots.Count == 0)
        {
            onFinalImpact?.Invoke();
            yield break;
        }

        int completedCount = 0;
        bool finalImpactTriggered = false;

        for (int i = 0; i < slots.Count; i++)
        {
            SlotVisualRefs slot = slots[i];
            int orderIndex = i;
            bool isFinal = i == slots.Count - 1;
            StartCoroutine(AnimateSlotRoutine(
                slot,
                orderIndex,
                isFinal,
                caster,
                primaryTarget,
                aoeTargets,
                mode,
                () =>
                {
                    if (isFinal && !finalImpactTriggered)
                    {
                        finalImpactTriggered = true;
                        onFinalImpact?.Invoke();
                    }
                },
                () => completedCount++));
        }

        while (completedCount < slots.Count)
            yield return null;
    }

    public void ResetAllVisualState()
    {
        for (int i = _activeTweens.Count - 1; i >= 0; i--)
        {
            Tween tween = _activeTweens[i];
            if (tween != null && tween.active)
                tween.Kill(false);
        }

        _activeTweens.Clear();

        for (int i = _activeProxyObjects.Count - 1; i >= 0; i--)
        {
            GameObject proxy = _activeProxyObjects[i];
            if (proxy != null)
                Destroy(proxy);
        }

        _activeProxyObjects.Clear();

        foreach (Transform releasedRoot in _temporarilyReleasedWorldSyncRoots)
            DiceEquipWorldSyncUtility.EndTemporaryRelease(releasedRoot);
        _temporarilyReleasedWorldSyncRoots.Clear();

        foreach (KeyValuePair<Transform, DetachedTransformState> kvp in _detachedStates)
            RestoreDetachedTransform(kvp.Key, kvp.Value);
        _detachedStates.Clear();

        foreach (Transform hiddenRoot in _hiddenOriginalRoots)
            SetRenderableVisibility(hiddenRoot, true);
        _hiddenOriginalRoots.Clear();

        foreach (KeyValuePair<Transform, TransformState> kvp in _baselineStates)
        {
            Transform target = kvp.Key;
            if (target == null)
                continue;

            TransformState state = kvp.Value;
            target.position = state.position;
            target.rotation = state.rotation;
            target.localScale = state.scale;
        }

        _baselineStates.Clear();
    }

    private IEnumerator AnimateSlotRoutine(
        SlotVisualRefs slot,
        int orderIndex,
        bool isFinal,
        CombatActor caster,
        CombatActor primaryTarget,
        IReadOnlyList<CombatActor> aoeTargets,
        CastMode mode,
        Action onImpact,
        Action onComplete)
    {
        if (slot == null || slot.dieRoot == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        if (orderIndex > 0 && interDieDelay > 0f)
            yield return new WaitForSeconds(interDieDelay * orderIndex);

        CacheTransformState(slot.dieRoot);
        if (slot.plateRoot != null && slot.plateRoot != slot.dieRoot)
            CacheTransformState(slot.plateRoot);

        if (mode == CastMode.Self)
            yield return AnimateSelfSlam(slot, isFinal, caster, onImpact);
        else
            yield return AnimateEnemyThrow(slot, orderIndex, isFinal, primaryTarget, aoeTargets, mode, onImpact);

        onComplete?.Invoke();
    }

    private IEnumerator AnimateEnemyThrow(
        SlotVisualRefs slot,
        int orderIndex,
        bool isFinal,
        CombatActor primaryTarget,
        IReadOnlyList<CombatActor> aoeTargets,
        CastMode mode,
        Action onImpact)
    {
        Transform die = slot.dieRoot;
        Transform plate = slot.plateRoot;
        DiceSpinnerGeneric spinner = slot.spinner;
        Transform moveTarget = spinner != null && spinner.pivot != null ? spinner.pivot : die;
        DiceDraggableUI diceUi = ResolveDiceUi(spinner);
        bool uiCardControlsLiftDrop = diceUi != null;
        Tween uiLiftTween = null;
        float liftLeadTime = Mathf.Clamp(launchPrepDuration * Mathf.Clamp01(liftCommitPortionBeforeThrow), 0f, launchPrepDuration);

        if (uiCardControlsLiftDrop)
        {
            uiLiftTween = diceUi.AnimateCastDisplayToReady(launchPrepDuration, Ease.OutBack);
        }

        Tween liftTween = null;
        bool plateCarriesDie = plate != null && plate != die && die != null && die.IsChildOf(plate);
        Vector3 plateReadyPosition = plate != null && plate != die
            ? plate.position + Vector3.up * (usedDropDistance * 0.7f)
            : Vector3.zero;
        if (!uiCardControlsLiftDrop && plateCarriesDie)
        {
            liftTween = RegisterTween(plate.DOMove(plateReadyPosition, launchPrepDuration).SetEase(Ease.OutBack));
        }

        if (liftLeadTime > 0f)
            yield return new WaitForSeconds(liftLeadTime);

        TransformState dieBase = GetTransformState(die);
        TransformState plateBase = plate != null && plate != die ? GetTransformState(plate) : default;
        TransformState moveBase = GetTransformState(moveTarget);
        if (plate != null && plate != die)
            plateReadyPosition = plateBase.position + Vector3.up * (usedDropDistance * 0.7f);

        ReleaseWorldSyncForCast(die, plate);

        float totalRollDuration = throwDuration + Mathf.Max(0f, impactHoldDuration) + returnDuration;
        Tween rollTween = StartExistingSpinnerPresentationRoll(spinner, orderIndex, totalRollDuration);

        Vector3 impactPoint = ResolveEnemyImpactPoint(primaryTarget, moveBase.position);
        Vector3 outwardControl = BuildArcControlPoint(moveBase.position, impactPoint, outwardArcHeight, 0f);
        Quaternion moveRotation = moveBase.rotation;

        Tween outwardTween = CreateQuadraticTween(
            moveTarget,
            moveBase.position,
            outwardControl,
            impactPoint,
            throwDuration,
            moveRotation,
            moveRotation);
        if (outwardTween != null)
            yield return outwardTween.WaitForCompletion();

        PlayTargetImpactFeedback(primaryTarget, isFinal);
        if (mode == CastMode.EnemyAoeAnchor && isFinal)
            PlayAoeTargetFeedback(aoeTargets, primaryTarget);

        if (isFinal)
            onImpact?.Invoke();

        if (impactHoldDuration > 0f)
            yield return new WaitForSeconds(impactHoldDuration);

        Vector3 returnControl = BuildArcControlPoint(impactPoint, moveBase.position, returnArcHeight, 0f);
        Tween returnTween = CreateQuadraticTween(
            moveTarget,
            impactPoint,
            returnControl,
            moveBase.position,
            returnDuration,
            moveRotation,
            moveRotation);

        float returnOverlapLead = Mathf.Clamp(returnDuration * Mathf.Clamp01(returnCatchOverlapPortion), 0f, returnDuration);
        float returnPreCatchTime = Mathf.Max(0f, returnDuration - returnOverlapLead);
        if (returnPreCatchTime > 0f)
            yield return new WaitForSeconds(returnPreCatchTime);

        Tween dropTween = null;
        if (uiCardControlsLiftDrop)
        {
            RestoreWorldSyncForCast(die, plate);
            dropTween = diceUi.AnimateCastDisplayToSpent(usedDropDuration, Ease.InOutQuad);
        }
        else if (plateCarriesDie)
        {
            PlayPlateCatch(plate);
            dropTween = RegisterTween(plate.DOMove(plateBase.position, usedDropDuration).SetEase(Ease.InOutQuad));
        }

        if (returnTween != null)
            yield return returnTween.WaitForCompletion();

        if (rollTween != null && rollTween.active)
            yield return rollTween.WaitForCompletion();
        if (dropTween != null && dropTween.active)
            yield return dropTween.WaitForCompletion();

        if (!plateCarriesDie)
        {
            moveTarget.position = moveBase.position;
            moveTarget.rotation = moveBase.rotation;
            die.position = dieBase.position;
            die.rotation = dieBase.rotation;
        }

        if (plate != null && plate != die)
        {
            plate.position = plateCarriesDie ? plateReadyPosition : plateBase.position;
            plate.rotation = plateBase.rotation;
        }

        if (uiCardControlsLiftDrop)
        {
            diceUi.EndCastMotionLock();
        }
        else if (plateCarriesDie)
        {
            RestoreWorldSyncForCast(die, plate);
        }
        else
        {
            RestoreWorldSyncForCast(die, plate);
            SnapToUsedState(die, plate, dieBase, plateBase);
        }
    }

    private IEnumerator AnimateSelfSlam(
        SlotVisualRefs slot,
        bool isFinal,
        CombatActor caster,
        Action onImpact)
    {
        Transform die = slot.dieRoot;
        Transform plate = slot.plateRoot;
        DiceSpinnerGeneric spinner = slot.spinner;
        DiceDraggableUI diceUi = ResolveDiceUi(spinner);
        bool uiCardControlsLiftDrop = diceUi != null;
        bool plateCarriesDie = plate != null && plate != die && die != null && die.IsChildOf(plate);
        Tween uiLiftTween = null;
        Tween plateLiftTween = null;
        float liftLeadTime = Mathf.Clamp(launchPrepDuration * Mathf.Clamp01(liftCommitPortionBeforeThrow), 0f, launchPrepDuration);

        if (uiCardControlsLiftDrop)
            uiLiftTween = diceUi.AnimateCastDisplayToReady(launchPrepDuration, Ease.OutBack);

        if (!uiCardControlsLiftDrop && plateCarriesDie)
        {
            Vector3 plateReadyPosition = plate.position + Vector3.up * (usedDropDistance * 0.7f);
            plateLiftTween = RegisterTween(plate.DOMove(plateReadyPosition, launchPrepDuration).SetEase(Ease.OutBack));
        }

        if (liftLeadTime > 0f)
            yield return new WaitForSeconds(liftLeadTime);

        TransformState dieBase = GetTransformState(die);
        TransformState plateBase = plate != null && plate != die ? GetTransformState(plate) : default;
        ReleaseWorldSyncForCast(die, plate);

        Vector3 apex = dieBase.position + Vector3.up * selfHopHeight;
        Tween hopTween = RegisterTween(die.DOMoveY(apex.y, selfHopDuration).SetEase(Ease.OutQuad));
        yield return hopTween.WaitForCompletion();

        Vector3 slamTarget = dieBase.position + Vector3.down * usedDropDistance;
        Tween slamTween = RegisterTween(die.DOMoveY(slamTarget.y, selfSlamDuration).SetEase(Ease.InQuad));

        float returnOverlapLead = Mathf.Clamp(selfSlamDuration * Mathf.Clamp01(returnCatchOverlapPortion), 0f, selfSlamDuration);
        float slamPreCatchTime = Mathf.Max(0f, selfSlamDuration - returnOverlapLead);
        if (slamPreCatchTime > 0f)
            yield return new WaitForSeconds(slamPreCatchTime);

        Tween dropTween = null;
        if (uiCardControlsLiftDrop)
        {
            RestoreWorldSyncForCast(die, plate);
            dropTween = diceUi.AnimateCastDisplayToSpent(usedDropDuration, Ease.InOutQuad);
        }
        else if (plate != null && plate != die)
        {
            Vector3 plateUsed = plateBase.position + Vector3.down * (usedDropDistance * 0.7f);
            PlayPlateCatch(plate);
            dropTween = RegisterTween(plate.DOMoveY(plateUsed.y, usedDropDuration).SetEase(Ease.InOutQuad));
        }

        yield return slamTween.WaitForCompletion();

        PlayTargetImpactFeedback(caster, isFinal);
        if (isFinal)
            onImpact?.Invoke();

        die.position = slamTarget;
        die.rotation = dieBase.rotation;

        if (uiLiftTween != null && uiLiftTween.active)
            yield return uiLiftTween.WaitForCompletion();
        if (plateLiftTween != null && plateLiftTween.active)
            yield return plateLiftTween.WaitForCompletion();
        if (dropTween != null && dropTween.active)
            yield return dropTween.WaitForCompletion();

        if (uiCardControlsLiftDrop)
        {
            diceUi.EndCastMotionLock();
        }
        else
        {
            RestoreWorldSyncForCast(die, plate);
        }
    }

    private IEnumerator DropIntoUsedState(Transform die, Transform plate, TransformState dieBase, TransformState plateBase)
    {
        if (die == null)
            yield break;

        Vector3 dieUsed = dieBase.position + Vector3.down * usedDropDistance;
        Sequence used = DOTween.Sequence();
        used.Append(RegisterTween(die.DOMove(dieUsed, usedDropDuration).SetEase(Ease.InOutQuad)));

        if (plate != null && plate != die)
        {
            Vector3 plateUsed = plateBase.position + Vector3.down * (usedDropDistance * 0.7f);
            used.Join(RegisterTween(plate.DOMove(plateUsed, usedDropDuration).SetEase(Ease.InOutQuad)));
        }

        yield return used.WaitForCompletion();

        die.position = dieUsed;
        die.rotation = dieBase.rotation;
    }

    private void SnapToUsedState(Transform die, Transform plate, TransformState dieBase, TransformState plateBase)
    {
        if (die == null)
            return;

        die.position = dieBase.position + Vector3.down * usedDropDistance;
        die.rotation = dieBase.rotation;

        if (plate != null && plate != die)
        {
            plate.position = plateBase.position + Vector3.down * (usedDropDistance * 0.7f);
            plate.rotation = plateBase.rotation;
        }
    }

    private Tween CreateQuadraticTween(
        Transform target,
        Vector3 p0,
        Vector3 pc,
        Vector3 p1,
        float duration,
        Quaternion rot0,
        Quaternion rot1)
    {
        if (target == null)
            return null;

        return RegisterTween(DOVirtual.Float(0f, 1f, Mathf.Max(0.01f, duration), t =>
        {
            float eased = DOVirtual.EasedValue(0f, 1f, t, Ease.InOutSine);
            target.position = EvaluateQuadratic(p0, pc, p1, eased);
            target.rotation = Quaternion.SlerpUnclamped(rot0, rot1, eased);
        }).SetEase(Ease.Linear));
    }

    private Vector3 ResolveEnemyImpactPoint(CombatActor primaryTarget, Vector3 fallback)
    {
        if (primaryTarget == null)
            return fallback;

        ActorWorldUI worldUi = primaryTarget.GetComponent<ActorWorldUI>();
        if (worldUi != null)
        {
            Transform uiTransform = worldUi.transform;
            return uiTransform.position + new Vector3(0f, 0.1f, 0f);
        }

        Transform targetTransform = primaryTarget.transform;
        return targetTransform != null
            ? targetTransform.position + new Vector3(0f, 0.1f, 0f)
            : fallback;
    }

    private Vector3 BuildArcControlPoint(Vector3 start, Vector3 end, float arcHeight, float forwardBias)
    {
        Vector3 midpoint = Vector3.Lerp(start, end, 0.5f);
        Vector3 travel = end - start;
        Vector3 planar = new Vector3(travel.x, 0f, travel.z);
        Vector3 forward = planar.sqrMagnitude > 0.0001f ? planar.normalized : Vector3.right;
        return midpoint + Vector3.up * arcHeight + forward * forwardBias;
    }

    private List<SlotVisualRefs> CollectSlotVisuals(DiceSlotRig diceRig, int start0, int span)
    {
        var results = new List<SlotVisualRefs>();
        if (diceRig == null || diceRig.slots == null)
            return results;

        start0 = Mathf.Clamp(start0, 0, 2);
        span = Mathf.Clamp(span, 1, 3);

        for (int i = start0; i < start0 + span && i < diceRig.slots.Length; i++)
        {
            DiceSlotRig.Entry entry = diceRig.slots[i];
            if (entry == null)
                continue;

            Transform dieRoot = entry.diceRoot != null
                ? entry.diceRoot.transform
                : (entry.dice != null ? entry.dice.transform : null);
            if (dieRoot == null)
                continue;

            Transform plateRoot = entry.slotRoot != null ? entry.slotRoot.transform : null;
            DiceSpinnerGeneric spinner = entry.dice != null ? entry.dice : dieRoot.GetComponentInChildren<DiceSpinnerGeneric>(true);
            results.Add(new SlotVisualRefs
            {
                dieRoot = dieRoot,
                plateRoot = plateRoot,
                spinner = spinner,
                spinnerTransform = spinner != null ? spinner.transform : dieRoot
            });
        }

        return results;
    }

    private void CacheTransformState(Transform target)
    {
        if (target == null || _baselineStates.ContainsKey(target))
            return;

        _baselineStates[target] = new TransformState
        {
            position = target.position,
            rotation = target.rotation,
            scale = target.localScale
        };
    }

    private TransformState GetTransformState(Transform target)
    {
        if (target == null)
            return default;

        if (_baselineStates.TryGetValue(target, out TransformState state))
            return state;

        state = new TransformState
        {
            position = target.position,
            rotation = target.rotation,
            scale = target.localScale
        };
        _baselineStates[target] = state;
        return state;
    }

    private Tween RegisterTween(Tween tween)
    {
        if (tween == null)
            return null;

        _activeTweens.Add(tween);
        tween.OnKill(() => _activeTweens.Remove(tween));
        return tween;
    }

    private void PlayTargetImpactFeedback(CombatActor actor, bool finalImpact)
    {
        if (actor == null)
            return;

        float strength = Mathf.Max(0f, impactPunchScale) * (finalImpact ? 1.35f : 0.8f);
        RegisterTween(actor.transform.DOPunchScale(Vector3.one * strength, impactPunchDuration, 1, 0.55f));
    }

    private ProxyVisualRefs CreateFlightProxy(SlotVisualRefs source, TransformState dieBase)
    {
        if (source == null || source.dieRoot == null)
            return null;

        GameObject proxyObject = Instantiate(source.dieRoot.gameObject, dieBase.position, dieBase.rotation, transform);
        proxyObject.name = source.dieRoot.gameObject.name + "_CastProxy";
        SetWorldScale(proxyObject.transform, source.dieRoot.lossyScale);
        _activeProxyObjects.Add(proxyObject);

        DiceSpinnerGeneric proxySpinner = proxyObject.GetComponentInChildren<DiceSpinnerGeneric>(true);
        if (proxySpinner != null && source.spinner != null)
        {
            proxySpinner.CopyRuntimeStateFrom(source.spinner, copyRotation: true);
            proxySpinner.enableSpaceKey = false;
        }

        return new ProxyVisualRefs
        {
            rootObject = proxyObject,
            root = proxyObject.transform,
            spinner = proxySpinner,
            spinnerTransform = proxySpinner != null ? proxySpinner.transform : proxyObject.transform,
            baseScale = proxySpinner != null ? proxySpinner.transform.localScale : proxyObject.transform.localScale
        };
    }

    private Tween StartSpinnerSpin(ProxyVisualRefs proxy, int orderIndex, float duration, int baseLoopsX, int baseLoopsY, int baseLoopsZ)
    {
        if (proxy == null || proxy.spinnerTransform == null)
            return null;

        Vector3 startEuler = proxy.spinnerTransform.localEulerAngles;
        Vector3 endEuler = startEuler + new Vector3(
            360f * (baseLoopsX + (orderIndex % 2)),
            360f * (baseLoopsY + (orderIndex % 3)),
            360f * (baseLoopsZ + ((orderIndex + 1) % 2)));

        return RegisterTween(proxy.spinnerTransform.DOLocalRotate(endEuler, Mathf.Max(0.01f, duration), RotateMode.FastBeyond360).SetEase(Ease.Linear));
    }

    private Tween StartExistingSpinnerPresentationRoll(DiceSpinnerGeneric spinner, int orderIndex, float duration)
    {
        if (spinner == null)
            return null;

        Transform spinTarget = spinner.FlightSpinTarget;
        if (spinTarget == null)
            return null;

        spinTarget.DOKill(complete: false);

        Vector3 startEuler = spinTarget.localEulerAngles;
        Vector3 targetEuler = spinTarget == spinner.pivot
            ? ResolvePivotTargetEulerForCurrentFace(spinner, startEuler)
            : Vector3.zero;

        float safeDuration = Mathf.Max(0.02f, duration);
        float accelDuration = Mathf.Clamp(launchPrepDuration, 0.04f, safeDuration * 0.25f);
        float settleDuration = Mathf.Max(0.01f, safeDuration - accelDuration);

        int loopsX = spinner.loopsMin.x + (spinner.loopsMax.x - spinner.loopsMin.x) / 2 + (orderIndex % 2);
        int loopsY = spinner.loopsMin.y + (spinner.loopsMax.y - spinner.loopsMin.y) / 2 + ((orderIndex + 1) % 2);
        int loopsZ = spinner.loopsMin.z + (spinner.loopsMax.z - spinner.loopsMin.z) / 2 + ((orderIndex + 2) % 2);

        Vector3 endEuler = targetEuler + new Vector3(360f * loopsX, 360f * loopsY, 360f * loopsZ);
        Vector3 midEuler = Vector3.Lerp(startEuler, endEuler, spinner.accelPortion);

        Sequence seq = DOTween.Sequence();
        seq.Append(RegisterTween(spinTarget.DOLocalRotate(midEuler, accelDuration, RotateMode.FastBeyond360).SetEase(Ease.InQuad)));
        seq.Append(RegisterTween(spinTarget.DOLocalRotate(endEuler, settleDuration, RotateMode.FastBeyond360).SetEase(Ease.OutQuart)));
        return RegisterTween(seq);
    }

    private DiceDraggableUI ResolveDiceUi(DiceSpinnerGeneric spinner)
    {
        if (spinner == null)
            return null;

        if (_diceUiBySpinner.TryGetValue(spinner, out DiceDraggableUI cached) && cached != null && cached.dice == spinner)
            return cached;

        DiceDraggableUI[] allDiceUi = FindObjectsOfType<DiceDraggableUI>(true);
        for (int i = 0; i < allDiceUi.Length; i++)
        {
            DiceDraggableUI candidate = allDiceUi[i];
            if (candidate == null || candidate.dice != spinner)
                continue;

            _diceUiBySpinner[spinner] = candidate;
            return candidate;
        }

        _diceUiBySpinner.Remove(spinner);
        return null;
    }

    private int ResolvePresentationFaceIndex(DiceSpinnerGeneric spinner)
    {
        if (spinner == null)
            return -1;

        if (spinner.LastFaceIndex >= 0 && spinner.faces != null && spinner.LastFaceIndex < spinner.faces.Length)
            return spinner.LastFaceIndex;

        Camera cam = Camera.main;
        return spinner.GetBestFacingFaceIndex(cam);
    }

    private static Vector3 ResolvePivotTargetEulerForCurrentFace(DiceSpinnerGeneric spinner, Vector3 fallback)
    {
        if (spinner == null || spinner.faces == null)
            return fallback;

        int faceIndex = spinner.LastFaceIndex;
        if (faceIndex < 0 || faceIndex >= spinner.faces.Length)
            return fallback;

        Vector3 euler = spinner.faces[faceIndex].localEuler;
        return new Vector3(NormalizeAngle(euler.x), NormalizeAngle(euler.y), NormalizeAngle(euler.z));
    }

    private static float NormalizeAngle(float value)
    {
        value %= 360f;
        if (value < 0f)
            value += 360f;
        return value;
    }


    private Tween StartProxyScaleTween(ProxyVisualRefs proxy, float scaleMultiplier, float duration, Ease ease)
    {
        if (proxy == null || proxy.spinnerTransform == null)
            return null;

        return RegisterTween(proxy.spinnerTransform.DOScale(proxy.baseScale * scaleMultiplier, Mathf.Max(0.01f, duration)).SetEase(ease));
    }

    private void DestroyFlightProxy(ProxyVisualRefs proxy)
    {
        if (proxy == null || proxy.rootObject == null)
            return;

        _activeProxyObjects.Remove(proxy.rootObject);
        Destroy(proxy.rootObject);
    }

    private void ReleaseWorldSyncForCast(Transform die, Transform plate)
    {
        TryReleaseWorldSyncRoot(die);
        if (plate != null && plate != die)
            TryReleaseWorldSyncRoot(plate);
    }

    private void RestoreWorldSyncForCast(Transform die, Transform plate)
    {
        TryRestoreWorldSyncRoot(die);
        if (plate != null && plate != die)
            TryRestoreWorldSyncRoot(plate);
    }

    private void TryReleaseWorldSyncRoot(Transform root)
    {
        if (root == null || !_temporarilyReleasedWorldSyncRoots.Add(root))
            return;

        DiceEquipWorldSyncUtility.BeginTemporaryRelease(root);
    }

    private void TryRestoreWorldSyncRoot(Transform root)
    {
        if (root == null || !_temporarilyReleasedWorldSyncRoots.Remove(root))
            return;

        DiceEquipWorldSyncUtility.EndTemporaryRelease(root);
    }

    private DetachedTransformState DetachForFlight(Transform target)
    {
        DetachedTransformState state = new DetachedTransformState
        {
            parent = target.parent,
            siblingIndex = target.GetSiblingIndex(),
            localPosition = target.localPosition,
            localRotation = target.localRotation,
            localScale = target.localScale,
            worldScale = target.lossyScale
        };

        _detachedStates[target] = state;
        target.SetParent(transform, true);
        SetWorldScale(target, state.worldScale);
        return state;
    }

    private void RestoreDetachedTransform(Transform target, DetachedTransformState state)
    {
        if (target == null)
            return;

        target.SetParent(state.parent, true);

        if (state.parent != null)
        {
            int maxIndex = Mathf.Max(0, state.parent.childCount - 1);
            target.SetSiblingIndex(Mathf.Clamp(state.siblingIndex, 0, maxIndex));
        }

        target.localPosition = state.localPosition;
        target.localRotation = state.localRotation;
        target.localScale = state.localScale;
    }

    private static void SetWorldScale(Transform target, Vector3 desiredWorldScale)
    {
        if (target == null)
            return;

        Vector3 parentScale = target.parent != null ? target.parent.lossyScale : Vector3.one;
        target.localScale = new Vector3(
            SafeDivide(desiredWorldScale.x, parentScale.x),
            SafeDivide(desiredWorldScale.y, parentScale.y),
            SafeDivide(desiredWorldScale.z, parentScale.z));
    }

    private static float SafeDivide(float value, float divisor)
    {
        return Mathf.Abs(divisor) <= 0.0001f ? value : value / divisor;
    }

    private void SetRenderableVisibility(Transform root, bool visible)
    {
        if (root == null)
            return;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].enabled = visible;
        }
    }

    private void PlayAoeTargetFeedback(IReadOnlyList<CombatActor> aoeTargets, CombatActor primaryTarget)
    {
        if (aoeTargets == null)
            return;

        for (int i = 0; i < aoeTargets.Count; i++)
        {
            CombatActor actor = aoeTargets[i];
            if (actor == null || actor.IsDead || actor == primaryTarget)
                continue;

            RegisterTween(actor.transform.DOPunchScale(Vector3.one * (impactPunchScale * 0.75f), impactPunchDuration * 0.9f, 1, 0.5f).SetDelay(i * 0.03f));
        }
    }

    private void PlayPlateCatch(Transform plate)
    {
        if (plate == null)
            return;

        float strength = Mathf.Max(0f, plateCatchPunchScale);
        if (strength <= 0f)
            return;

        RegisterTween(plate.DOPunchScale(Vector3.one * strength, plateCatchPunchDuration, 1, 0.6f));
    }

    private Vector3 GetImpactPoint(CombatActor actor)
    {
        if (actor == null)
            return transform.position;

        Transform namedImpact = actor.transform.Find("ImpactPoint");
        if (namedImpact != null)
            return namedImpact.position;

        if (actor.uiAnchor != null)
            return actor.uiAnchor.position;

        Renderer[] renderers = actor.GetComponentsInChildren<Renderer>(true);
        if (renderers != null && renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                if (renderers[i] == null)
                    continue;
                bounds.Encapsulate(renderers[i].bounds);
            }
            return bounds.center;
        }

        Collider2D hitbox2D = actor.GetComponentInChildren<Collider2D>(true);
        if (hitbox2D != null)
            return hitbox2D.bounds.center;

        return actor.transform.position;
    }

    private static Vector3 EvaluateQuadratic(Vector3 p0, Vector3 pc, Vector3 p1, float t)
    {
        float u = 1f - t;
        return (u * u * p0) + (2f * u * t * pc) + (t * t * p1);
    }
}
