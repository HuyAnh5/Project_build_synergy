public partial class RunInventoryManager
{
#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureSizes();
        ApplyBindingsToIcons();
        RunInventorySetupUtility.BootstrapEquippedDiceFromRigIfNeeded(diceRig, equippedDicePrefabs, equippedDice);
        if (!HasAnyAssignedDicePrefabs())
        {
            SyncDiceRigFromInventory();
        }
    }
#endif
}
