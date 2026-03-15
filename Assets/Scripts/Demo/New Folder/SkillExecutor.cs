using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class SkillExecutor : MonoBehaviour
{
    [System.Serializable]
    public struct AttackPreview
    {
        public int effectiveDieValue;
        public int baseDamage;
        public int bonusDamage;
        public int finalDamage;
        public bool canDealDamage;
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

    private DamagePopupSystem GetPopups()
    {
        if (damagePopups != null) return damagePopups;
        damagePopups = FindObjectOfType<DamagePopupSystem>();
        return damagePopups;
    }

    public IEnumerator ExecuteSkill(SkillSO skill, CombatActor caster, CombatActor target, int dieValue, bool skipCost = false)
    {
        var rt = SkillRuntime.FromSkill(skill);
        yield return ExecuteSkill(rt, caster, target, dieValue, skipCost);
    }

    // NEW: SkillDamageSO direct execution (builds runtime; condition requires dice context, so best-effort)
    public IEnumerator ExecuteSkill(SkillDamageSO skill, CombatActor caster, CombatActor target, int dieValue, bool skipCost = false)
    {
        var rt = SkillRuntime.FromDamage(skill);
        yield return ExecuteSkill(rt, caster, target, dieValue, skipCost);
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

        List<CombatActor> targets = new List<CombatActor>(8);
        switch (skill.target)
        {
            case SkillTargetRule.Self:
                targets.Add(caster);
                break;

            case SkillTargetRule.AllEnemies:
            case SkillTargetRule.AllAllies:
            case SkillTargetRule.AllUnits:
                if (aoeTargets != null) targets.AddRange(aoeTargets);
                else if (clickedTarget != null) targets.Add(clickedTarget);
                break;

            case SkillTargetRule.SingleAlly:
            case SkillTargetRule.SingleEnemy:
            default:
                if (clickedTarget != null) targets.Add(clickedTarget);
                break;
        }

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
            ((rt.useV2Targeting && (rt.targetRuleV2 == SkillTargetRule.AllEnemies || rt.targetRuleV2 == SkillTargetRule.AllAllies || rt.targetRuleV2 == SkillTargetRule.AllUnits))
             || (!rt.useV2Targeting && (rt.hitAllEnemies || rt.hitAllAllies)));

        bool useAoe = wantsAoe && aoeTargets != null && aoeTargets.Count > 0;

        CombatActor primaryTarget = ResolveTarget(rt, caster, clickedTarget, useAoe ? aoeTargets : null);
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

            // apply GuardGainPercent passive
            float pct = 0f;
            var ps = caster.GetComponent<PassiveSystem>();
            if (ps != null) pct = ps.GetGuardGainPercent();

            float mult = 1f + Mathf.Max(-0.99f, pct);
            int scaledGuard = Mathf.FloorToInt(baseGuard * mult);

            caster.SetGuard(scaledGuard);
            caster.GainFocus(rt.focusGainOnCast);

            yield return new WaitForSeconds(delayBetweenActions);
            yield break;
        }

        if (rt.kind == SkillKind.Attack)
        {
            // MELEE (visual 1 lần)
            if (rt.range == RangeType.Melee)
            {
                yield return MeleeLungeDOTween_HitMoment(caster, primaryTarget, () =>
                {
                    CombatActor.DamageResult res = default;

                    // Apply đúng 1 lần tại hit moment
                    if (useAoe)
                    {
                        ApplyAttackToTargets(rt, caster, aoeTargets, dieValue);
                    }
                    else
                    {
                        res = ApplyAttack(rt, caster, primaryTarget, dieValue);
                        var popups = GetPopups();
                        if (popups != null && primaryTarget != null)
                            popups.SpawnDamageSplit(caster, primaryTarget, res.blocked, res.hpLost);
                    }
                });

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
                    CombatActor.DamageResult res = default;

                    if (useAoe)
                    {
                        ApplyAttackToTargets(rt, caster, aoeTargets, dieValue);
                    }
                    else
                    {
                        res = ApplyAttack(rt, caster, primaryTarget, dieValue);
                        var popups = GetPopups();
                        if (popups != null && primaryTarget != null)
                            popups.SpawnDamageSplit(caster, primaryTarget, res.blocked, res.hpLost);
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

    private static CombatActor ResolveTarget(SkillRuntime rt, CombatActor caster, CombatActor clicked, IReadOnlyList<CombatActor> aoeTargets)
    {
        if (rt == null) return null;

        // New targeting
        if (rt.useV2Targeting)
        {
            switch (rt.targetRuleV2)
            {
                case SkillTargetRule.Self:
                    return caster;

                case SkillTargetRule.SingleAlly:
                    return clicked != null ? clicked : caster;

                case SkillTargetRule.AllAllies:
                    return caster;

                case SkillTargetRule.AllEnemies:
                case SkillTargetRule.AllUnits:
                    if (clicked != null) return clicked;
                    if (aoeTargets != null && aoeTargets.Count > 0) return aoeTargets[0];
                    return null;

                case SkillTargetRule.SingleEnemy:
                default:
                    return clicked;
            }
        }

        // Legacy targeting
        if (rt.target == TargetRule.Self) return caster;
        if (clicked != null) return clicked;
        if (aoeTargets != null && aoeTargets.Count > 0) return aoeTargets[0];
        return null;
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

    public int PreviewDamage(SkillSO skill, CombatActor caster, CombatActor target, int dieValue)
    {
        if (skill == null) return 0;
        return PreviewDamage(SkillRuntime.FromSkill(skill), caster, target, dieValue);
    }

    public AttackPreview BuildAttackPreview(SkillRuntime rt, CombatActor caster, CombatActor target, int dieValue)
    {
        AttackPreview preview = new AttackPreview
        {
            effectiveDieValue = Mathf.Max(0, dieValue),
            baseDamage = 0,
            bonusDamage = 0,
            finalDamage = 0,
            canDealDamage = false
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

        if (rt.canUseMarkMultiplier && target != null && target.status != null && target.status.marked)
        {
            float mul = (rt.element == ElementType.Lightning) ? 2f : 1.25f;
            if (ps != null && rt.element == ElementType.Lightning)
                mul += ps.GetLightningVsMarkMultiplierAdd();
            dmg = Mathf.FloorToInt(dmg * mul);
        }

        if (rt.group == DamageGroup.Sunder && rt.sunderBonusIfTargetHasGuard && targetHasGuard)
            dmg = Mathf.FloorToInt(dmg * Mathf.Max(0f, rt.sunderGuardDamageMultiplier));

        if (rt.element == ElementType.Fire && rt.consumesBurn && target != null && target.status != null)
        {
            int burnStacks = target.status.burnStacks;
            if (burnStacks > 0)
            {
                float burnMul = (ps != null) ? ps.GetBurnConsumeMultiplier() : 1f;
                int add = Mathf.FloorToInt(burnStacks * Mathf.Max(0, rt.burnDamagePerStack) * Mathf.Max(0f, burnMul));
                preview.bonusDamage += add;
                dmg += add;
            }
        }

        bool attemptedDamage = preview.baseDamage > 0 || preview.bonusDamage > 0;
        if (attemptedDamage && dmg < 1)
            dmg = 1;

        preview.finalDamage = Mathf.Max(0, dmg);
        preview.canDealDamage = preview.finalDamage > 0;
        return preview;
    }

    private void ApplyAttackToTargets(SkillRuntime rt, CombatActor caster, IReadOnlyList<CombatActor> targets, int dieValue)
    {
        for (int i = 0; i < targets.Count; i++)
        {
            var t = targets[i];
            if (t == null || t.IsDead) continue;
            ApplyAttack(rt, caster, t, dieValue); // ignore result (AoE popup optional)
        }
    }

    private CombatActor.DamageResult ApplyAttack(SkillRuntime rt, CombatActor caster, CombatActor target, int dieValue)
    {
        if (rt == null || target == null) return default;

        PassiveSystem ps = null;
        if (caster != null) ps = caster.GetComponent<PassiveSystem>();

        bool hadGuardBeforeHit = target.guardPool > 0;
        AttackPreview preview = BuildAttackPreview(rt, caster, target, dieValue);

        var info = new DamageInfo
        {
            group = rt.group,
            element = rt.element,
            bypassGuard = rt.bypassGuard,
            clearsGuard = rt.clearsGuard,
            canUseMarkMultiplier = rt.canUseMarkMultiplier,
            isDamage = preview.finalDamage > 0
        };

        if (rt.element == ElementType.Fire && rt.consumesBurn && target.status != null && target.status.burnStacks > 0)
        {
            target.status.burnStacks = 0;
            target.status.burnTurns = 0;
        }

        if (Debug.isDebugBuild)
        {
            bool hasPS = ps != null;
            Debug.Log($"[EXEC] ApplyAttack rt={rt.kind}/{rt.group}/{rt.element} die={dieValue} base={preview.baseDamage} bonus={preview.bonusDamage} finalDmg={preview.finalDamage} hadGuard={hadGuardBeforeHit} hasPassiveSystem={hasPS}", this);
        }

        var dmgResult = target.TakeDamageDetailed(preview.finalDamage, bypassGuard: info.bypassGuard);

        if (info.isDamage && rt.element == ElementType.Ice && target.status != null)
        {
            bool isFrozen = target.status.frozen;
            bool isChilled = target.status.chilledTurns > 0;

            if (isFrozen || isChilled)
            {
                int dealt = dmgResult.hpLost + dmgResult.blocked;
                int guardGain = Mathf.FloorToInt(dealt * 0.45f);
                if (guardGain > 0) caster.AddGuard(guardGain);
                caster.GainFocus(1);
            }
        }

        if (info.clearsGuard) target.guardPool = 0;

        if (target.status != null && caster != null)
        {
            int reward = target.status.OnHitByDamageReturnFocusReward(ref info);

            if (reward > 0 && ps != null)
                reward += ps.GetFreezeBreakFocusBonusAdd();

            if (reward != 0) caster.GainFocus(reward);
        }

        ApplyStatusesAfterHit(rt, target);

        return dmgResult;
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

    private static float ComputePassiveDamageMultiplier_Reflection(CombatActor caster, SkillRuntime rt, CombatActor target)
    {
        if (caster == null || rt == null) return 1f;

        Component ps = caster.GetComponent("PassiveSystem");
        if (ps == null) return 1f;

        float pct = 0f;

        try
        {
            var t = ps.GetType();

            // try fields: equipped / equippedPassives / passives
            System.Reflection.FieldInfo f =
                t.GetField("equipped") ?? t.GetField("equippedPassives") ?? t.GetField("passives");

            object listObj = f != null ? f.GetValue(ps) : null;

            // try properties if no field
            if (listObj == null)
            {
                var p = t.GetProperty("equipped") ?? t.GetProperty("equippedPassives") ?? t.GetProperty("passives");
                if (p != null) listObj = p.GetValue(ps);
            }

            var enumerable = listObj as System.Collections.IEnumerable;
            if (enumerable == null) return 1f;

            foreach (var it in enumerable)
            {
                if (it is not SkillPassiveSO passive || passive.effects == null) continue;

                for (int i = 0; i < passive.effects.Count; i++)
                {
                    var e = passive.effects[i];
                    if (e == null) continue;

                    switch (e.id)
                    {
                        case PassiveEffectId.DamagePercentAll:
                            pct += e.valueF;
                            break;

                        case PassiveEffectId.DamagePercentByElement:
                            if (e.element == rt.element) pct += e.valueF;
                            break;

                        case PassiveEffectId.ConditionalDamagePercent:
                            {
                                bool atkOk =
                                    e.attackType == PassiveAttackType.Any ||
                                    (e.attackType == PassiveAttackType.Melee && rt.range == RangeType.Melee) ||
                                    (e.attackType == PassiveAttackType.Ranged && rt.range == RangeType.Ranged);

                                bool rowOk = true;
                                if (target != null)
                                {
                                    rowOk =
                                        e.targetRow == PassiveTargetRow.Any ||
                                        (e.targetRow == PassiveTargetRow.Front && target.row == CombatActor.RowTag.Front) ||
                                        (e.targetRow == PassiveTargetRow.Back && target.row == CombatActor.RowTag.Back);
                                }

                                if (atkOk && rowOk) pct += e.valueF;
                                break;
                            }
                    }
                }
            }
        }
        catch { }

        return 1f + pct;
    }

    private static float GetPassiveBurnConsumeMul_Reflection(CombatActor caster)
    {
        if (caster == null) return 1f;
        Component ps = caster.GetComponent("PassiveSystem");
        if (ps == null) return 1f;

        float mul = 1f;

        try
        {
            var t = ps.GetType();
            var f = t.GetField("equipped") ?? t.GetField("equippedPassives") ?? t.GetField("passives");
            object listObj = f != null ? f.GetValue(ps) : null;
            if (listObj == null)
            {
                var p = t.GetProperty("equipped") ?? t.GetProperty("equippedPassives") ?? t.GetProperty("passives");
                if (p != null) listObj = p.GetValue(ps);
            }

            var enumerable = listObj as System.Collections.IEnumerable;
            if (enumerable == null) return 1f;

            foreach (var it in enumerable)
            {
                if (it is not SkillPassiveSO passive || passive.effects == null) continue;
                for (int i = 0; i < passive.effects.Count; i++)
                {
                    var e = passive.effects[i];
                    if (e == null) continue;
                    if (e.id == PassiveEffectId.BurnConsumeDamageMultiplier)
                        mul *= Mathf.Max(0f, e.valueF);
                }
            }
        }
        catch { }

        return mul;
    }

    private static float GetPassiveLightningVsMarkAdd_Reflection(CombatActor caster)
    {
        if (caster == null) return 0f;
        Component ps = caster.GetComponent("PassiveSystem");
        if (ps == null) return 0f;

        float add = 0f;

        try
        {
            var t = ps.GetType();
            var f = t.GetField("equipped") ?? t.GetField("equippedPassives") ?? t.GetField("passives");
            object listObj = f != null ? f.GetValue(ps) : null;
            if (listObj == null)
            {
                var p = t.GetProperty("equipped") ?? t.GetProperty("equippedPassives") ?? t.GetProperty("passives");
                if (p != null) listObj = p.GetValue(ps);
            }

            var enumerable = listObj as System.Collections.IEnumerable;
            if (enumerable == null) return 0f;

            foreach (var it in enumerable)
            {
                if (it is not SkillPassiveSO passive || passive.effects == null) continue;
                for (int i = 0; i < passive.effects.Count; i++)
                {
                    var e = passive.effects[i];
                    if (e == null) continue;
                    if (e.id == PassiveEffectId.LightningVsMarkMultiplierAdd)
                        add += e.valueF;
                }
            }
        }
        catch { }

        return add;
    }

    private static int GetPassiveFreezeBreakFocusBonusAdd_Reflection(CombatActor caster)
    {
        if (caster == null) return 0;
        Component ps = caster.GetComponent("PassiveSystem");
        if (ps == null) return 0;

        int add = 0;

        try
        {
            var t = ps.GetType();
            var f = t.GetField("equipped") ?? t.GetField("equippedPassives") ?? t.GetField("passives");
            object listObj = f != null ? f.GetValue(ps) : null;
            if (listObj == null)
            {
                var p = t.GetProperty("equipped") ?? t.GetProperty("equippedPassives") ?? t.GetProperty("passives");
                if (p != null) listObj = p.GetValue(ps);
            }

            var enumerable = listObj as System.Collections.IEnumerable;
            if (enumerable == null) return 0;

            foreach (var it in enumerable)
            {
                if (it is not SkillPassiveSO passive || passive.effects == null) continue;
                for (int i = 0; i < passive.effects.Count; i++)
                {
                    var e = passive.effects[i];
                    if (e == null) continue;
                    if (e.id == PassiveEffectId.FreezeBreakFocusBonusAdd)
                        add += e.valueI;
                }
            }
        }
        catch { }

        return add;
    }


}
