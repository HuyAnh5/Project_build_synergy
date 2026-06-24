using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public partial class DiceDraggableUI
{
    internal void HandleEnchantHoverEnter()
    {
        EnsureInitialized();
        _enchantHoverTooltipActive = true;
        RefreshHoverTooltip();
    }

    internal void HandleEnchantHoverExit()
    {
        _enchantHoverTooltipActive = false;
        SkillTooltipUI.HideCurrentUnlessPointerOverTooltip(gameObject);
    }

    

    

    

    

    

    

    

    

    

    private static Camera GetCanvasEventCamera(Canvas canvas)
    {
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera;
    }
}

public sealed class DiceEnchantHoverProxy : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    private DiceDraggableUI _owner;

    public DiceDraggableUI Owner => _owner;

    public void Bind(DiceDraggableUI owner)
    {
        _owner = owner;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _owner?.HandleEnchantHoverEnter();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _owner?.HandleEnchantHoverExit();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _owner?.OnBeginDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        _owner?.OnDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _owner?.OnEndDrag(eventData);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        _owner?.OnPointerClick(eventData);
    }
}
