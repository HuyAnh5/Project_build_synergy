using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PassiveSystem : MonoBehaviour
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

    private int _focusBonusTurnStart;
    private bool _diceForgingTriggeredThisCombat;

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

    public int GetFocusBonusOnTurnStart() => _focusBonusTurnStart;

    public void OnCombatStarted()
    {
        _diceForgingTriggeredThisCombat = false;
        _combatAllFaceBonuses.Clear();
        CaptureKnownDiceFaces();
        ReapplyAllTrackedFaceBonuses();
    }

    public void OnDiceRolled(CombatActor owner, DiceSlotRig diceRig)
    {
        if (owner == null || diceRig == null)
            return;

        bool hasFailForward = HasBehavior(PassiveBehaviorId.FailForward);
        bool hasCritEscalation = HasBehavior(PassiveBehaviorId.CritEscalation);

        for (int i = 0; i < 3; i++)
        {
            if (!diceRig.IsSlotActive(i))
                continue;

            DiceSpinnerGeneric die = diceRig.slots != null && i < diceRig.slots.Length && diceRig.slots[i] != null
                ? diceRig.slots[i].dice
                : null;
            if (die == null)
                continue;

            if (hasFailForward && diceRig.IsFail(i))
                owner.AddGuard(3);

            if (hasCritEscalation && diceRig.IsCrit(i))
                AddCombatAllFacesBonus(die, 1);
        }
    }

    public int GetAddedValueForDie(DiceSlotRig diceRig, int slot0)
    {
        if (diceRig == null || !diceRig.IsSlotActive(slot0))
            return 0;

        int add = 0;
        if (HasBehavior(PassiveBehaviorId.EvenResonance))
        {
            int baseValue = diceRig.GetBaseValue(slot0);
            if (baseValue > 0 && (baseValue % 2) == 0)
                add += 3;
        }

        return add;
    }

    public int GetBonusStatusStacksApplied(StatusKind statusKind)
    {
        if (!HasBehavior(PassiveBehaviorId.ElementalCatalyst))
            return 0;

        return statusKind == StatusKind.Burn || statusKind == StatusKind.Bleed ? 1 : 0;
    }

    public bool ShouldRetainGuardAtEndOfTurn()
        => HasBehavior(PassiveBehaviorId.IronStance);

    public void TryHandleBasicStrikeUse(DiceSlotRig diceRig, int start0)
    {
        if (!HasBehavior(PassiveBehaviorId.DiceForging) || _diceForgingTriggeredThisCombat || diceRig == null || start0 < 0)
            return;

        DiceSpinnerGeneric die = diceRig.slots != null && start0 < diceRig.slots.Length && diceRig.slots[start0] != null
            ? diceRig.slots[start0].dice
            : null;
        if (die == null || die.faces == null || die.LastFaceIndex < 0)
            return;

        AddPermanentFaceBonus(die, die.LastFaceIndex, 1);
        _diceForgingTriggeredThisCombat = true;
    }

    private bool HasBehavior(PassiveBehaviorId behaviorId)
    {
        if (equipped == null || behaviorId == PassiveBehaviorId.None)
            return false;

        for (int i = 0; i < equipped.Count; i++)
        {
            SkillPassiveSO passive = equipped[i];
            if (passive == null)
                continue;
            if (passive.behaviorId == behaviorId)
                return true;
        }

        return false;
    }

    private void CaptureKnownDiceFaces()
    {
        DiceSlotRig diceRig = runInventory != null ? runInventory.DiceRig : null;
        if (diceRig == null)
            diceRig = FindObjectOfType<DiceSlotRig>(true);
        if (diceRig == null || diceRig.slots == null)
            return;

        for (int i = 0; i < diceRig.slots.Length; i++)
        {
            DiceSpinnerGeneric die = diceRig.slots != null && i < diceRig.slots.Length && diceRig.slots[i] != null
                ? diceRig.slots[i].dice
                : null;
            if (die == null || die.faces == null)
                continue;

            EnsureDieTracked(die);
        }
    }

    private void EnsureDieTracked(DiceSpinnerGeneric die)
    {
        if (die == null || die.faces == null)
            return;

        if (!_baseFaceValues.ContainsKey(die))
        {
            int[] baseValues = new int[die.faces.Length];
            for (int i = 0; i < die.faces.Length; i++)
                baseValues[i] = die.faces[i].value;
            _baseFaceValues[die] = baseValues;
        }

        if (!_permanentFaceBonuses.ContainsKey(die))
            _permanentFaceBonuses[die] = new int[die.faces.Length];

        if (!_combatAllFaceBonuses.ContainsKey(die))
            _combatAllFaceBonuses[die] = 0;
    }

    private void AddPermanentFaceBonus(DiceSpinnerGeneric die, int faceIndex, int amount)
    {
        if (die == null || die.faces == null || amount == 0)
            return;

        EnsureDieTracked(die);
        int[] permanentBonuses = _permanentFaceBonuses[die];
        if (faceIndex < 0 || faceIndex >= permanentBonuses.Length)
            return;

        permanentBonuses[faceIndex] += amount;
        ReapplyTrackedFaceBonuses(die);
    }

    private void AddCombatAllFacesBonus(DiceSpinnerGeneric die, int amount)
    {
        if (die == null || die.faces == null || amount == 0)
            return;

        EnsureDieTracked(die);
        _combatAllFaceBonuses[die] += amount;
        ReapplyTrackedFaceBonuses(die);
    }

    private void ReapplyAllTrackedFaceBonuses()
    {
        foreach (var pair in _baseFaceValues)
            ReapplyTrackedFaceBonuses(pair.Key);
    }

    private void ReapplyTrackedFaceBonuses(DiceSpinnerGeneric die)
    {
        if (die == null || die.faces == null || !_baseFaceValues.TryGetValue(die, out int[] baseValues))
            return;

        int[] permanentBonuses = _permanentFaceBonuses.TryGetValue(die, out int[] bonuses) ? bonuses : null;
        int combatAdd = _combatAllFaceBonuses.TryGetValue(die, out int add) ? add : 0;

        for (int i = 0; i < die.faces.Length && i < baseValues.Length; i++)
        {
            DiceFace face = die.faces[i];
            int permanentAdd = permanentBonuses != null && i < permanentBonuses.Length ? permanentBonuses[i] : 0;
            face.value = Mathf.Max(1, baseValues[i] + permanentAdd + combatAdd);
            die.faces[i] = face;
        }

        die.RefreshDisplayedState();
    }

    public float GetBurnConsumeMultiplier() => 1f;
    public float GetLightningVsMarkMultiplierAdd() => 0f;
    public int GetFreezeBreakFocusBonusAdd() => 0;
    public float GetGuardGainPercent() => 0f;
    public int GetGuardFlatAtTurnEnd() => 0;
    public float GetOutgoingDamageMultiplier(SkillRuntime rt, CombatActor target) => 1f;
}
