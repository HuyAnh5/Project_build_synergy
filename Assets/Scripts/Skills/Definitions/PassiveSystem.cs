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
    private DiceSlotRig _cachedDiceRig;
    private const float RollThreeHitDelay = 0.15f;

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

    

    public void OnCombatStarted()
    {
        _combatAllFaceBonuses.Clear();
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
                owner.status.GrantNextSkillAddedValue(addedValue);
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

    private void HandleRollThreeEnemyDamage(CombatActor owner, DiceSlotRig diceRig, DiceSpinnerGeneric changedDie, bool processAllActiveDice)
    {
        HandleRollThreeEnemyDamage(owner, diceRig, changedDie != null ? new[] { changedDie } : null, processAllActiveDice);
    }

    private void HandleRollThreeEnemyDamage(CombatActor owner, DiceSlotRig diceRig, IReadOnlyList<DiceSpinnerGeneric> changedDice, bool processAllActiveDice)
    {
        int damagePerProc = GetEffectValue(PassiveEffectId.RollThreeRandomEnemyDamage);
        if (owner == null || diceRig == null || damagePerProc <= 0)
            return;

        int procCount = 0;
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
            if (rolledValue == 3)
                procCount++;
        }

        if (procCount <= 0)
            return;

        StartCoroutine(ResolveRollThreeEnemyDamageSequence(owner, damagePerProc, procCount));
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

    private IEnumerator ResolveRollThreeEnemyDamageSequence(CombatActor owner, int damagePerProc, int procCount)
    {
        DamagePopupSystem popups = DamagePopupSystemRegistry.Get();
        for (int i = 0; i < procCount; i++)
        {
            CombatActor target = ResolvePreferredEnemyTarget(owner);
            if (target == null)
                break;

            CombatActor.DamageResult result = target.TakeDamageDetailed(damagePerProc, bypassGuard: false, attacker: owner);
            if (result.blocked > 0 || result.hpLost > 0)
                CombatHitFeedback.Play(target, CombatHitFeedback.FeedbackKind.Hit);
            if (result.guardBroken && target.status != null)
                target.status.ApplyStagger();
            if (popups != null)
                popups.SpawnDamageSplit(owner, target, result.blocked, result.hpLost);
            PulsePassiveEffect(PassiveEffectId.RollThreeRandomEnemyDamage);

            if (i < procCount - 1)
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
