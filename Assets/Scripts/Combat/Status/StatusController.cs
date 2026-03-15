using System;
using System.Collections.Generic;
using UnityEngine;
public class StatusController : MonoBehaviour
{
    // Burn: stacks + duration
    public int burnStacks;
    public int burnTurns;

    // Mark: tồn tại tới hit kế tiếp
    public bool marked;

    // Bleed: stack giảm dần (-1 mỗi cuối turn), đầu turn mất HP = stack
    public int bleedStacks;

    // Freeze: skip 1 turn
    public bool frozen;

    // Chilled: tồn tại 2 turn sau khi hết Freeze, không stack
    public int chilledTurns;
    private bool _chilledJustAppliedThisTurn;

    // Stagger: mở sau khi Guard bị phá, hit direct kế tiếp mới ăn x1.2
    public bool staggered;

    // -------------------- NEW: Buff/Debuff/Ailment layer (non-breaking) --------------------

    private readonly List<StatusPendingBuffDebuff> _pending = new List<StatusPendingBuffDebuff>(8);
    private readonly List<StatusActiveBuffDebuff> _active = new List<StatusActiveBuffDebuff>(8);
    private readonly List<StatusPendingAilment> _pendingAilments = new List<StatusPendingAilment>(4);

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
        => StatusStateUtility.ClearAll(this, _pending, _active, _pendingAilments, debugLog);

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
                    StatusBuffDebuffUtility.ApplyBuffDebuffEntryNow(this, _active, e, applier, rolledValue);
                }
                else
                {
                    _pending.Add(new StatusPendingBuffDebuff
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
                StatusAilmentUtility.TryApplyAilment(this, type, dur, applier, rolledValue, maxFaceValue, mult, debugForceAilmentChance100, debugLog);
            }
            else
            {
                _pendingAilments.Add(new StatusPendingAilment
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
        => StatusBuffDebuffUtility.GetAllDiceDelta(_active);

    public int GetParityFocusDelta(int diceTotal)
        => StatusBuffDebuffUtility.GetParityFocusDelta(_active, diceTotal);

    public bool HasSlotCollapse()
        => StatusBuffDebuffUtility.HasSlotCollapse(_active);

    /// <summary>
    /// Outgoing damage multiplier from buffs/debuffs.
    /// Uses the strongest multiplier currently active (stable default).
    /// </summary>
    public float GetOutgoingDamageMultiplier()
        => StatusBuffDebuffUtility.GetOutgoingDamageMultiplier(_active);

    /// <summary>
    /// TurnManager should call this at the end of THIS actor's turn.
    /// It decrements durations for buff/debuff + ailment.
    /// </summary>
    public void OnOwnerTurnEnded()
        => StatusStateUtility.OnOwnerTurnEnded(this, _active);

    public void ApplyBurn(int addStacks, int refreshTurns)
        => StatusStateUtility.ApplyBurn(this, addStacks, refreshTurns);

    public void ApplyMark() => marked = true;

    public void ApplyBleed(int stacks)
        => StatusStateUtility.ApplyBleed(this, stacks);

    public void ApplyFreeze()
        => StatusStateUtility.ApplyFreeze(this);

    public void ApplyStagger()
    {
        staggered = true;
    }

    public void ClearStagger()
    {
        staggered = false;
    }

    // gọi ở "đầu lượt của mục tiêu" (Bleed tick + giảm duration Burn)
    public int TickStartOfTurnDamage()
        => StatusStateUtility.TickStartOfTurnDamage(this);

    // ✅ Needed by TurnManager (skip 1 lượt rồi tự gỡ Freeze)
    public int OnTurnStarted(bool consumeFreezeToSkipTurn, out bool skipTurn)
    {
        int dot = StatusStateUtility.OnTurnStarted(this, consumeFreezeToSkipTurn, out skipTurn);
        ProcessPendingAtTurnStart();
        return dot;
    }

    // ✅ NEW: Consume rules + trả focus reward cho attacker (Ice phá Freeze -> +1 Focus)
    public int OnHitByDamageReturnFocusReward(ref DamageInfo info)
        => StatusStateUtility.OnHitByDamageReturnFocusReward(this, ref info);

    // Back-compat: code cũ vẫn gọi được
    public void OnHitByDamage(ref DamageInfo info)
    {
        OnHitByDamageReturnFocusReward(ref info);
    }

    // Chance dạng 0..1
    public bool TryApplyFreeze(float chance01)
        => StatusStateUtility.TryApplyFreeze(this, chance01);

    // Chance dạng % 0..100
    public bool TryApplyFreeze(int chancePercent)
        => StatusStateUtility.TryApplyFreeze(this, chancePercent);

    private void ProcessPendingAtTurnStart()
        => StatusStateUtility.ProcessPendingAtTurnStart(this, _pending, _active, _pendingAilments, debugForceAilmentChance100, debugLog);

    internal void SetAilmentCleared()
    {
        _hasAilment = false;
        _ailmentType = default;
        _ailmentTurnsLeft = 0;
    }

    internal void TickAilmentDuration()
    {
        if (_hasAilment && _ailmentTurnsLeft > 0)
        {
            _ailmentTurnsLeft--;
            if (_ailmentTurnsLeft <= 0)
                ClearAilment();
        }
    }

    internal bool GetChilledJustApplied() => _chilledJustAppliedThisTurn;
    internal void SetChilledJustApplied(bool value) => _chilledJustAppliedThisTurn = value;

    public void SetAilmentState(AilmentType type, int turns)
    {
        _hasAilment = true;
        _ailmentType = type;
        _ailmentTurnsLeft = Mathf.Max(1, turns);
    }
}
