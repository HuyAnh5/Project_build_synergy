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
    private readonly List<CombatLabPrototypeConfigSO.SkillPairEntry> _skillGroupOptions = new List<CombatLabPrototypeConfigSO.SkillPairEntry>(6);
    private readonly List<ScriptableObject> _selectedSkills = new List<ScriptableObject>(4);
    private static string s_lastSelectedSkillGroupKey;

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
