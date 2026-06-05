using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-500)]
[DisallowMultipleComponent]
public class CombatLabPrototypeController : MonoBehaviour
{
    [SerializeField] private CombatLabPrototypeConfigSO config;
    [SerializeField] private BattlePartyManager2D party;
    [SerializeField] private RunInventoryManager runInventory;
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private RewardGachaDemoController rewardController;
    [SerializeField] private bool shuffleSelectedSkillOrder = true;

    private readonly List<DiceSpinnerGeneric> _dicePrefabOptions = new List<DiceSpinnerGeneric>(2);
    private readonly List<CombatLabPrototypeConfigSO.SkillPairEntry> _skillGroupOptions = new List<CombatLabPrototypeConfigSO.SkillPairEntry>(6);
    private readonly List<ScriptableObject> _selectedSkills = new List<ScriptableObject>(4);
    private readonly List<ConsumableDataSO> _heldConsumables = new List<ConsumableDataSO>(RunInventoryManager.DEFAULT_CONSUMABLE_CAPACITY);
    private int _currentCombatIndex;
    private bool _runEnded;
    private static string s_lastSelectedSkillGroupKey;

    private void Awake()
    {
        AutoResolveReferences();
        ApplyEncounterAuthoringToParty();
    }

    private void Start()
    {
        AutoResolveReferences();
        if (turnManager != null)
            turnManager.CombatVictoryResolved += HandleCombatVictoryResolved;

        ApplyPrototypeLoadout();
        ShowConsumableRewardThenStartCombat(0);
    }

    private void OnDestroy()
    {
        if (turnManager != null)
            turnManager.CombatVictoryResolved -= HandleCombatVictoryResolved;
    }

    [ContextMenu("Apply Prototype Loadout")]
    public void ApplyPrototypeLoadout()
    {
        if (config == null || runInventory == null)
            return;

        _currentCombatIndex = 0;
        _runEnded = false;
        ApplyRandomSkillLoadout();
        ClearConsumables();
        ClearPassiveSlot();
        ApplyRandomDiceLoadout();

        if (turnManager != null)
            turnManager.SetPlayerInteractionLocked(true);
    }

    public void ResetGame()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid())
            return;

        if (activeScene.buildIndex >= 0)
            SceneManager.LoadScene(activeScene.buildIndex);
        else
            SceneManager.LoadScene(activeScene.path);
    }

    private void ApplyEncounterAuthoringToParty()
    {
        if (config == null || party == null)
            return;

        party.enemySlots = BuildEnemySpawnSlots(0);
    }

    private BattlePartyManager2D.SpawnSlot[] BuildEnemySpawnSlots(int encounterIndex)
    {
        BattlePartyManager2D.SpawnSlot[] result = new BattlePartyManager2D.SpawnSlot[3];
        CombatLabPrototypeConfigSO.EnemyEntry[] source = ResolveEncounterEnemies(encounterIndex);
        if (source == null)
            return result;

        int writeIndex = 0;
        for (int i = 0; i < source.Length && writeIndex < result.Length; i++)
        {
            CombatLabPrototypeConfigSO.EnemyEntry entry = source[i];
            if (entry == null || !entry.enabled || entry.prefab == null)
                continue;

            result[writeIndex++] = new BattlePartyManager2D.SpawnSlot
            {
                label = entry.prefab.name,
                prefab = entry.prefab,
                row = entry.row,
                orderInRow = entry.orderInRow
            };
        }

        return result;
    }

    private CombatLabPrototypeConfigSO.EnemyEntry[] ResolveEncounterEnemies(int encounterIndex)
    {
        if (config == null)
            return null;

        CombatLabPrototypeConfigSO.EncounterEntry encounter = config.GetRunEncounter(encounterIndex);
        if (encounter != null && HasAnyEnemy(encounter.enemies))
            return encounter.enemies;

        return config.enemies;
    }

    private static bool HasAnyEnemy(CombatLabPrototypeConfigSO.EnemyEntry[] entries)
    {
        if (entries == null)
            return false;

        for (int i = 0; i < entries.Length; i++)
        {
            CombatLabPrototypeConfigSO.EnemyEntry entry = entries[i];
            if (entry != null && entry.enabled && entry.prefab != null)
                return true;
        }

        return false;
    }

    private void ApplyRandomSkillLoadout()
    {
        for (int i = 0; i < RunInventoryManager.OWNED_SKILL_COUNT; i++)
            runInventory.SetSkill(RunInventoryManager.SkillSource.Owned, i, null);

        BuildSkillGroupOptions();
        if (_skillGroupOptions.Count <= 0)
        {
            Debug.LogWarning("[CombatLabPrototypeController] Need at least 1 valid 4-skill group configured.", this);
            return;
        }

        CombatLabPrototypeConfigSO.SkillPairEntry group = PickRandomSkillGroup();

        _selectedSkills.Clear();
        _selectedSkills.Add(group.skillA);
        _selectedSkills.Add(group.skillB);
        _selectedSkills.Add(group.skillC);
        _selectedSkills.Add(group.skillD);

        if (shuffleSelectedSkillOrder)
            Shuffle(_selectedSkills);

        for (int i = 0; i < _selectedSkills.Count && i < RunInventoryManager.OWNED_SKILL_COUNT; i++)
            runInventory.SetSkill(RunInventoryManager.SkillSource.Owned, i, _selectedSkills[i]);
    }

    private void ClearConsumables()
    {
        for (int i = runInventory.ConsumableCapacity - 1; i >= 0; i--)
            runInventory.ClearConsumable(i);
    }

    private void ApplyFixedConsumables()
    {
        ClearConsumables();

        if (config.consumables == null)
            return;

        int writeCount = Mathf.Min(config.consumables.Length, runInventory.ConsumableCapacity);
        for (int i = 0; i < writeCount; i++)
        {
            ConsumableDataSO consumable = config.consumables[i];
            if (consumable == null)
                continue;

            runInventory.TrySetConsumable(i, consumable);
        }
    }

    private void ClearPassiveSlot()
    {
        for (int i = 0; i < RunInventoryManager.PASSIVE_SLOT_COUNT; i++)
            runInventory.ClearEquippedPassive(i);
    }

    private void ApplyRandomDiceLoadout()
    {
        BuildDicePrefabOptions();
        if (_dicePrefabOptions.Count <= 0)
        {
            Debug.LogWarning("[CombatLabPrototypeController] No d4/d8 prefab configured for prototype randomization.", this);
            return;
        }

        for (int i = 0; i < RunInventoryManager.EQUIPPED_DICE_COUNT; i++)
        {
            DiceSpinnerGeneric prefab = _dicePrefabOptions[Random.Range(0, _dicePrefabOptions.Count)];
            runInventory.SetEquippedDicePrefab(i, prefab);
        }

        for (int i = 0; i < RunInventoryManager.EQUIPPED_DICE_COUNT; i++)
        {
            DiceSpinnerGeneric runtimeDie = runInventory.GetEquippedDice(i);
            RandomizeRuntimeDieFaces(runtimeDie);
        }

        if (runInventory.DiceRig != null)
            runInventory.DiceRig.RefreshRollInfoCache();
    }

    private void RandomizeRuntimeDieFaces(DiceSpinnerGeneric die)
    {
        if (die == null || die.faces == null || die.faces.Length <= 0)
            return;

        int maxSpawnValue = die.faces.Length;
        for (int faceIndex = 0; faceIndex < die.faces.Length; faceIndex++)
        {
            int value = Random.Range(1, maxSpawnValue + 1);
            die.SetFaceValue(faceIndex, value);
        }
    }

    private void BuildDicePrefabOptions()
    {
        _dicePrefabOptions.Clear();
        if (config == null)
            return;

        if (config.d4Prefab != null)
            _dicePrefabOptions.Add(config.d4Prefab);
        if (config.d8Prefab != null)
            _dicePrefabOptions.Add(config.d8Prefab);
    }

    private void BuildSkillGroupOptions()
    {
        _skillGroupOptions.Clear();
        if (config == null || config.skillGroups == null)
            return;

        for (int i = 0; i < config.skillGroups.Length; i++)
        {
            CombatLabPrototypeConfigSO.SkillPairEntry group = config.skillGroups[i];
            if (group == null || !IsValidSkillGroup(group))
                continue;

            _skillGroupOptions.Add(group);
        }
    }

    private static bool IsValidSkillGroup(CombatLabPrototypeConfigSO.SkillPairEntry group)
    {
        return IsActiveSkill(group.skillA) &&
               IsActiveSkill(group.skillB) &&
               IsActiveSkill(group.skillC) &&
               IsActiveSkill(group.skillD);
    }

    private CombatLabPrototypeConfigSO.SkillPairEntry PickRandomSkillGroup()
    {
        if (_skillGroupOptions.Count <= 1)
        {
            CombatLabPrototypeConfigSO.SkillPairEntry onlyGroup = _skillGroupOptions[0];
            s_lastSelectedSkillGroupKey = BuildSkillGroupKey(onlyGroup);
            return onlyGroup;
        }

        int selectedIndex = Random.Range(0, _skillGroupOptions.Count);
        string selectedKey = BuildSkillGroupKey(_skillGroupOptions[selectedIndex]);

        if (!string.IsNullOrEmpty(s_lastSelectedSkillGroupKey) && selectedKey == s_lastSelectedSkillGroupKey)
        {
            int safety = 0;
            while (selectedKey == s_lastSelectedSkillGroupKey && safety < 16)
            {
                selectedIndex = Random.Range(0, _skillGroupOptions.Count);
                selectedKey = BuildSkillGroupKey(_skillGroupOptions[selectedIndex]);
                safety++;
            }

            if (selectedKey == s_lastSelectedSkillGroupKey)
            {
                for (int i = 0; i < _skillGroupOptions.Count; i++)
                {
                    string candidateKey = BuildSkillGroupKey(_skillGroupOptions[i]);
                    if (candidateKey == s_lastSelectedSkillGroupKey)
                        continue;

                    selectedIndex = i;
                    selectedKey = candidateKey;
                    break;
                }
            }
        }

        s_lastSelectedSkillGroupKey = selectedKey;
        return _skillGroupOptions[selectedIndex];
    }

    private static string BuildSkillGroupKey(CombatLabPrototypeConfigSO.SkillPairEntry group)
    {
        if (group == null)
            return string.Empty;

        return string.Join("|",
            GetSkillKey(group.skillA),
            GetSkillKey(group.skillB),
            GetSkillKey(group.skillC),
            GetSkillKey(group.skillD));
    }

    private static string GetSkillKey(ScriptableObject skill)
    {
        return skill != null ? skill.name : "null";
    }

    private static bool IsActiveSkill(ScriptableObject asset)
    {
        return asset is SkillDamageSO || asset is SkillBuffDebuffSO;
    }

    private void AutoResolveReferences()
    {
        if (party == null)
            party = FindFirstObjectByType<BattlePartyManager2D>(FindObjectsInactive.Include);
        if (runInventory == null)
            runInventory = FindFirstObjectByType<RunInventoryManager>(FindObjectsInactive.Include);
        if (turnManager == null)
            turnManager = FindFirstObjectByType<TurnManager>(FindObjectsInactive.Include);
        if (rewardController == null)
            rewardController = FindFirstObjectByType<RewardGachaDemoController>(FindObjectsInactive.Include);
    }

    private void HandleCombatVictoryResolved()
    {
        if (_runEnded)
            return;

        if (turnManager != null)
            turnManager.SetPlayerInteractionLocked(true);

        if (_currentCombatIndex >= CombatLabPrototypeConfigSO.PrototypeCombatCount - 1)
        {
            _runEnded = true;
            Debug.Log("[CombatLabPrototypeController] Prototype run complete after boss combat.", this);
            return;
        }

        ShowConsumableRewardThenStartCombat(_currentCombatIndex + 1);
    }

    private void ShowConsumableRewardThenStartCombat(int combatIndex)
    {
        if (turnManager != null)
            turnManager.SetPlayerInteractionLocked(true);

        if (!HasAvailableConsumableReward())
        {
            StartCombat(combatIndex);
            return;
        }

        if (rewardController == null)
        {
            Debug.LogWarning("[CombatLabPrototypeController] No RewardGachaDemoController assigned/found. Starting next combat without reward choice.", this);
            StartCombat(combatIndex);
            return;
        }

        rewardController.ShowConsumablePrototypeOffer(
            config.consumableRewardPool,
            BuildHeldConsumableSnapshot(),
            runInventory,
            _ =>
            {
                if (rewardController != null)
                    rewardController.gameObject.SetActive(false);
                StartCombat(combatIndex);
            });
    }

    private void StartCombat(int combatIndex)
    {
        _currentCombatIndex = Mathf.Clamp(combatIndex, 0, CombatLabPrototypeConfigSO.PrototypeCombatCount - 1);
        if (party != null)
            party.SpawnPrototypeEncounter(BuildEnemySpawnSlots(_currentCombatIndex), resetPlayerForBattle: true);

        if (turnManager != null)
            turnManager.BeginPrototypeCombat();
    }

    private bool HasAvailableConsumableReward()
    {
        if (config == null || config.consumableRewardPool == null || config.consumableRewardPool.Length == 0)
            return false;
        if (runInventory == null || runInventory.FindFirstEmptyConsumableSlot() < 0)
            return false;

        BuildHeldConsumableSnapshot();
        for (int i = 0; i < config.consumableRewardPool.Length; i++)
        {
            ConsumableDataSO candidate = config.consumableRewardPool[i];
            if (candidate != null && !_heldConsumables.Contains(candidate))
                return true;
        }

        return false;
    }

    private List<ConsumableDataSO> BuildHeldConsumableSnapshot()
    {
        _heldConsumables.Clear();
        if (runInventory == null)
            return _heldConsumables;

        for (int i = 0; i < runInventory.ConsumableCapacity; i++)
        {
            ConsumableDataSO data = runInventory.GetConsumable(i);
            if (data != null && !_heldConsumables.Contains(data))
                _heldConsumables.Add(data);
        }

        return _heldConsumables;
    }

    private static void Shuffle<T>(List<T> list)
    {
        if (list == null)
            return;

        for (int i = list.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            (list[i], list[swapIndex]) = (list[swapIndex], list[i]);
        }
    }
}
