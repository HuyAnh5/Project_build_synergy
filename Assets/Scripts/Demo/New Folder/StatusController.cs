using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class StatusController : MonoBehaviour
{
    // Burn: stacks + duration
    public int burnStacks;
    public int burnTurns;

    // Mark: tồn tại tới hit kế tiếp
    public bool marked;

    // Bleed: stack = số turn còn lại
    public int bleedTurns;

    // Freeze: nếu proc thì skip 1 lượt (duration 1 turn)
    public bool frozen;

    // -------------------- NEW: Buff/Debuff/Ailment layer (non-breaking) --------------------

    [Serializable]
    private class PendingBuffDebuff
    {
        public BuffDebuffEffectEntry entry;
        public int delayTurns;
        public CombatActor applier;
        public int rolledValue;
        public int maxFaceValue;
    }

    [Serializable]
    private class ActiveBuffDebuff
    {
        public BuffDebuffEffectEntry entry;
        public int remainingTurns;
        public CombatActor applier;
    }

    [Serializable]
    private class PendingAilment
    {
        public AilmentType type;
        public int delayTurns;
        public int durationTurns;
        public float chanceMultiplier;
        public CombatActor applier;
        public int rolledValue;
        public int maxFaceValue;
    }

    private readonly List<PendingBuffDebuff> _pending = new List<PendingBuffDebuff>(8);
    private readonly List<ActiveBuffDebuff> _active = new List<ActiveBuffDebuff>(8);
    private readonly List<PendingAilment> _pendingAilments = new List<PendingAilment>(4);

    [SerializeField] private bool _hasAilment;
    [SerializeField] private AilmentType _ailmentType;
    [SerializeField] private int _ailmentTurnsLeft;

    [Header("Debug")]
    [Tooltip("When ON: any SkillBuffDebuffSO ailment will apply 100% regardless of dice/chance calculator.")]
    [SerializeField] private bool debugForceAilmentChance100 = true;

    [Tooltip("Extra logs for tracing buff/debuff/ailment pipeline.")]
    [SerializeField] private bool debugLog = true;

    // Convenience for UI/others
    public bool HasAnyAilment => _hasAilment && _ailmentTurnsLeft > 0;
    public AilmentType CurrentAilment => HasAnyAilment ? _ailmentType : default;
    public int CurrentAilmentTurnsLeft => HasAnyAilment ? _ailmentTurnsLeft : 0;

    // ✅ Needed by CombatActor (reset between fights / retry)
    public void ClearAll()
    {
        burnStacks = 0;
        burnTurns = 0;
        marked = false;
        bleedTurns = 0;
        frozen = false;

        _pending.Clear();
        _active.Clear();
        _pendingAilments.Clear();
        _hasAilment = false;
        _ailmentType = default;
        _ailmentTurnsLeft = 0;

        if (debugLog)
            Debug.Log($"[STATUS] ClearAll -> {name}", this);
    }

    /// <summary>
    /// NEW: Apply a SkillBuffDebuffSO onto THIS actor.
    /// - Immediate effects apply instantly (delay 0)
    /// - Delayed effects are processed on OnTurnStarted (delay 1 => next start, delay 2 => start after 2 turns)
    /// Duration ticking is exposed via OnOwnerTurnEnded (TurnManager should call it).
    /// </summary>
    public void ApplyBuffDebuffSkill(SkillBuffDebuffSO skill, CombatActor applier, int rolledValue, int maxFaceValue)
    {
        if (skill == null) return;

        int delay = Mathf.Max(0, skill.applyDelayTurns);

        // Effects
        if (skill.effects != null)
        {
            for (int i = 0; i < skill.effects.Count; i++)
            {
                var e = skill.effects[i];
                if (e == null) continue;

                if (delay <= 0)
                {
                    ApplyBuffDebuffEntryNow(e, applier, rolledValue);
                }
                else
                {
                    _pending.Add(new PendingBuffDebuff
                    {
                        entry = e,
                        delayTurns = delay,
                        applier = applier,
                        rolledValue = rolledValue,
                        maxFaceValue = Mathf.Max(1, maxFaceValue)
                    });
                }
            }
        }

        // Ailment (optional) - correct schema: skill.applyAilment + skill.ailment (AilmentConfig)
        if (skill.applyAilment && skill.ailment != null)
        {
            var a = skill.ailment;
            var type = a.ailment;
            var dur = Mathf.Max(1, a.durationTurns);
            var mult = Mathf.Max(0f, a.chanceTuningMultiplier);

            if (debugLog)
                Debug.Log($"[STATUS] ApplyBuffDebuffSkill(ailment) target={name} skill={skill.name} type={type} dur={dur} delay={delay} force100={debugForceAilmentChance100} rolled={rolledValue} maxFace={maxFaceValue} mult={mult}", this);

            if (delay <= 0)
            {
                TryApplyAilment(type, dur, applier, rolledValue, maxFaceValue, mult);
            }
            else
            {
                _pendingAilments.Add(new PendingAilment
                {
                    type = type,
                    delayTurns = delay,
                    durationTurns = dur,
                    chanceMultiplier = mult,
                    applier = applier,
                    rolledValue = rolledValue,
                    maxFaceValue = Mathf.Max(1, maxFaceValue)
                });
            }
        }
    }

    public bool HasAilment(out AilmentType type, out int turnsLeft)
    {
        type = _ailmentType;
        turnsLeft = _ailmentTurnsLeft;
        return _hasAilment && _ailmentTurnsLeft > 0;
    }

    public bool HasAilmentType(AilmentType type)
    {
        return _hasAilment && _ailmentTurnsLeft > 0 && _ailmentType.Equals(type);
    }

    public void ClearAilment()
    {
        _hasAilment = false;
        _ailmentType = default;
        _ailmentTurnsLeft = 0;

        if (debugLog)
            Debug.Log($"[STATUS] ClearAilment -> {name}", this);
    }

    /// <summary>
    /// NEW: dice-layer delta (sum of all active DiceAllDelta entries).
    /// TurnManager / DiceSlotRig will use this when resolving final die values.
    /// </summary>
    public int GetAllDiceDelta()
    {
        int sum = 0;
        for (int i = 0; i < _active.Count; i++)
        {
            var a = _active[i];
            if (a == null || a.entry == null) continue;
            if (a.entry.id == BuffDebuffEffectId.DiceAllDelta)
                sum += a.entry.GetDiceAllDelta();
        }
        return sum;
    }

    public int GetParityFocusDelta(int diceTotal)
    {
        bool even = (diceTotal % 2) == 0;
        int sum = 0;
        for (int i = 0; i < _active.Count; i++)
        {
            var a = _active[i];
            if (a == null || a.entry == null) continue;
            if (a.entry.id != BuffDebuffEffectId.ParityFocusDelta) continue;
            sum += even ? a.entry.parityEvenDelta : a.entry.parityOddDelta;
        }
        return sum;
    }

    public bool HasSlotCollapse()
    {
        for (int i = 0; i < _active.Count; i++)
        {
            var a = _active[i];
            if (a == null || a.entry == null) continue;
            if (a.entry.id == BuffDebuffEffectId.SlotCollapse) return true;
        }
        return false;
    }

    /// <summary>
    /// Outgoing damage multiplier from buffs/debuffs.
    /// Uses the strongest multiplier currently active (stable default).
    /// </summary>
    public float GetOutgoingDamageMultiplier()
    {
        float best = 1f;
        for (int i = 0; i < _active.Count; i++)
        {
            var a = _active[i];
            if (a == null || a.entry == null) continue;
            if (a.entry.id != BuffDebuffEffectId.DamageMultiplier) continue;
            best = Mathf.Max(best, a.entry.GetDamageMultiplier());
        }
        return best;
    }

    /// <summary>
    /// TurnManager should call this at the end of THIS actor's turn.
    /// It decrements durations for buff/debuff + ailment.
    /// </summary>
    public void OnOwnerTurnEnded()
    {
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            var a = _active[i];
            if (a == null || a.entry == null) { _active.RemoveAt(i); continue; }
            a.remainingTurns--;
            if (a.remainingTurns <= 0) _active.RemoveAt(i);
        }

        if (_hasAilment && _ailmentTurnsLeft > 0)
        {
            _ailmentTurnsLeft--;
            if (_ailmentTurnsLeft <= 0) ClearAilment();
        }
    }

    public void ApplyBurn(int addStacks, int refreshTurns)
    {
        burnStacks += Mathf.Max(0, addStacks);
        burnTurns = Mathf.Max(burnTurns, refreshTurns);
    }

    public void ApplyMark() => marked = true;

    public void ApplyBleed(int turns)
    {
        if (turns <= 0) return;
        bleedTurns += turns;
    }

    public void ApplyFreeze() => frozen = true;

    // gọi ở "đầu lượt của mục tiêu" (Bleed tick + giảm duration Burn)
    public int TickStartOfTurnDamage()
    {
        int dot = 0;

        // Bleed: -1 HP mỗi đầu lượt của target
        if (bleedTurns > 0)
        {
            dot += 1;
            bleedTurns -= 1;
        }

        // Burn không DoT nhưng giảm turn; hết turn thì mất stacks
        if (burnTurns > 0) burnTurns -= 1;
        if (burnTurns <= 0) burnStacks = 0;

        return dot;
    }

    // ✅ Needed by TurnManager (skip 1 lượt rồi tự gỡ Freeze)
    public int OnTurnStarted(bool consumeFreezeToSkipTurn, out bool skipTurn)
    {
        skipTurn = false;

        int dot = TickStartOfTurnDamage();

        if (consumeFreezeToSkipTurn && frozen)
        {
            frozen = false;
            skipTurn = true; // skip đúng 1 lượt của chính target
        }

        // NEW: delayed buff/debuff + ailment applications
        ProcessPendingAtTurnStart();

        return dot;
    }

    // ✅ NEW: Consume rules + trả focus reward cho attacker (Ice phá Freeze -> +1 Focus)
    public int OnHitByDamageReturnFocusReward(ref DamageInfo info)
    {
        // NEW: minimal ailment break-on-damage behavior
        if (_hasAilment && _ailmentTurnsLeft > 0)
        {
            if (_ailmentType == AilmentType.Sleep)
                ClearAilment();
        }

        // Effect skills KHÔNG consume status + KHÔNG reward
        if (info.group == DamageGroup.Effect) return 0;

        int reward = 0;

        // Mark: bị xóa bởi bất kỳ damage
        if (marked && info.isDamage)
            marked = false;

        // Burn: bị xóa khi bị Fire damage
        if (burnStacks > 0 && info.element == ElementType.Fire && info.isDamage)
        {
            burnStacks = 0;
            burnTurns = 0;
        }

        // Freeze: bị xóa khi bị Ice damage; phá freeze thưởng focus +1
        if (frozen && info.element == ElementType.Ice && info.isDamage)
        {
            frozen = false;
            reward += 1;
        }

        return reward;
    }

    // Back-compat: code cũ vẫn gọi được
    public void OnHitByDamage(ref DamageInfo info)
    {
        OnHitByDamageReturnFocusReward(ref info);
    }

    // Chance dạng 0..1
    public bool TryApplyFreeze(float chance01)
    {
        if (chance01 <= 0f) return false;

        if (chance01 >= 1f)
        {
            ApplyFreeze();
            return true;
        }

        if (Random.value < chance01)
        {
            ApplyFreeze();
            return true;
        }

        return false;
    }

    // Chance dạng % 0..100
    public bool TryApplyFreeze(int chancePercent)
    {
        if (chancePercent <= 0) return false;

        if (chancePercent >= 100)
        {
            ApplyFreeze();
            return true;
        }

        if (Random.Range(0, 100) < chancePercent)
        {
            ApplyFreeze();
            return true;
        }

        return false;
    }

    private void ProcessPendingAtTurnStart()
    {
        // Buff/debuff entries
        for (int i = _pending.Count - 1; i >= 0; i--)
        {
            var p = _pending[i];
            if (p == null || p.entry == null) { _pending.RemoveAt(i); continue; }

            p.delayTurns--;
            if (p.delayTurns <= 0)
            {
                ApplyBuffDebuffEntryNow(p.entry, p.applier, p.rolledValue);
                _pending.RemoveAt(i);
            }
        }

        // Ailments
        for (int i = _pendingAilments.Count - 1; i >= 0; i--)
        {
            var p = _pendingAilments[i];
            if (p == null) { _pendingAilments.RemoveAt(i); continue; }

            p.delayTurns--;
            if (p.delayTurns <= 0)
            {
                TryApplyAilment(p.type, p.durationTurns, p.applier, p.rolledValue, p.maxFaceValue, p.chanceMultiplier);
                _pendingAilments.RemoveAt(i);
            }
        }
    }

    private void ApplyBuffDebuffEntryNow(BuffDebuffEffectEntry entry, CombatActor applier, int rolledValue)
    {
        if (entry == null) return;

        var actor = GetComponent<CombatActor>();

        switch (entry.id)
        {
            case BuffDebuffEffectId.HealFlat:
                if (actor != null) actor.Heal(entry.GetHealAmount());
                break;

            case BuffDebuffEffectId.HealByDiceSum:
                if (actor != null) actor.Heal(Mathf.Max(0, rolledValue));
                break;

            case BuffDebuffEffectId.FocusDelayed:
                if (actor != null) actor.GainFocus(entry.GetFocusAmount());
                break;

            case BuffDebuffEffectId.DamageMultiplier:
            case BuffDebuffEffectId.DiceAllDelta:
            case BuffDebuffEffectId.ParityFocusDelta:
            case BuffDebuffEffectId.SlotCollapse:
                {
                    int dur = Mathf.Max(0, entry.durationTurns);
                    if (dur <= 0) break;
                    _active.Add(new ActiveBuffDebuff { entry = entry, remainingTurns = dur, applier = applier });
                }
                break;
        }
    }

    private void TryApplyAilment(AilmentType type, int durationTurns, CombatActor applier, int rolledValue, int maxFaceValue, float chanceMultiplier)
    {
        var actor = GetComponent<CombatActor>();
        if (actor == null) return;

        float chance;
        if (debugForceAilmentChance100)
        {
            chance = 1f;
        }
        else
        {
            chance = AilmentChanceCalculator.ComputeChance(
                attackerIsPlayer: applier != null && applier.isPlayer,
                targetIsPlayer: actor.isPlayer,
                rolledValue: rolledValue,
                maxFaceValue: Mathf.Max(1, maxFaceValue),
                skillTuningMultiplier: Mathf.Max(0f, chanceMultiplier));
        }

        if (debugLog)
            Debug.Log($"[STATUS] TryApplyAilment target={name} type={type} dur={durationTurns} chance={chance:0.###} force100={debugForceAilmentChance100} rolled={rolledValue} maxFace={maxFaceValue}", this);

        if (chance <= 0f) return;

        if (chance >= 1f || Random.value <= chance)
        {
            _hasAilment = true;
            _ailmentType = type;
            _ailmentTurnsLeft = Mathf.Max(1, durationTurns);

            if (debugLog)
                Debug.Log($"[STATUS] APPLY ailment -> target={name} type={_ailmentType} turns={_ailmentTurnsLeft}", this);
        }
        else
        {
            if (debugLog)
                Debug.Log($"[STATUS] FAIL ailment roll -> target={name} type={type}", this);
        }
    }
}
