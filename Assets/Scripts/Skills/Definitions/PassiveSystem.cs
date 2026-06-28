using System.Collections.Generic;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public partial class PassiveSystem : MonoBehaviour
{
    [Tooltip("Equip-only passives currently active on this actor.")]
    public List<SkillPassiveSO> equipped = new List<SkillPassiveSO>();

    [Header("Auto Sync (Prefab-safe)")]
    [Tooltip("If true, this component will automatically find RunInventoryManager in the scene and sync passives from it.")]
    [SerializeField] private bool autoSyncFromRunInventory = true;

    [Tooltip("Optional explicit reference. Leave empty on prefabs; it will be auto-found at runtime.")]
    [SerializeField] private RunInventoryManager runInventory;

    [Tooltip("Optional passive database for random Common passive effects.")]
    [SerializeField] private SkillDatabaseSO skillDatabase;

    private readonly List<SkillPassiveSO> _syncBuffer = new List<SkillPassiveSO>(16);
    private readonly List<SkillPassiveSO> _temporaryCombatPassives = new List<SkillPassiveSO>(4);
    private readonly Dictionary<DiceSpinnerGeneric, int[]> _baseFaceValues = new Dictionary<DiceSpinnerGeneric, int[]>();
    private readonly Dictionary<DiceSpinnerGeneric, int[]> _permanentFaceBonuses = new Dictionary<DiceSpinnerGeneric, int[]>();
    private readonly Dictionary<DiceSpinnerGeneric, int> _combatAllFaceBonuses = new Dictionary<DiceSpinnerGeneric, int>();
    private DiceSlotRig _cachedDiceRig;
    private const float RollThreeHitDelay = 0.15f;

    private bool _bloodCounterAddedValueActive;
    private bool _failDieNextSkillAddedValueActive;
    private bool _lowHpRefillUsedThisCombat;
    private bool _randomCommonPassiveRolledThisCombat;
    private bool _reviveUsedThisCombat;
    private SkillPassiveSO _randomCommonPassivePickedThisCombat;
    private int _failDiceCountdownProgress;
    private int _combatAddedValueBonus;

    partial void OverrideTesterPassiveRuntimeEnabled(SkillPassiveSO passive, ref bool enabled);

    private void Awake()
    {
        TryBindInventory();
        SyncFromInventoryIfPossible();
        Rebuild();
    }

    private void Start()
    {
        if (autoSyncFromRunInventory && runInventory == null)
            StartCoroutine(RetryBindInventoryRoutine());
    }

    private void OnEnable()
    {
        TryBindInventory();
        SyncFromInventoryIfPossible();
        Rebuild();
        PassiveSystemRegistry.Register(this);
    }

    private void OnDisable()
    {
        PassiveSystemRegistry.Unregister(this);
        UnbindInventory();
    }

    private System.Collections.IEnumerator RetryBindInventoryRoutine()
    {
        const int attempts = 25;
        const float wait = 0.2f;

        for (int i = 0; i < attempts && runInventory == null; i++)
        {
            TryBindInventory();
            if (runInventory != null)
                break;
            yield return new WaitForSeconds(wait);
        }

        if (runInventory != null)
        {
            SyncFromInventoryIfPossible();
            Rebuild();
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
            Rebuild();
    }
#endif

    public void Rebuild()
    {
        if (equipped == null)
            equipped = new List<SkillPassiveSO>();
    }

    private void TryBindInventory()
    {
        if (!autoSyncFromRunInventory)
            return;

        if (runInventory == null)
        {
            runInventory = GetComponentInParent<RunInventoryManager>(true);
            if (runInventory == null)
                runInventory = RunInventoryManagerRegistry.Get();
        }

        if (runInventory != null)
        {
            runInventory.InventoryChanged -= OnInventoryChanged;
            runInventory.InventoryChanged += OnInventoryChanged;
        }
    }

    

    private DiceSlotRig GetDiceRig()
    {
        if (runInventory != null && runInventory.DiceRig != null)
            return runInventory.DiceRig;

        if (_cachedDiceRig == null)
            _cachedDiceRig = DiceSlotRigRegistry.Get();

        return _cachedDiceRig;
    }

    private void UnbindInventory()
    {
        if (runInventory != null)
            runInventory.InventoryChanged -= OnInventoryChanged;
    }

    private void OnInventoryChanged()
    {
        SyncFromInventoryIfPossible();
        Rebuild();
    }

    private void SyncFromInventoryIfPossible()
    {
        if (!autoSyncFromRunInventory || runInventory == null)
            return;

        runInventory.FillPassives(_syncBuffer);

        if (equipped == null)
            equipped = new List<SkillPassiveSO>();
        equipped.Clear();
        for (int i = 0; i < _syncBuffer.Count; i++)
            equipped.Add(_syncBuffer[i]);
        for (int i = 0; i < _temporaryCombatPassives.Count; i++)
        {
            SkillPassiveSO passive = _temporaryCombatPassives[i];
            if (passive != null && !equipped.Contains(passive))
                equipped.Add(passive);
        }
    }

    public bool Equip(SkillPassiveSO passive)
    {
        if (passive == null || equipped.Contains(passive))
            return false;

        equipped.Add(passive);
        Rebuild();
        return true;
    }

    public bool IsEquipped(SkillPassiveSO passive)
    {
        return passive != null && equipped.Contains(passive);
    }

    private bool IsPassiveRuntimeEnabled(SkillPassiveSO passive)
    {
        bool enabled = true;
        OverrideTesterPassiveRuntimeEnabled(passive, ref enabled);
        return enabled;
    }

    public bool Unequip(SkillPassiveSO passive)
    {
        if (passive == null || !equipped.Remove(passive))
            return false;

        Rebuild();
        return true;
    }

    

    public void OnCombatStarted()
    {
        _combatAllFaceBonuses.Clear();
        _temporaryCombatPassives.Clear();
        _bloodCounterAddedValueActive = false;
        _failDieNextSkillAddedValueActive = false;
        _lowHpRefillUsedThisCombat = false;
        _randomCommonPassiveRolledThisCombat = false;
        _reviveUsedThisCombat = false;
        _randomCommonPassivePickedThisCombat = null;
        _failDiceCountdownProgress = 0;
        _combatAddedValueBonus = 0;
        SyncFromInventoryIfPossible();
        ApplyRandomCommonPassiveForCombat();
        CaptureKnownDiceFaces(refreshTrackedBaseValues: true);
        ReapplyAllTrackedFaceBonuses();
    }

    public void OnTurnStarted()
    {
    }

    public void OnDiceRolled(CombatActor owner, DiceSlotRig diceRig)
    {
        HandleRollThreeEnemyDamage(owner, diceRig, changedDie: null, processAllActiveDice: true);
    }

    public void OnDiceRolled(CombatActor owner, DiceSlotRig diceRig, DiceSpinnerGeneric changedDie)
    {
        HandleRollThreeEnemyDamage(owner, diceRig, changedDie, processAllActiveDice: false);
    }

    public void OnDiceRolled(CombatActor owner, DiceSlotRig diceRig, IReadOnlyList<DiceSpinnerGeneric> changedDice)
    {
        HandleRollThreeEnemyDamage(owner, diceRig, changedDice, processAllActiveDice: false);
    }

    public int GetAddedValueForDie(DiceSlotRig diceRig, int slot0)
    {
        return 0;
    }

    public int GetBonusStatusStacksApplied(StatusKind statusKind)
    {
        return 0;
    }

    public bool ShouldRetainGuardAtEndOfTurn()
        => false;

    public void TryHandleMeleeSkillUse(DiceSlotRig diceRig, int start0)
    {
        // Passive behavior-id hooks were removed; passive runtime now comes from effect entries.
    }

    public int GetEffectValue(PassiveEffectId effectId)
    {
        int total = 0;
        if (equipped == null)
            return total;

        for (int i = 0; i < equipped.Count; i++)
        {
            SkillPassiveSO passive = equipped[i];
            if (passive == null || passive.effects == null || !IsPassiveRuntimeEnabled(passive))
                continue;

            for (int k = 0; k < passive.effects.Count; k++)
            {
                PassiveEffectEntry effect = passive.effects[k];
                if (effect != null && effect.id == effectId)
                    total += Mathf.Max(0, effect.valueI);
            }
        }

        return total;
    }

    public void HandleUsedDiceCritFocus(CombatActor owner, DiceSlotRig diceRig, int paymentMask, SkillRuntime runtime = null)
    {
        int focusPerCrit = GetEffectValue(PassiveEffectId.CritFocusOnUsedDie);
        if (owner == null || diceRig == null || focusPerCrit <= 0 || paymentMask <= 0)
            return;

        int critCount = CountUsedDiceWithFlag(runtime != null ? runtime.localCritFlags : null, diceRig, paymentMask, diceRig.IsCrit);

        if (critCount > 0)
        {
            owner.GainFocus(focusPerCrit * critCount);
            PulsePassiveEffect(PassiveEffectId.CritFocusOnUsedDie);
        }
    }

    public void HandleUsedDicePassiveEffects(CombatActor owner, DiceSlotRig diceRig, int paymentMask, SkillRuntime runtime = null)
    {
        HandleUsedDiceCritFocus(owner, diceRig, paymentMask, runtime);
        HandleUsedFailDiceEffects(owner, diceRig, paymentMask, runtime);
    }

    private void HandleUsedFailDiceEffects(CombatActor owner, DiceSlotRig diceRig, int paymentMask, SkillRuntime runtime)
    {
        if (owner == null || diceRig == null || paymentMask <= 0)
            return;

        int failCount = CountUsedDiceWithFlag(runtime != null ? runtime.localFailFlags : null, diceRig, paymentMask, diceRig.IsFail);

        if (failCount <= 0)
            return;

        int nextSkillAddedValue = GetEffectValue(PassiveEffectId.FailDieNextSkillAddedValue);
        if (nextSkillAddedValue > 0 && owner.status != null)
        {
            owner.status.GrantNextSkillAddedValue(nextSkillAddedValue * failCount);
            _failDieNextSkillAddedValueActive = true;
            PulsePassiveEffect(PassiveEffectId.FailDieNextSkillAddedValue);
        }

        ApplyFailCountdownCombatAddedValue(failCount);
        SkillTooltipUI.RefreshCurrent();
    }

    private delegate bool DiceSlotFlagGetter(int slot0);

    private static int CountUsedDiceWithFlag(IReadOnlyList<bool> runtimeFlags, DiceSlotRig diceRig, int paymentMask, DiceSlotFlagGetter fallback)
    {
        if (paymentMask <= 0)
            return 0;

        if (runtimeFlags != null && runtimeFlags.Count > 0)
        {
            int count = 0;
            for (int i = 0; i < runtimeFlags.Count; i++)
            {
                if (runtimeFlags[i])
                    count++;
            }

            return count;
        }

        if (diceRig == null || fallback == null)
            return 0;

        int fallbackCount = 0;
        for (int slot0 = 0; slot0 < 3; slot0++)
        {
            if ((paymentMask & (1 << slot0)) != 0 && fallback(slot0))
                fallbackCount++;
        }

        return fallbackCount;
    }

    private void ApplyFailCountdownCombatAddedValue(int failCount)
    {
        if (equipped == null || failCount <= 0)
            return;

        for (int i = 0; i < equipped.Count; i++)
        {
            SkillPassiveSO passive = equipped[i];
            if (passive == null || passive.effects == null || !IsPassiveRuntimeEnabled(passive))
                continue;

            for (int k = 0; k < passive.effects.Count; k++)
            {
                PassiveEffectEntry effect = passive.effects[k];
                if (effect == null || effect.id != PassiveEffectId.FailDiceCountdownCombatAddedValue)
                    continue;

                int threshold = Mathf.Max(1, effect.valueI);
                int addedValue = Mathf.Max(0, effect.value2I);
                if (addedValue <= 0)
                    continue;

                _failDiceCountdownProgress += failCount;
                int procCount = _failDiceCountdownProgress / threshold;
                if (procCount <= 0)
                    continue;

                _failDiceCountdownProgress -= procCount * threshold;
                _combatAddedValueBonus += addedValue * procCount;
                PulsePassiveEffect(PassiveEffectId.FailDiceCountdownCombatAddedValue);
            }
        }
    }

    public int PreviewUsedDiceCritFocus(DiceSlotRig diceRig, int paymentMask)
    {
        int focusPerCrit = GetEffectValue(PassiveEffectId.CritFocusOnUsedDie);
        if (diceRig == null || focusPerCrit <= 0 || paymentMask <= 0)
            return 0;

        int critCount = 0;
        for (int slot0 = 0; slot0 < 3; slot0++)
        {
            if ((paymentMask & (1 << slot0)) != 0 && diceRig.IsCrit(slot0))
                critCount++;
        }

        return focusPerCrit * critCount;
    }

    public int PreviewRuntimeCritFocus(SkillRuntime runtime)
    {
        int focusPerCrit = GetEffectValue(PassiveEffectId.CritFocusOnUsedDie);
        if (runtime == null || runtime.localCritFlags == null || focusPerCrit <= 0)
            return 0;

        int critCount = 0;
        for (int i = 0; i < runtime.localCritFlags.Count; i++)
        {
            if (runtime.localCritFlags[i])
                critCount++;
        }

        return critCount * focusPerCrit;
    }

    public int GetCombatAddedValueBonus()
        => Mathf.Max(0, _combatAddedValueBonus);

    public int GetPendingNextSkillAddedValueBonus()
    {
        CombatActor owner = GetComponent<CombatActor>();
        return owner != null && owner.status != null
            ? owner.status.PeekNextSkillAddedValue()
            : 0;
    }

    public int GetFailDiceCountdownRemaining(int threshold)
    {
        int safeThreshold = Mathf.Max(1, threshold);
        int progress = Mathf.Clamp(_failDiceCountdownProgress, 0, safeThreshold - 1);
        int remaining = safeThreshold - progress;
        return Mathf.Clamp(remaining, 1, safeThreshold);
    }

    public int GetAppliedMarkMinimumPayoffCount()
    {
        return HasEffect(PassiveEffectId.MarkPayoffMinHits) ? 2 : 1;
    }

    private bool HasEffect(PassiveEffectId effectId)
    {
        if (equipped == null)
            return false;

        for (int i = 0; i < equipped.Count; i++)
        {
            SkillPassiveSO passive = equipped[i];
            if (passive == null || passive.effects == null || !IsPassiveRuntimeEnabled(passive))
                continue;

            for (int k = 0; k < passive.effects.Count; k++)
            {
                PassiveEffectEntry effect = passive.effects[k];
                if (effect != null && effect.id == effectId)
                    return true;
            }
        }

        return false;
    }

    private PassiveEffectEntry FindFirstEffect(PassiveEffectId effectId, out SkillPassiveSO sourcePassive)
    {
        sourcePassive = null;
        if (equipped == null)
            return null;

        for (int i = 0; i < equipped.Count; i++)
        {
            SkillPassiveSO passive = equipped[i];
            if (passive == null || passive.effects == null || !IsPassiveRuntimeEnabled(passive))
                continue;

            for (int k = 0; k < passive.effects.Count; k++)
            {
                PassiveEffectEntry effect = passive.effects[k];
                if (effect == null || effect.id != effectId)
                    continue;

                sourcePassive = passive;
                return effect;
            }
        }

        return null;
    }

    private void ApplyRandomCommonPassiveForCombat()
    {
        if (_randomCommonPassiveRolledThisCombat || !HasEffect(PassiveEffectId.RandomCommonPassiveThisCombat))
            return;

        _randomCommonPassiveRolledThisCombat = true;
        List<SkillPassiveSO> candidates = new List<SkillPassiveSO>();
        AddRandomCommonPassiveCandidatesFromDatabase(candidates);
        AddRandomCommonPassiveCandidatesFromLoadedAssets(candidates);

        if (candidates.Count <= 0)
            return;

        SkillPassiveSO picked = candidates[Random.Range(0, candidates.Count)];
        _randomCommonPassivePickedThisCombat = picked;
        _temporaryCombatPassives.Add(picked);
        if (equipped == null)
            equipped = new List<SkillPassiveSO>();
        if (!equipped.Contains(picked))
            equipped.Add(picked);

        PulsePassiveEffect(PassiveEffectId.RandomCommonPassiveThisCombat);
    }

    public SkillPassiveSO GetRandomCommonPassivePickedThisCombat()
        => _randomCommonPassivePickedThisCombat;

    private void AddRandomCommonPassiveCandidatesFromDatabase(List<SkillPassiveSO> candidates)
    {
        SkillDatabaseSO database = ResolveSkillDatabase();
        if (database == null || database.passiveSkills == null)
            return;

        for (int i = 0; i < database.passiveSkills.Count; i++)
            AddRandomCommonPassiveCandidate(candidates, database.passiveSkills[i]);
    }

    private void AddRandomCommonPassiveCandidatesFromLoadedAssets(List<SkillPassiveSO> candidates)
    {
        SkillPassiveSO[] loadedPassives = Resources.FindObjectsOfTypeAll<SkillPassiveSO>();
        for (int i = 0; loadedPassives != null && i < loadedPassives.Length; i++)
            AddRandomCommonPassiveCandidate(candidates, loadedPassives[i]);
    }

    private void AddRandomCommonPassiveCandidate(List<SkillPassiveSO> candidates, SkillPassiveSO candidate)
    {
        if (candidates == null || candidate == null || candidate.spec == null || candidate.spec.rarity != ContentRarity.Common)
            return;
        if (IsEquipped(candidate) || ContainsEffect(candidate, PassiveEffectId.RandomCommonPassiveThisCombat))
            return;
        if (candidates.Contains(candidate))
            return;

        candidates.Add(candidate);
    }

    private SkillDatabaseSO ResolveSkillDatabase()
    {
        if (skillDatabase != null)
            return skillDatabase;

        skillDatabase = Resources.Load<SkillDatabaseSO>("Database_Skills");
        return skillDatabase;
    }

    private static bool ContainsEffect(SkillPassiveSO passive, PassiveEffectId effectId)
    {
        if (passive == null || passive.effects == null)
            return false;

        for (int i = 0; i < passive.effects.Count; i++)
        {
            PassiveEffectEntry effect = passive.effects[i];
            if (effect != null && effect.id == effectId)
                return true;
        }

        return false;
    }

    public void HandleResolvedHit(SkillRuntime runtime, CombatActor target, CombatActor.DamageResult result)
    {
        if (runtime == null || target == null || target.status == null)
            return;

        bool hitSucceeded = result.blocked > 0 || result.hpLost > 0 || result.requested > 0;
        if (!hitSucceeded || runtime.kind != SkillKind.Attack || runtime.range != RangeType.Ranged)
            return;

        int chance = Mathf.Clamp(GetEffectValue(PassiveEffectId.RangedHitChanceApplyMark), 0, 100);
        if (chance <= 0)
            return;

        if (chance >= 100 || Random.Range(0, 100) < chance)
        {
            CombatActor owner = GetComponent<CombatActor>();
            target.status.ApplyMark(owner);
            PulsePassiveEffect(PassiveEffectId.RangedHitChanceApplyMark);
        }
    }

    public void HandleIncomingDamage(CombatActor attacker, CombatActor.DamageResult result)
    {
        HandleLowHpRefillApIfNeeded();

        if (attacker == null || attacker.IsDead)
            return;

        if (result.blocked > 0)
        {
            int counterDamage = GetEffectValue(PassiveEffectId.GuardCounterDamage);
            if (counterDamage > 0)
            {
                StartCoroutine(ApplyGuardCounterDamageAfterDelay(attacker, counterDamage));
                PulsePassiveEffect(PassiveEffectId.GuardCounterDamage);
            }
        }

        if (result.hpLost > 0)
        {
            int addedValue = GetEffectValue(PassiveEffectId.BloodCounterNextAttackDamage);
            CombatActor owner = GetComponent<CombatActor>();
            if (addedValue > 0 && owner != null && owner.status != null)
            {
                owner.status.GrantNextSkillAddedValue(addedValue);
                _bloodCounterAddedValueActive = true;
                PulsePassiveEffect(PassiveEffectId.BloodCounterNextAttackDamage);
                SkillTooltipUI.RefreshCurrent();
            }
        }

        if (result.guardBroken && GetEffectValue(PassiveEffectId.GuardBreakMark) > 0 && attacker.status != null)
        {
            attacker.status.ApplyMark(GetComponent<CombatActor>());
            PulsePassiveEffect(PassiveEffectId.GuardBreakMark);
        }
    }

    public void HandleHpChanged()
    {
        HandleLowHpRefillApIfNeeded();
    }

    private IEnumerator ApplyGuardCounterDamageAfterDelay(CombatActor attacker, int damage)
    {
        if (damage <= 0 || attacker == null || attacker.IsDead)
            yield break;

        yield return new WaitForSeconds(0.1f);

        if (attacker == null || attacker.IsDead)
            yield break;

        CombatActor owner = GetComponent<CombatActor>();
        CombatActor.DamageResult counterResult = attacker.TakeDamageDetailed(damage, bypassGuard: false, attacker: owner);
        CombatHitFeedback.Play(attacker, CombatHitFeedback.FeedbackKind.Hit);

        DamagePopupSystem popups = DamagePopupSystemRegistry.Get();
        if (popups != null)
            popups.SpawnDamageSplit(null, attacker, counterResult.blocked, counterResult.hpLost);
    }

    public int AdjustOutgoingHitDamage(SkillRuntime runtime, int damage)
        => AdjustOutgoingDamageAgainstTarget(null, damage, pulseEffect: true);

    public int PreviewOutgoingHitDamage(SkillRuntime runtime, int damage)
        => PreviewOutgoingDamageAgainstTarget(null, damage);

    public int AdjustOutgoingDamageAgainstTarget(CombatActor target, int damage, bool pulseEffect = true)
    {
        int adjusted = Mathf.Max(0, damage);
        if (adjusted <= 0 || !ShouldApplyMinimumImpactToTarget(target))
            return adjusted;

        int minimum = GetEffectValue(PassiveEffectId.MinimumImpactDamage);
        if (minimum > 0 && adjusted < minimum)
        {
            adjusted = minimum;
            if (pulseEffect)
                PulsePassiveEffect(PassiveEffectId.MinimumImpactDamage);
        }

        return adjusted;
    }

    public int PreviewOutgoingDamageAgainstTarget(CombatActor target, int damage)
        => AdjustOutgoingDamageAgainstTarget(target, damage, pulseEffect: false);

    public int GetMeleeFollowUpDamage(SkillRuntime runtime)
    {
        if (runtime == null || runtime.kind != SkillKind.Attack || runtime.range != RangeType.Melee)
            return 0;

        return GetEffectValue(PassiveEffectId.MeleeFollowUpDamage);
    }

    public void ClearBloodCounterAddedValueIfUnused()
    {
        if (!_bloodCounterAddedValueActive && !_failDieNextSkillAddedValueActive)
            return;

        CombatActor owner = GetComponent<CombatActor>();
        if (owner != null && owner.status != null && owner.status.PeekNextSkillAddedValue() > 0)
            owner.status.ClearNextSkillAddedValue();

        _bloodCounterAddedValueActive = false;
        _failDieNextSkillAddedValueActive = false;
    }

    public bool TryPreventDeath()
    {
        if (_reviveUsedThisCombat)
            return false;

        PassiveEffectEntry reviveEffect = FindFirstEffect(PassiveEffectId.OneTimeReviveThenEmptySlot, out SkillPassiveSO sourcePassive);
        if (reviveEffect == null || sourcePassive == null)
            return false;

        CombatActor owner = GetComponent<CombatActor>();
        if (owner == null || owner.hp > 0)
            return false;

        _reviveUsedThisCombat = true;
        int hpPercent = Mathf.Clamp(reviveEffect.valueI, 1, 100);
        int reviveHp = Mathf.Max(1, Mathf.CeilToInt(owner.maxHP * hpPercent / 100f));
        owner.hp = Mathf.Clamp(reviveHp, 1, Mathf.Max(1, owner.maxHP));
        PulsePassiveEffect(PassiveEffectId.OneTimeReviveThenEmptySlot);

        if (runInventory == null)
            TryBindInventory();
        if (runInventory != null)
            runInventory.RemoveOwnedPassive(sourcePassive);

        Unequip(sourcePassive);
        return true;
    }

    private void HandleLowHpRefillApIfNeeded()
    {
        if (_lowHpRefillUsedThisCombat)
            return;

        int thresholdPercent = GetEffectValue(PassiveEffectId.LowHpRefillApOncePerCombat);
        if (thresholdPercent <= 0)
            return;

        CombatActor owner = GetComponent<CombatActor>();
        if (owner == null || owner.maxHP <= 0 || owner.hp <= 0)
            return;

        float hpPercent = owner.hp / (float)owner.maxHP * 100f;
        if (hpPercent > Mathf.Clamp(thresholdPercent, 1, 100))
            return;

        _lowHpRefillUsedThisCombat = true;
        owner.GainFocus(owner.maxFocus);
        PulsePassiveEffect(PassiveEffectId.LowHpRefillApOncePerCombat);
    }

    public void PulsePassiveEffect(PassiveEffectId effectId)
    {
        if (equipped == null)
            return;

        for (int i = 0; i < equipped.Count; i++)
        {
            SkillPassiveSO passive = equipped[i];
            if (passive == null || passive.effects == null || !IsPassiveRuntimeEnabled(passive))
                continue;

            bool hasEffect = false;
            for (int k = 0; k < passive.effects.Count; k++)
            {
                PassiveEffectEntry effect = passive.effects[k];
                if (effect != null && effect.id == effectId)
                {
                    hasEffect = true;
                    break;
                }
            }

            if (hasEffect)
                DraggableSkillIcon.PulseSkillAssetIcons(passive);
        }
    }

    public float GetBurnConsumeMultiplier() => 1f;
    public float GetLightningVsMarkMultiplierAdd() => 0f;
    public int GetFreezeBreakFocusBonusAdd() => 0;
    public float GetGuardGainPercent() => 0f;
    public int GetGuardFlatAtTurnEnd() => 0;
    public float GetOutgoingDamageMultiplier(SkillRuntime rt, CombatActor target) => 1f;

    private void HandleRollThreeEnemyDamage(CombatActor owner, DiceSlotRig diceRig, DiceSpinnerGeneric changedDie, bool processAllActiveDice)
    {
        HandleRollThreeEnemyDamage(owner, diceRig, changedDie != null ? new[] { changedDie } : null, processAllActiveDice);
    }

    private void HandleRollThreeEnemyDamage(CombatActor owner, DiceSlotRig diceRig, IReadOnlyList<DiceSpinnerGeneric> changedDice, bool processAllActiveDice)
    {
        int legacyRollThreeDamage = GetEffectValue(PassiveEffectId.RollThreeRandomEnemyDamage);
        if (owner == null || diceRig == null)
            return;

        List<int> procDamages = new List<int>(3);
        for (int slot0 = 0; slot0 < 3; slot0++)
        {
            if (!diceRig.IsSlotActive(slot0))
                continue;

            DiceSpinnerGeneric die = diceRig.GetDice(slot0);
            if (die == null)
                continue;
            if (!processAllActiveDice && !ContainsChangedDie(changedDice, die))
                continue;

            DiceSlotRig.RollInfo rollInfo = diceRig.GetRollInfo(slot0);
            if (!rollInfo.isUsable || rollInfo.isBrokenFace)
                continue;

            int rolledValue = rollInfo.rolledValue;
            if (rolledValue == 3 && legacyRollThreeDamage > 0)
                procDamages.Add(legacyRollThreeDamage);
        }

        if (procDamages.Count <= 0)
            return;

        StartCoroutine(ResolveRollThreeEnemyDamageSequence(owner, procDamages));
    }

    private static bool ContainsChangedDie(IReadOnlyList<DiceSpinnerGeneric> changedDice, DiceSpinnerGeneric die)
    {
        if (die == null)
            return false;
        if (changedDice == null)
            return false;

        for (int i = 0; i < changedDice.Count; i++)
        {
            if (changedDice[i] == die)
                return true;
        }

        return false;
    }

    private IEnumerator ResolveRollThreeEnemyDamageSequence(CombatActor owner, IReadOnlyList<int> procDamages)
    {
        DamagePopupSystem popups = DamagePopupSystemRegistry.Get();
        if (procDamages == null)
            yield break;

        for (int i = 0; i < procDamages.Count; i++)
        {
            int damage = Mathf.Max(0, procDamages[i]);
            if (damage <= 0)
                continue;

            CombatActor target = ResolvePreferredEnemyTarget(owner);
            if (target == null)
                break;

            CombatActor.DamageResult result = target.TakeDamageDetailed(damage, bypassGuard: false, attacker: owner);
            if (result.blocked > 0 || result.hpLost > 0)
                CombatHitFeedback.Play(target, CombatHitFeedback.FeedbackKind.Hit);
            if (result.guardBroken && target.status != null)
                target.status.ApplyStagger();
            if (popups != null)
                popups.SpawnDamageSplit(owner, target, result.blocked, result.hpLost);
            PulsePassiveEffect(PassiveEffectId.RollThreeRandomEnemyDamage);

            if (i < procDamages.Count - 1)
                yield return new WaitForSeconds(RollThreeHitDelay);
        }
    }

    private static CombatActor ResolvePreferredEnemyTarget(CombatActor owner)
    {
        if (owner == null)
            return null;

        BattlePartyManager2D party = BattlePartyManagerRegistry.Get();
        if (party != null)
        {
            CombatActor frontTarget = PickRandomActor(TurnManagerCombatUtility.ResolveAliveEnemiesInRow(party, null, CombatActor.RowTag.Front));
            if (frontTarget != null)
                return frontTarget;

            return PickRandomActor(TurnManagerCombatUtility.ResolveAliveEnemiesSnapshot(party, null));
        }

        CombatActor[] actors = CombatActorRegistry.GetAllSnapshot(includeInactive: false);
        List<CombatActor> frontFallback = new List<CombatActor>();
        List<CombatActor> anyFallback = new List<CombatActor>();
        for (int i = 0; i < actors.Length; i++)
        {
            CombatActor actor = actors[i];
            if (actor == null || actor == owner || actor.IsDead || actor.team == owner.team)
                continue;

            anyFallback.Add(actor);
            if (actor.row == CombatActor.RowTag.Front)
                frontFallback.Add(actor);
        }

        CombatActor fallbackTarget = PickRandomActor(frontFallback);
        return fallbackTarget != null ? fallbackTarget : PickRandomActor(anyFallback);
    }

    private static CombatActor PickRandomActor(IReadOnlyList<CombatActor> actors)
    {
        if (actors == null || actors.Count <= 0)
            return null;

        return actors[Random.Range(0, actors.Count)];
    }

    private bool ShouldApplyMinimumImpactToTarget(CombatActor target)
    {
        if (target == null)
            return false;

        CombatActor owner = GetComponent<CombatActor>();
        if (owner == null || owner == target)
            return false;

        return owner.team != target.team;
    }
}

internal static class PassiveSystemRegistry
{
    private static PassiveSystem _playerPassiveSystem;

    public static void Register(PassiveSystem passiveSystem)
    {
        if (passiveSystem == null)
            return;

        CombatActor actor = passiveSystem.GetComponent<CombatActor>();
        if (actor != null && actor.isPlayer)
            _playerPassiveSystem = passiveSystem;
    }

    public static void Unregister(PassiveSystem passiveSystem)
    {
        if (passiveSystem != null && _playerPassiveSystem == passiveSystem)
            _playerPassiveSystem = null;
    }

    public static PassiveSystem GetPlayer()
    {
        if (_playerPassiveSystem != null && _playerPassiveSystem.isActiveAndEnabled)
            return _playerPassiveSystem;

#if UNITY_2023_1_OR_NEWER
        PassiveSystem[] systems = Object.FindObjectsByType<PassiveSystem>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
        PassiveSystem[] systems = Object.FindObjectsOfType<PassiveSystem>();
#endif
        for (int i = 0; systems != null && i < systems.Length; i++)
        {
            PassiveSystem system = systems[i];
            CombatActor actor = system != null ? system.GetComponent<CombatActor>() : null;
            if (actor != null && actor.isPlayer)
            {
                _playerPassiveSystem = system;
                return _playerPassiveSystem;
            }
        }

        return null;
    }
}
