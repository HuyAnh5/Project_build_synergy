using UnityEngine;

internal static class DiceEquipStateUtility
{
    public static void RebindCombatSlotLaneIndices(RectTransform[] linkedCombatSlotAnchors, TurnManager turnManager)
    {
        if (linkedCombatSlotAnchors == null || linkedCombatSlotAnchors.Length == 0)
            return;

        for (int lane0 = 0; lane0 < linkedCombatSlotAnchors.Length; lane0++)
        {
            RectTransform laneRoot = linkedCombatSlotAnchors[lane0];
            if (laneRoot == null)
                continue;

            ActionSlotDrop drop = laneRoot.GetComponent<ActionSlotDrop>();
            if (drop == null)
                drop = laneRoot.GetComponentInChildren<ActionSlotDrop>(true);

            if (drop != null)
            {
                if (drop.turn == null && turnManager != null)
                    drop.turn = turnManager;

                drop.SetVisualLaneIndex(lane0 + 1);
            }

            SlotIconDragToClear clear = laneRoot.GetComponent<SlotIconDragToClear>();
            if (clear == null)
                clear = laneRoot.GetComponentInChildren<SlotIconDragToClear>(true);

            if (clear != null)
            {
                if (clear.turn == null && turnManager != null)
                    clear.turn = turnManager;

                clear.SetVisualLaneIndex(lane0 + 1);
            }
        }
    }
}
