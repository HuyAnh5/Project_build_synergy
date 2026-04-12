using UnityEngine;

/// <summary>
/// Single-slot passive loadout UI.
/// Passive loadout no longer supports drag, reorder, or swap.
/// </summary>
public class PassiveEquipUIManager : MonoBehaviour
{
    [Header("Wiring")]
    public RectTransform[] equipSlotAnchors = new RectTransform[RunInventoryManager.PASSIVE_SLOT_COUNT];
    public RunInventoryManager runInventory;

    [Header("Behavior")]
    public bool interactionsLocked;

    public PassiveDraggableUI[] equipped = new PassiveDraggableUI[RunInventoryManager.PASSIVE_SLOT_COUNT];

    private void Awake()
    {
        if (runInventory == null)
            runInventory = GetComponentInParent<RunInventoryManager>(true);

        BootstrapFromAnchorsIfNeeded();
    }

    private void Start()
    {
        BootstrapFromAnchorsIfNeeded();
        RefreshAllSlots();
        SyncToRunInventory();
    }

    public bool CanInteract() => !interactionsLocked;

    public void Register(PassiveDraggableUI passive)
    {
        if (passive == null) return;
        passive.manager = this;
        passive.RefreshVisual();
    }

    [ContextMenu("Rebuild From Children")]
    public void RebuildFromChildren()
    {
        for (int i = 0; i < equipped.Length; i++)
            equipped[i] = null;

        for (int i = 0; i < equipSlotAnchors.Length; i++)
        {
            RectTransform anchor = equipSlotAnchors[i];
            if (anchor == null) continue;

            PassiveDraggableUI passive = anchor.GetComponentInChildren<PassiveDraggableUI>(true);
            if (passive == null) continue;

            Register(passive);
            equipped[i] = passive;
        }

        CompactEquipped();
        RefreshAllSlots();
        SyncToRunInventory();
    }

    public void HandleDropToEquipSlot(PassiveDraggableUI passive, int slotIndex)
    {
        if (!CanInteract() || passive == null) return;

        equipped[0] = passive;
        CompactEquipped();
        RefreshAllSlots();
        SyncToRunInventory();
    }

    public void HandleInvalidDrop(PassiveDraggableUI passive)
    {
        if (passive == null) return;
        RefreshAllSlots();
    }

    private void BootstrapFromAnchorsIfNeeded()
    {
        if (equipSlotAnchors == null || equipSlotAnchors.Length == 0)
            return;

        bool hasSerializedEquipped = false;
        bool needsManagerRebind = false;
        bool foundPassiveChildInAnchor = false;

        EnsureSingleSlotArrays();

        for (int i = 0; i < equipped.Length; i++)
        {
            PassiveDraggableUI passive = equipped[i];
            if (passive == null)
                continue;

            hasSerializedEquipped = true;
            if (passive.manager != this)
                needsManagerRebind = true;
        }

        for (int i = 0; i < equipSlotAnchors.Length; i++)
        {
            RectTransform anchor = equipSlotAnchors[i];
            if (anchor == null)
                continue;

            PassiveDraggableUI childPassive = anchor.GetComponentInChildren<PassiveDraggableUI>(true);
            if (childPassive == null)
                continue;

            foundPassiveChildInAnchor = true;
            if (childPassive.manager != this)
                needsManagerRebind = true;
        }

        if (!foundPassiveChildInAnchor)
            return;

        if (!hasSerializedEquipped || needsManagerRebind)
        {
            for (int i = 0; i < equipped.Length; i++)
                equipped[i] = null;

            for (int i = 0; i < equipSlotAnchors.Length && i < equipped.Length; i++)
            {
                RectTransform anchor = equipSlotAnchors[i];
                if (anchor == null)
                    continue;

                PassiveDraggableUI passive = anchor.GetComponentInChildren<PassiveDraggableUI>(true);
                if (passive == null)
                    continue;

                Register(passive);
                equipped[i] = passive;
            }

            CompactEquipped();
        }
        else
        {
            for (int i = 0; i < equipped.Length; i++)
                Register(equipped[i]);
        }

        for (int i = 1; i < equipSlotAnchors.Length; i++)
        {
            if (equipSlotAnchors[i] != null)
                equipSlotAnchors[i].gameObject.SetActive(false);
        }
    }

    private void CompactEquipped()
        => equipped = PassiveEquipStateUtility.Compact(equipped);

    private void RefreshAllSlots(bool instant = false)
        => PassiveEquipPresentationUtility.RefreshSingleSlot(equipped, equipSlotAnchors, instant, this);

    private void SyncToRunInventory()
        => PassiveEquipPresentationUtility.SyncToRunInventory(equipped, runInventory);

    private void EnsureSingleSlotArrays()
    {
        if (equipped == null || equipped.Length != RunInventoryManager.PASSIVE_SLOT_COUNT)
        {
            PassiveDraggableUI first = equipped != null && equipped.Length > 0 ? equipped[0] : null;
            equipped = new PassiveDraggableUI[RunInventoryManager.PASSIVE_SLOT_COUNT];
            equipped[0] = first;
        }
    }
}
