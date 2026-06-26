using System;
using Sirenix.OdinInspector;
using UnityEngine;

public partial class RunInventoryManager
{
    [Serializable]
    public struct ConsumableSlot
    {
        public ConsumableDataSO asset;
        public int charges;

        public bool IsEmpty
        {
            get { return asset == null; }
        }
    }

    [Serializable]
    public class SlotBinding
    {
        [LabelText("UI Icon")]
        [Tooltip("Drag the DraggableSkillIcon for this slot here.")]
        public DraggableSkillIcon uiIcon;

        [LabelText("Skill")]
        [Tooltip("The actual skill asset stored in this slot (changes often).")]
        public ScriptableObject skillAsset;
    }

    [Serializable]
    public class PassiveSlotBinding
    {
        [LabelText("Passive")]
        [Tooltip("Legacy passive asset field retained for compatibility with passive inventory/runtime sync.")]
        public SkillPassiveSO passiveAsset;
    }

    /// <summary>
    /// Pushes the inventory data currently stored in slot bindings back into the bound UI icons.
    /// </summary>
    [Button(ButtonSizes.Medium)]
    private void ApplyBindingsToIcons()
    {
        EnsureSizes();
        RunInventoryBindingUtility.ApplyBindingsToIcons(this, ownedSlots);

        InventoryChanged?.Invoke();
        Debug.Log("[RunInventoryManager] Applied slot bindings to UI icons.");
    }

    /// <summary>
    /// Rebuilds runtime dice instances from prefab slots so inspector changes are reflected immediately.
    /// </summary>
    [Button(ButtonSizes.Medium)]
    private void RebuildSpawnedEquippedDiceFromPrefabsButton()
    {
        EnsureSizes();
        RebuildSpawnedEquippedDiceFromPrefabs();
        SyncDiceRigFromInventory();
        InventoryChanged?.Invoke();
    }
}
