using UnityEngine;

[CreateAssetMenu(menuName = "Dice Edit/Drag Profile", fileName = "DiceEditDragProfile")]
public class DiceEditDragProfileSO : ScriptableObject
{
    public enum RotationAxisMode
    {
        FreeXY,
        ZOnly
    }

    [Header("Free Drag")]
    public float rotationSpeed = 4f;
    public float verticalFlipSpeed = 3f;
    public bool invertHorizontalDrag;
    public RotationAxisMode rotationAxisMode = RotationAxisMode.FreeXY;
    public bool allowVerticalFlipInZOnly;

    [Header("Inertia")]
    public float inertiaDamping = 720f;
    public float flipInertiaDamping = 900f;
    public float maxRollVelocity = 540f;
    public float maxFlipVelocity = 360f;
    [Range(0f, 2f)] public float verticalFlipBias = 0.75f;

    [Header("Selection")]
    public float clickThresholdPixels = 10f;
}
