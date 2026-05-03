using UnityEngine;
using UnityEngine.EventSystems;

public class SlotIconPreviewInputForwarder : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
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

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (owner != null)
            owner.OnPointerEnter(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (owner != null)
            owner.OnPointerExit(eventData);
    }
}
