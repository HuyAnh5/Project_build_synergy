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
        if (rt == null || target == null) return;

        int baseDmg = rt.CalculateDamage(dieValue);

        // ---- multipliers (order is deterministic; keep legacy behaviors intact) ----
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

        // --- keep legacy logic (guard, mark, sunder...) ---
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

        int dmg = baseDmg;

        // Status buff/debuff outgoing multiplier
        if (!Mathf.Approximately(statusOutMul, 1f))
            dmg = Mathf.CeilToInt(dmg * statusOutMul);

        // Passive outgoing multiplier
        if (!Mathf.Approximately(passiveOutMul, 1f))
            dmg = Mathf.CeilToInt(dmg * passiveOutMul);

        // Ailment vulnerability (Sleep)
        if (!Mathf.Approximately(sleepMul, 1f))
            dmg = Mathf.CeilToInt(dmg * sleepMul);

        // Mark multiplier (legacy), with optional passive add for Lightning vs Mark
        if (rt.canUseMarkMultiplier && target.status != null && target.status.marked)
        {
            float mul = (rt.element == ElementType.Lightning) ? 2f : 1.25f;
            if (ps != null && rt.element == ElementType.Lightning)
                mul += ps.GetLightningVsMarkMultiplierAdd();
            dmg = Mathf.RoundToInt(dmg * mul);
        }

        // Sunder bonus vs Guard (legacy)
        if (rt.group == DamageGroup.Sunder && rt.sunderBonusIfTargetHasGuard && hadGuardBeforeHit)
        {
            dmg = Mathf.RoundToInt(dmg * Mathf.Max(0f, rt.sunderGuardDamageMultiplier));
        }

        // Burn consume (legacy) + passive multiplier hook
        if (rt.element == ElementType.Fire && rt.consumesBurn && target.status != null)
        {
            int b = target.status.burnStacks;
            if (b > 0)
            {
                float burnMul = (ps != null) ? ps.GetBurnConsumeMultiplier() : 1f;
                int add = Mathf.RoundToInt(b * Mathf.Max(0, rt.burnDamagePerStack) * Mathf.Max(0f, burnMul));
                dmg += add;
                target.status.burnStacks = 0;
                target.status.burnTurns = 0;
            }
        }

        info.isDamage = (dmg > 0);

        if (Debug.isDebugBuild)
        {
            bool hasPS = ps != null;
            Debug.Log($"[EXEC] ApplyAttack rt={rt.kind}/{rt.group}/{rt.element} die={dieValue} base={baseDmg} statusOutMul={statusOutMul:0.###} passiveOutMul={passiveOutMul:0.###} sleepMul={sleepMul:0.###} finalDmg={dmg} hasPassiveSystem={hasPS}", this);
        }

        target.TakeDamage(dmg, bypassGuard: info.bypassGuard);
        if (info.clearsGuard) target.guardPool = 0;

        if (target.status != null && caster != null)
        {
            int reward = target.status.OnHitByDamageReturnFocusReward(ref info);

            // Passive bonus: break Freeze -> +extra Focus
            if (reward > 0 && ps != null)
                reward += ps.GetFreezeBreakFocusBonusAdd();

            if (reward != 0) caster.GainFocus(reward);
        }

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
