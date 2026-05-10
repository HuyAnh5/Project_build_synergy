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
    [SerializeField] private bool shuffleSelectedSkillOrder = true;

    private readonly List<DiceSpinnerGeneric> _dicePrefabOptions = new List<DiceSpinnerGeneric>(2);
    private readonly List<CombatLabPrototypeConfigSO.SkillPairEntry> _skillPairOptions = new List<CombatLabPrototypeConfigSO.SkillPairEntry>(6);
    private readonly List<ScriptableObject> _selectedSkills = new List<ScriptableObject>(4);

    private void Awake()
    {
        AutoResolveReferences();
        ApplyEncounterAuthoringToParty();
    }

    private void Start()
    {
        AutoResolveReferences();
        ApplyPrototypeLoadout();
    }

    [ContextMenu("Apply Prototype Loadout")]
    public void ApplyPrototypeLoadout()
    {
        if (config == null || runInventory == null)
            return;

        ApplyRandomSkillLoadout();
        ApplyFixedConsumables();
        ClearPassiveSlot();
        ApplyRandomDiceLoadout();

        if (turnManager != null)
            turnManager.SetPlayerInteractionLocked(false);
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

        party.enemySlots = BuildEnemySpawnSlots();
    }

    private BattlePartyManager2D.SpawnSlot[] BuildEnemySpawnSlots()
    {
        BattlePartyManager2D.SpawnSlot[] result = new BattlePartyManager2D.SpawnSlot[3];
        if (config.enemies == null)
            return result;

        int writeIndex = 0;
        for (int i = 0; i < config.enemies.Length && writeIndex < result.Length; i++)
        {
            CombatLabPrototypeConfigSO.EnemyEntry entry = config.enemies[i];
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

    private void ApplyRandomSkillLoadout()
    {
        for (int i = 0; i < RunInventoryManager.OWNED_SKILL_COUNT; i++)
            runInventory.SetSkill(RunInventoryManager.SkillSource.Owned, i, null);

        BuildSkillPairOptions();
        if (_skillPairOptions.Count < 2)
        {
            Debug.LogWarning("[CombatLabPrototypeController] Need at least 2 valid skill pairs configured.", this);
            return;
        }

        int firstIndex = Random.Range(0, _skillPairOptions.Count);
        int secondIndex = firstIndex;
        while (secondIndex == firstIndex)
            secondIndex = Random.Range(0, _skillPairOptions.Count);

        CombatLabPrototypeConfigSO.SkillPairEntry firstPair = _skillPairOptions[firstIndex];
        CombatLabPrototypeConfigSO.SkillPairEntry secondPair = _skillPairOptions[secondIndex];

        _selectedSkills.Clear();
        _selectedSkills.Add(firstPair.skillA);
        _selectedSkills.Add(firstPair.skillB);
        _selectedSkills.Add(secondPair.skillA);
        _selectedSkills.Add(secondPair.skillB);

        if (shuffleSelectedSkillOrder)
            Shuffle(_selectedSkills);

        for (int i = 0; i < _selectedSkills.Count && i < RunInventoryManager.OWNED_SKILL_COUNT; i++)
            runInventory.SetSkill(RunInventoryManager.SkillSource.Owned, i, _selectedSkills[i]);
    }

    private void ApplyFixedConsumables()
    {
        for (int i = runInventory.ConsumableCapacity - 1; i >= 0; i--)
            runInventory.ClearConsumable(i);

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

    private void BuildSkillPairOptions()
    {
        _skillPairOptions.Clear();
        if (config == null || config.skillPairs == null)
            return;

        for (int i = 0; i < config.skillPairs.Length; i++)
        {
            CombatLabPrototypeConfigSO.SkillPairEntry pair = config.skillPairs[i];
            if (pair == null || !IsValidSkillPair(pair))
                continue;

            _skillPairOptions.Add(pair);
        }
    }

    private static bool IsValidSkillPair(CombatLabPrototypeConfigSO.SkillPairEntry pair)
    {
        return IsActiveSkill(pair.skillA) &&
               IsActiveSkill(pair.skillB);
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
