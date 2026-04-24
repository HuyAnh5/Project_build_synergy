using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class ConsumableSlotInteractionProxy : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
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
}
