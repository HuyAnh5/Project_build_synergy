using UnityEngine;
using UnityEngine.EventSystems;

public class SlotIconPreviewInputForwarder : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    public SlotIconDragToClear owner;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (owner != null)
            owner.OnBeginDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (owner != null)
            owner.OnDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (owner != null)
            owner.OnEndDrag(eventData);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (owner != null)
            owner.OnPointerClick(eventData);
    }
}
