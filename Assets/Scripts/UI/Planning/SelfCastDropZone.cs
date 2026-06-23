using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SelfCastDropZone : MonoBehaviour
{
    [SerializeField] private RectTransform zoneRect;

    private void Awake()
    {
        SelfCastDropZoneRegistry.Register(this);
        if (zoneRect == null)
            zoneRect = transform as RectTransform;
    }

    private void OnDestroy()
    {
        SelfCastDropZoneRegistry.Unregister(this);
    }

    public bool ContainsScreenPoint(Vector2 screenPoint, Camera uiCamera)
    {
        if (zoneRect == null)
            zoneRect = transform as RectTransform;

        return zoneRect != null &&
               RectTransformUtility.RectangleContainsScreenPoint(zoneRect, screenPoint, uiCamera);
    }
}

internal static class SelfCastDropZoneRegistry
{
    private static SelfCastDropZone _instance;

    public static void Register(SelfCastDropZone zone)
    {
        if (zone == null)
            return;

        _instance = zone;
    }

    public static void Unregister(SelfCastDropZone zone)
    {
        if (_instance == zone)
            _instance = null;
    }

    public static SelfCastDropZone Get()
    {
        if (_instance != null)
            return _instance;

#if UNITY_2023_1_OR_NEWER
        _instance = UnityEngine.Object.FindFirstObjectByType<SelfCastDropZone>(FindObjectsInactive.Include);
#else
        _instance = UnityEngine.Object.FindObjectOfType<SelfCastDropZone>(true);
#endif
        return _instance;
    }
}
