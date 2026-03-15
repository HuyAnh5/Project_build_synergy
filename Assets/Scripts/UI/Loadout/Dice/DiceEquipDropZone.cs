using UnityEngine;
using UnityEngine.EventSystems;

public class DiceEquipDropZone : MonoBehaviour, IDropHandler
{
    public DiceEquipUIManager manager;
    [Range(0, 2)] public int slotIndex = 0;

    public void OnDrop(PointerEventData eventData)
    {
        if (manager == null) return;

        DiceDraggableUI drag = eventData.pointerDrag != null
            ? eventData.pointerDrag.GetComponent<DiceDraggableUI>()
            : null;

        if (drag != null)
            manager.HandleDropToEquipSlot(drag, slotIndex);
    }
}
