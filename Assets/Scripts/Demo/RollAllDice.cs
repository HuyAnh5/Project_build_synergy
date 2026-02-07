using UnityEngine;

public class RollAllDice : MonoBehaviour
{
    [Tooltip("Prefer using DiceSlotRig. If not set, will roll the array below.")]
    public DiceSlotRig rig;

    public DiceSpinnerGeneric[] dice;

    public void Roll()
    {
        if (rig != null)
        {
            rig.RollOnce();
            return;
        }

        if (dice == null) return;
        foreach (var d in dice)
            if (d != null) d.RollRandomFace();
    }
}
