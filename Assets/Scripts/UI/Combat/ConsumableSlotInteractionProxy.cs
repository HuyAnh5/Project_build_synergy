using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class ConsumableSlotInteractionProxy : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public ConsumableBarUIManager manager;
    public int slotIndex;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (manager != null)
            manager.HandleSlotHoverEnter(slotIndex);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (manager != null)
            manager.HandleSlotHoverExit(slotIndex);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (manager != null)
            manager.HandleSlotClicked(slotIndex);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (manager != null)
            manager.HandleSlotBeginDrag(slotIndex, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (manager != null)
            manager.HandleSlotDrag(slotIndex, eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (manager != null)
            manager.HandleSlotEndDrag(slotIndex, eventData);
    }
}
