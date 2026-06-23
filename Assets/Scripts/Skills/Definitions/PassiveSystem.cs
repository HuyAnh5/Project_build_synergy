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

    private readonly List<SkillPassiveSO> _syncBuffer = new List<SkillPassiveSO>(16);
    private readonly Dictionary<DiceSpinnerGeneric, int[]> _baseFaceValues = new Dictionary<DiceSpinnerGeneric, int[]>();
    private readonly Dictionary<DiceSpinnerGeneric, int[]> _permanentFaceBonuses = new Dictionary<DiceSpinnerGeneric, int[]>();
    private readonly Dictionary<DiceSpinnerGeneric, int> _combatAllFaceBonuses = new Dictionary<DiceSpinnerGeneric, int>();
    private BattlePartyManager2D _cachedParty;
    private DiceSlotRig _cachedDiceRig;

    private int _focusBonusTurnStart;
    private bool _bloodCounterAddedValueActive;

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
    }

    private void OnDisable()
    {
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
        _focusBonusTurnStart = 0;

        if (equipped == null)
            equipped = new List<SkillPassiveSO>();

        for (int i = 0; i < equipped.Count; i++)
        {
            SkillPassiveSO passive = equipped[i];
            if (passive == null || passive.effects == null)
                continue;

            for (int k = 0; k < passive.effects.Count; k++)
            {
                PassiveEffectEntry effect = passive.effects[k];
                if (effect == null)
                    continue;

                Accumulate(effect);
            }
        }
    }

    private void TryBindInventory()
    {
        if (!autoSyncFromRunInventory)
            return;

        if (runInventory == null)
        {
            runInventory = GetComponentInParent<RunInventoryManager>(true);
            if (runInventory == null)
            {
#if UNITY_2023_1_OR_NEWER
                runInventory = Object.FindFirstObjectByType<RunInventoryManager>(FindObjectsInactive.Include);
#else
                runInventory = Object.FindObjectOfType<RunInventoryManager>(true);
#endif
            }
        }

        if (runInventory != null)
        {
            runInventory.InventoryChanged -= OnInventoryChanged;
            runInventory.InventoryChanged += OnInventoryChanged;
        }
    }

    private BattlePartyManager2D GetParty()
    {
        if (_cachedParty == null)
            _cachedParty = Object.FindObjectOfType<BattlePartyManager2D>(true);
        return _cachedParty;
    }

    private DiceSlotRig GetDiceRig()
    {
        if (runInventory != null && runInventory.DiceRig != null)
            return runInventory.DiceRig;

        if (_cachedDiceRig == null)
            _cachedDiceRig = FindObjectOfType<DiceSlotRig>(true);

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

    public bool Unequip(SkillPassiveSO passive)
    {
        if (passive == null || !equipped.Remove(passive))
            return false;

        Rebuild();
        return true;
    }

    private void Accumulate(PassiveEffectEntry effect)
    {
        switch (effect.id)
        {
            case PassiveEffectId.FocusBonusOnTurnStart:
                _focusBonusTurnStart += effect.valueI;
                break;
        }
    }

    public int GetFocusBonusOnTurnStart(CombatActor owner = null, DiceSlotRig diceRig = null, CombatActor target = null)
    {
        int total = _focusBonusTurnStart;

        if (equipped == null || equipped.Count == 0)
            return total;

        SkillConditionContext conditionContext = BuildPassiveConditionContext(owner, diceRig, target);
        for (int i = 0; i < equipped.Count; i++)
        {
            SkillPassiveSO passive = equipped[i];
            if (passive == null || passive.effects == null)
                continue;

            for (int k = 0; k < passive.effects.Count; k++)
            {
                PassiveEffectEntry effect = passive.effects[k];
                if (effect != null && effect.id == PassiveEffectId.FocusBonusOnTurnStart)
                    total += effect.valueI;
            }
        }

        return total;
    }

    private SkillConditionContext BuildPassiveConditionContext(CombatActor owner, DiceSlotRig diceRig, CombatActor target)
    {
        var localBaseValues = new List<int>(3);
        var localResolvedValues = new List<int>(3);
        var localNumericFlags = new List<bool>(3);
        var localCritFlags = new List<bool>(3);
        var localFailFlags = new List<bool>(3);

        if (diceRig != null)
        {
            for (int i = 0; i < 3; i++)
            {
                if (!diceRig.IsSlotActive(i))
                    continue;

                localBaseValues.Add(diceRig.IsNumericFaceForConditions(i) ? diceRig.GetBaseValue(i) : 0);
                localResolvedValues.Add(diceRig.GetResolvedDieValue(i, owner));
                localNumericFlags.Add(diceRig.IsNumericFaceForConditions(i));
                localCritFlags.Add(diceRig.IsCrit(i));
                localFailFlags.Add(diceRig.IsFail(i));
            }
        }

        BattlePartyManager2D party = GetParty();
        int enemiesWithBurnCount = 0;
        int markedEnemiesCount = 0;
        int totalBleedOnBoard = 0;
        int aliveEnemiesCount = 0;

        if (party != null && party.Enemies != null)
        {
            for (int i = 0; i < party.Enemies.Count; i++)
            {
                CombatActor enemy = party.Enemies[i];
                if (enemy == null || enemy.IsDead || enemy.status == null)
                    continue;

                aliveEnemiesCount++;
                if (enemy.status.burnStacks > 0)
                    enemiesWithBurnCount++;
                if (enemy.status.marked)
                    markedEnemiesCount++;
                totalBleedOnBoard += Mathf.Max(0, enemy.status.bleedStacks);
            }
        }

        return new SkillConditionContext
        {
            scope = SkillConditionScope.Global,
            localBaseValues = localBaseValues,
            localNumericFlags = localNumericFlags,
            localResolvedValues = localResolvedValues,
            localCritFlags = localCritFlags,
            localFailFlags = localFailFlags,
            currentFocus = owner != null ? owner.focus : 0,
            currentGuard = owner != null ? owner.guardPool : 0,
            occupiedSlots = localBaseValues.Count,
            remainingSlots = Mathf.Clamp(3 - localBaseValues.Count, 0, 3),
            enemiesWithBurnCount = enemiesWithBurnCount,
            markedEnemiesCount = markedEnemiesCount,
            totalBleedOnBoard = totalBleedOnBoard,
            aliveEnemiesCount = aliveEnemiesCount,
            targetHasBurn = target != null && target.status != null && target.status.burnStacks > 0,
            targetHasFreeze = target != null && target.status != null && target.status.frozen,
            targetHasChilled = target != null && target.status != null && target.status.chilledTurns > 0,
            targetHasMark = target != null && target.status != null && target.status.marked,
            targetHasBleed = target != null && target.status != null && target.status.bleedStacks > 0,
            targetHasStagger = target != null && target.status != null && target.status.staggered
        };
    }

    public void OnCombatStarted()
    {
        _combatAllFaceBonuses.Clear();
        CaptureKnownDiceFaces(refreshTrackedBaseValues: true);
        ReapplyAllTrackedFaceBonuses();
    }

    public void OnDiceRolled(CombatActor owner, DiceSlotRig diceRig)
    {
        // Passive behavior-id hooks were removed; passive runtime now comes from effect entries.
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
            if (passive == null || passive.effects == null)
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

    public void HandleUsedDiceCritFocus(CombatActor owner, DiceSlotRig diceRig, int paymentMask)
    {
        int focusPerCrit = GetEffectValue(PassiveEffectId.CritFocusOnUsedDie);
        if (owner == null || diceRig == null || focusPerCrit <= 0 || paymentMask <= 0)
            return;

        int critCount = 0;
        for (int slot0 = 0; slot0 < 3; slot0++)
        {
            if ((paymentMask & (1 << slot0)) != 0 && diceRig.IsCrit(slot0))
                critCount++;
        }

        if (critCount > 0)
        {
            owner.GainFocus(focusPerCrit * critCount);
            PulsePassiveEffect(PassiveEffectId.CritFocusOnUsedDie);
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

    public void HandleIncomingDamage(CombatActor attacker, CombatActor.DamageResult result)
    {
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
                owner.status.GrantNextSkillAddedValueOnceNonStacking(addedValue);
                _bloodCounterAddedValueActive = true;
                PulsePassiveEffect(PassiveEffectId.BloodCounterNextAttackDamage);
            }
        }

        if (result.guardBroken && GetEffectValue(PassiveEffectId.GuardBreakMark) > 0 && attacker.status != null)
        {
            attacker.status.ApplyMark();
            PulsePassiveEffect(PassiveEffectId.GuardBreakMark);
        }
    }

    private IEnumerator ApplyGuardCounterDamageAfterDelay(CombatActor attacker, int damage)
    {
        if (damage <= 0 || attacker == null || attacker.IsDead)
            yield break;

        yield return new WaitForSeconds(0.1f);

        if (attacker == null || attacker.IsDead)
            yield break;

        CombatActor.DamageResult counterResult = attacker.TakeDamageDetailed(damage, bypassGuard: false);
        CombatHitFeedback.Play(attacker, CombatHitFeedback.FeedbackKind.Hit);

        DamagePopupSystem popups = FindObjectOfType<DamagePopupSystem>(true);
        if (popups != null)
            popups.SpawnDamageSplit(null, attacker, counterResult.blocked, counterResult.hpLost);
    }

    public int AdjustOutgoingHitDamage(SkillRuntime runtime, int damage)
    {
        int adjusted = Mathf.Max(0, damage);
        if (runtime == null || runtime.kind != SkillKind.Attack || adjusted <= 0)
            return adjusted;

        int minimum = GetEffectValue(PassiveEffectId.MinimumImpactDamage);
        if (minimum > 0 && adjusted < minimum)
        {
            adjusted = minimum;
            PulsePassiveEffect(PassiveEffectId.MinimumImpactDamage);
        }
        return adjusted;
    }

    public int PreviewOutgoingHitDamage(SkillRuntime runtime, int damage)
    {
        int adjusted = Mathf.Max(0, damage);
        if (runtime == null || runtime.kind != SkillKind.Attack || adjusted <= 0)
            return adjusted;

        int minimum = GetEffectValue(PassiveEffectId.MinimumImpactDamage);
        if (minimum > 0 && adjusted < minimum)
            adjusted = minimum;
        return adjusted;
    }

    public int GetMeleeFollowUpDamage(SkillRuntime runtime)
    {
        if (runtime == null || runtime.kind != SkillKind.Attack || runtime.range != RangeType.Melee)
            return 0;
        return GetEffectValue(PassiveEffectId.MeleeFollowUpDamage);
    }

    public void ClearBloodCounterAddedValueIfUnused()
    {
        if (!_bloodCounterAddedValueActive)
            return;

        CombatActor owner = GetComponent<CombatActor>();
        if (owner != null && owner.status != null && owner.status.PeekNextSkillAddedValue() > 0)
            owner.status.ClearNextSkillAddedValue();

        _bloodCounterAddedValueActive = false;
    }

    public void PulsePassiveEffect(PassiveEffectId effectId)
    {
        if (equipped == null)
            return;

        for (int i = 0; i < equipped.Count; i++)
        {
            SkillPassiveSO passive = equipped[i];
            if (passive == null || passive.effects == null)
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
}
