using System;
using System.Collections.Generic;
using UnityEngine;
public class StatusController : MonoBehaviour
{
    [Serializable]
    public class BurnBatchState
    {
        public int stacks;
        public int turnsRemaining;
    }

    [Header("Core Status")]
    // Burn: total visible stacks + longest remaining batch duration for debug/back-compat
    public int burnStacks;
    public int burnTurns;
    [SerializeField] private List<BurnBatchState> _burnBatches = new List<BurnBatchState>(4);

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

    [Header("Named Runtime Hooks")]
    public int emberWeaponTurns;
    public int emberWeaponBonusDamage = 1;
    public bool emberWeaponBurnEqualsDamage = true;
    [HideInInspector] public bool emberWeaponBurnOnCritOnly;
    [HideInInspector] public int emberWeaponBurnTurns = 3;
    [HideInInspector] public int cinderbrandTurns;
    [HideInInspector] public int cinderbrandBonusPerBurn = 1;
    [HideInInspector] public int repeatFirstSkillNextTurnPending;
    [HideInInspector] public int repeatFirstSkillReadyExtraCasts;
    [HideInInspector] public int nextSkillAddedValue;
    [HideInInspector] public int nextSkillAddedValueCharges;

    [HideInInspector, SerializeField] private bool _hasAilment;
    [HideInInspector, SerializeField] private AilmentType _ailmentType;
    [HideInInspector, SerializeField] private int _ailmentTurnsLeft;

    [Header("Debug")]
    [Tooltip("Extra logs for tracing status state.")]
    [SerializeField] private bool debugLog = true;

    // Convenience for UI/others
    public bool HasAnyAilment => _hasAilment && _ailmentTurnsLeft > 0;
    public AilmentType CurrentAilment => HasAnyAilment ? _ailmentType : default;
    public int CurrentAilmentTurnsLeft => HasAnyAilment ? _ailmentTurnsLeft : 0;

    // ✅ Needed by CombatActor (reset between fights / retry)
    public void ClearAll()
        => StatusStateUtility.ClearAll(this, debugLog);

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

    public int GetAllDiceDelta()
        => 0;

    public int GetParityFocusDelta(int diceTotal)
        => 0;

    public bool HasSlotCollapse()
        => false;

    public float GetOutgoingDamageMultiplier()
        => 1f;

    public void GrantRepeatFirstSkillNextTurn(int extraCasts)
    {
        repeatFirstSkillNextTurnPending += Mathf.Max(0, extraCasts);
    }

    public int ConsumeRepeatFirstSkillReady()
    {
        int casts = Mathf.Max(0, repeatFirstSkillReadyExtraCasts);
        repeatFirstSkillReadyExtraCasts = 0;
        return casts;
    }

    public int PeekRepeatFirstSkillReady()
        => Mathf.Max(0, repeatFirstSkillReadyExtraCasts);

    public void GrantNextSkillAddedValue(int amount, int charges = 1)
    {
        int safeAmount = Mathf.Max(0, amount);
        int safeCharges = Mathf.Max(1, charges);
        if (safeAmount <= 0)
            return;

        nextSkillAddedValue += safeAmount;
        nextSkillAddedValueCharges += safeCharges;
    }

    public int PeekNextSkillAddedValue()
        => nextSkillAddedValueCharges > 0 ? Mathf.Max(0, nextSkillAddedValue) : 0;

    public void ConsumeNextSkillAddedValue()
    {
        if (nextSkillAddedValueCharges <= 0)
            return;

        nextSkillAddedValueCharges--;
        if (nextSkillAddedValueCharges <= 0)
        {
            nextSkillAddedValueCharges = 0;
            nextSkillAddedValue = 0;
        }
    }

    public void GrantEmberWeapon(int turns, int bonusDamage, bool burnEqualsDamage, bool burnOnCritOnly, int burnTurns)
    {
        emberWeaponTurns = Mathf.Max(emberWeaponTurns, Mathf.Max(1, turns));
        emberWeaponBonusDamage = Mathf.Max(0, bonusDamage);
        emberWeaponBurnEqualsDamage = burnEqualsDamage;
        emberWeaponBurnOnCritOnly = burnOnCritOnly;
        emberWeaponBurnTurns = Mathf.Max(1, burnTurns);
    }

    /// <summary>
    /// TurnManager should call this at the end of THIS actor's turn.
    /// It decrements durations for buff/debuff + ailment.
    /// </summary>
    public void OnOwnerTurnEnded()
        => StatusStateUtility.OnOwnerTurnEnded(this);

    public void ApplyBurn(int addStacks, int refreshTurns)
        => StatusStateUtility.ApplyBurn(this, addStacks, refreshTurns);

    public int ConsumeAllBurn()
        => StatusStateUtility.ConsumeAllBurn(this);

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
        if (repeatFirstSkillNextTurnPending > 0)
        {
            repeatFirstSkillReadyExtraCasts += repeatFirstSkillNextTurnPending;
            repeatFirstSkillNextTurnPending = 0;
        }

        int dot = StatusStateUtility.OnTurnStarted(this, consumeFreezeToSkipTurn, out skipTurn);
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
    internal List<BurnBatchState> GetBurnBatches() => _burnBatches;
    internal void SyncBurnDisplay(int stacks, int maxTurns)
    {
        burnStacks = Mathf.Max(0, stacks);
        burnTurns = Mathf.Max(0, maxTurns);
    }

    public void SetAilmentState(AilmentType type, int turns)
    {
        _hasAilment = true;
        _ailmentType = type;
        _ailmentTurnsLeft = Mathf.Max(1, turns);
    }
}
