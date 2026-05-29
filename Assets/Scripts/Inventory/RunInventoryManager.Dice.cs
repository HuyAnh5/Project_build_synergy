using System.Collections.Generic;
using UnityEngine;

public partial class RunInventoryManager
{
    public DiceSpinnerGeneric GetEquippedDice(int index)
    {
        return RunInventoryLoadoutUtility.GetAt(equippedDice, index);
    }

    public DiceSpinnerGeneric GetEquippedDicePrefab(int index)
    {
        return RunInventoryLoadoutUtility.GetAt(equippedDicePrefabs, index);
    }

    public void FillEquippedDice(List<DiceSpinnerGeneric> buffer)
    {
        RunInventoryLoadoutUtility.Fill(equippedDice, buffer);
    }

    public int FindFirstEmptyEquippedDiceSlot()
    {
        return RunInventoryLoadoutUtility.FindFirstEmpty(equippedDice);
    }

    public bool IsDiceLoadoutFull()
    {
        return FindFirstEmptyEquippedDiceSlot() < 0;
    }

    public bool TryAddDiceToFirstEmptySlot(DiceSpinnerGeneric dice)
    {
        if (IsPrefabAsset(dice))
        {
            return TryAddDicePrefabToFirstEmptySlot(dice);
        }

        if (!RunInventoryLoadoutUtility.TryAddToFirstEmpty(equippedDice, dice, out int addedIndex))
        {
            return false;
        }

        equippedDicePrefabs[addedIndex] = ResolveTrackedPrefabForRuntime(dice);
        SyncDiceRigFromInventory();
        InventoryChanged?.Invoke();
        return true;
    }

    public bool TryAddDicePrefabToFirstEmptySlot(DiceSpinnerGeneric dicePrefab)
    {
        if (!RunInventoryLoadoutUtility.TryAddToFirstEmpty(equippedDicePrefabs, dicePrefab, out _))
        {
            return false;
        }

        RebuildSpawnedEquippedDiceFromPrefabs();
        SyncDiceRigFromInventory();
        InventoryChanged?.Invoke();
        return true;
    }

    public void SetEquippedDice(int index, DiceSpinnerGeneric dice)
    {
        if (IsPrefabAsset(dice))
        {
            SetEquippedDicePrefab(index, dice);
            return;
        }

        DestroyIfSpawned(equippedDice[index]);
        if (!RunInventoryLoadoutUtility.SetAt(equippedDice, index, dice))
        {
            return;
        }

        equippedDicePrefabs[index] = ResolveTrackedPrefabForRuntime(dice);
        SyncDiceRigFromInventory();
        InventoryChanged?.Invoke();
    }

    public void SetEquippedDicePrefab(int index, DiceSpinnerGeneric dicePrefab)
    {
        if (!RunInventoryLoadoutUtility.SetAt(equippedDicePrefabs, index, dicePrefab))
        {
            return;
        }

        RebuildSpawnedEquippedDiceFromPrefabs();
        SyncDiceRigFromInventory();
        InventoryChanged?.Invoke();
    }

    public void SwapEquippedDice(int a, int b)
    {
        if (!RunInventoryLoadoutUtility.Swap(equippedDice, a, b))
        {
            return;
        }

        RunInventoryLoadoutUtility.Swap(equippedDicePrefabs, a, b);
        SyncDiceRigFromInventory();
        InventoryChanged?.Invoke();
    }

    public void ClearEquippedDice(int index)
    {
        DestroyIfSpawned(RunInventoryLoadoutUtility.GetAt(equippedDice, index));
        if (!RunInventoryLoadoutUtility.SetAt<DiceSpinnerGeneric>(equippedDice, index, null))
        {
            return;
        }

        RunInventoryLoadoutUtility.SetAt<DiceSpinnerGeneric>(equippedDicePrefabs, index, null);
        SyncDiceRigFromInventory();
        InventoryChanged?.Invoke();
    }

    public bool RemoveEquippedDice(DiceSpinnerGeneric dice)
    {
        DestroyIfSpawned(dice);
        if (!RunInventoryLoadoutUtility.RemoveReference(equippedDice, dice))
        {
            return false;
        }

        SyncPrefabLayoutFromRuntime();
        SyncDiceRigFromInventory();
        InventoryChanged?.Invoke();
        return true;
    }

    public void SetDiceLayout(DiceSpinnerGeneric[] equipped, bool notifyChanged = true)
    {
        EnsureSizes();
        DiceSpinnerGeneric[] previousLayout = new DiceSpinnerGeneric[equippedDice.Length];
        RunInventoryLoadoutUtility.CopyLayout(previousLayout, equippedDice);
        RunInventoryLoadoutUtility.CopyLayout(equippedDice, equipped);
        DestroyRemovedSpawnedDice(previousLayout, equippedDice);
        SyncPrefabLayoutFromRuntime();
        SyncDiceRigFromInventory();
        if (notifyChanged)
        {
            InventoryChanged?.Invoke();
        }
    }

    public void SyncDiceRigFromInventory()
    {
        RunInventoryLoadoutUtility.SyncDiceRig(diceRig, equippedDice);
    }

    public bool ContainsEquippedDice(DiceSpinnerGeneric dice)
    {
        return RunInventoryLoadoutUtility.ContainsReference(equippedDice, dice);
    }

    private bool HasAnyAssignedDicePrefabs()
    {
        for (int i = 0; i < equippedDicePrefabs.Length; i++)
        {
            if (equippedDicePrefabs[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    private void RebuildSpawnedEquippedDiceFromPrefabs()
    {
        ClearSpawnedEquippedDiceInstances();
        if (diceRig == null)
        {
            return;
        }

        for (int i = 0; i < equippedDicePrefabs.Length; i++)
        {
            DiceSpinnerGeneric prefab = equippedDicePrefabs[i];
            if (prefab == null)
            {
                equippedDice[i] = null;
                continue;
            }

            DiceSpinnerGeneric instance = SpawnEquippedDiceInstance(i, prefab);
            equippedDice[i] = instance;
            if (instance != null)
            {
                m_spawnedPrefabByRuntime[instance] = prefab;
            }
        }
    }

    private void ClearSpawnedEquippedDiceInstances()
    {
        for (int i = 0; i < equippedDice.Length; i++)
        {
            DiceSpinnerGeneric runtime = equippedDice[i];
            if (runtime == null)
            {
                continue;
            }

            if (m_spawnedPrefabByRuntime.ContainsKey(runtime))
            {
                DestroyDiceInstance(runtime.gameObject);
            }

            equippedDice[i] = null;
        }

        m_spawnedPrefabByRuntime.Clear();
    }

    private void DestroyRemovedSpawnedDice(DiceSpinnerGeneric[] previousLayout, DiceSpinnerGeneric[] nextLayout)
    {
        if (previousLayout == null)
        {
            return;
        }

        for (int i = 0; i < previousLayout.Length; i++)
        {
            DiceSpinnerGeneric previous = previousLayout[i];
            if (previous == null || !m_spawnedPrefabByRuntime.ContainsKey(previous))
            {
                continue;
            }

            bool stillPresent = false;
            if (nextLayout != null)
            {
                for (int j = 0; j < nextLayout.Length; j++)
                {
                    if (nextLayout[j] == previous)
                    {
                        stillPresent = true;
                        break;
                    }
                }
            }

            if (!stillPresent)
            {
                DestroyIfSpawned(previous);
            }
        }
    }

    private DiceSpinnerGeneric SpawnEquippedDiceInstance(int slotIndex, DiceSpinnerGeneric prefab)
    {
        if (prefab == null)
        {
            return null;
        }

        Transform parent = diceRig != null &&
                           diceRig.slots != null &&
                           slotIndex >= 0 &&
                           slotIndex < diceRig.slots.Length &&
                           diceRig.slots[slotIndex] != null &&
                           diceRig.slots[slotIndex].slotRoot != null
            ? diceRig.slots[slotIndex].slotRoot.transform
            : (diceRig != null ? diceRig.transform : transform);

        GameObject instanceGo = Instantiate(prefab.gameObject, parent, false);
        instanceGo.name = prefab.gameObject.name;
        instanceGo.transform.localPosition = Vector3.zero;
        instanceGo.transform.localRotation = Quaternion.identity;
        instanceGo.transform.localScale = prefab.transform.localScale;
        return instanceGo.GetComponent<DiceSpinnerGeneric>();
    }

    private void SyncPrefabLayoutFromRuntime()
    {
        for (int i = 0; i < equippedDice.Length; i++)
        {
            equippedDicePrefabs[i] = ResolveTrackedPrefabForRuntime(equippedDice[i]);
        }
    }

    private DiceSpinnerGeneric ResolveTrackedPrefabForRuntime(DiceSpinnerGeneric runtime)
    {
        if (runtime == null)
        {
            return null;
        }

        if (m_spawnedPrefabByRuntime.TryGetValue(runtime, out DiceSpinnerGeneric prefab))
        {
            return prefab;
        }

        return null;
    }

    private void DestroyIfSpawned(DiceSpinnerGeneric runtime)
    {
        if (runtime == null)
        {
            return;
        }

        if (!m_spawnedPrefabByRuntime.Remove(runtime))
        {
            return;
        }

        DestroyDiceInstance(runtime.gameObject);
    }

    private static bool IsPrefabAsset(DiceSpinnerGeneric dice)
    {
        if (dice == null)
        {
            return false;
        }

        return !dice.gameObject.scene.IsValid();
    }

    private static void DestroyDiceInstance(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }
}
