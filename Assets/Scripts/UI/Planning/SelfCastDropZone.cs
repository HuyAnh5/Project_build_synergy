using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SelfCastDropZone : MonoBehaviour
{
    [SerializeField] private RectTransform zoneRect;

    private void Awake()
    {
        if (zoneRect == null)
            zoneRect = transform as RectTransform;
    }

    public bool ContainsScreenPoint(Vector2 screenPoint, Camera uiCamera)
    {
        if (zoneRect == null)
            zoneRect = transform as RectTransform;

        return zoneRect != null &&
               RectTransformUtility.RectangleContainsScreenPoint(zoneRect, screenPoint, uiCamera);
    }
}
