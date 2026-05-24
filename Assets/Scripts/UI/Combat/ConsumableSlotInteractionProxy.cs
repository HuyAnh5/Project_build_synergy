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
        if (IsLocalActionPanelClick(eventData))
            return;

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

    private bool IsLocalActionPanelClick(PointerEventData eventData)
    {
        return IsLocalActionPanelObject(eventData != null ? eventData.pointerPress : null) ||
               IsLocalActionPanelObject(eventData != null ? eventData.pointerEnter : null) ||
               IsLocalActionPanelObject(eventData != null ? eventData.rawPointerPress : null) ||
               IsLocalActionPanelObject(eventData != null ? eventData.pointerCurrentRaycast.gameObject : null);
    }

    private bool IsLocalActionPanelObject(GameObject candidate)
    {
        if (candidate == null)
            return false;

        Transform current = candidate.transform;
        while (current != null && current != transform)
        {
            if (current.name == "LocalActionPanel" || current.name == "UseButton" || current.name == "SellButton")
                return true;

            current = current.parent;
        }

        return false;
    }
}
