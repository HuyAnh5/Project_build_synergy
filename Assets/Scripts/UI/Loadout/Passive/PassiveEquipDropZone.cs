using UnityEngine;
using UnityEngine.EventSystems;

public class PassiveEquipDropZone : MonoBehaviour, IDropHandler
{
    public PassiveEquipUIManager manager;
    [Range(0, 0)] public int slotIndex = 0;

    public void OnDrop(PointerEventData eventData)
    {
        if (manager == null) return;

        PassiveDraggableUI drag = eventData.pointerDrag != null
            ? eventData.pointerDrag.GetComponent<PassiveDraggableUI>()
            : null;

        if (drag != null)
            manager.HandleDropToEquipSlot(drag, slotIndex);
    }
}
