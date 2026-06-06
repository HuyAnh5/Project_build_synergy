using UnityEngine;
using UnityEngine.EventSystems;

public class DiceEquipDropZone : MonoBehaviour, IDropHandler
{
    public DiceEquipUIManager manager;
    [Range(0, 2)] public int slotIndex = 0;

    public void OnDrop(PointerEventData eventData)
    {
        if (manager == null) return;

        DiceDraggableUI drag = ResolveDraggedDice(eventData.pointerDrag);

        if (drag != null)
            manager.HandleDropToEquipSlot(drag, slotIndex);
    }

    private static DiceDraggableUI ResolveDraggedDice(GameObject pointerDrag)
    {
        if (pointerDrag == null)
            return null;

        DiceDraggableUI drag = pointerDrag.GetComponent<DiceDraggableUI>();
        if (drag != null)
            return drag;

        DiceEnchantHoverProxy enchantProxy = pointerDrag.GetComponent<DiceEnchantHoverProxy>();
        return enchantProxy != null ? enchantProxy.Owner : null;
    }
}
