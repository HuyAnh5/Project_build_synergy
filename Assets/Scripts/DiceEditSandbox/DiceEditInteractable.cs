using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(DiceSpinnerGeneric))]
public class DiceEditInteractable : MonoBehaviour
{
    private const bool DebugLogs = false;

    [SerializeField] private float rotationSpeed = 0.2f;
    [SerializeField] private float verticalFlipSpeed = 1.2f;
    [SerializeField] private float clickThresholdPixels = 10f;
    [SerializeField] private bool invertHorizontalDrag;
    [SerializeField] private DiceEditDragProfileSO dragProfile;
    [SerializeField] private DiceEditDragProfileSO.RotationAxisMode rotationAxisMode = DiceEditDragProfileSO.RotationAxisMode.FreeXY;
    [SerializeField] private bool allowVerticalFlipInZOnly;
    [SerializeField] private float inertiaDamping = 1400f;
    [SerializeField] private float flipInertiaDamping = 1600f;
    [SerializeField] private float maxRollVelocity = 200f;
    [SerializeField] private float maxFlipVelocity = 140f;
    [SerializeField, Range(0f, 2f)] private float verticalFlipBias = 0.75f;

    private DiceEditSandboxController _controller;
    private DiceSpinnerGeneric _spinner;
    private DiceFaceSelectionMap _selectionMap;
    private DiceFaceHighlightRenderer _highlightRenderer;
    private Camera _cachedMainCamera;
    private bool _bindingsReady;
    private bool _rollCompleteSubscribed;

    private bool _dragging;
    private Vector3 _pointerDownPosition;
    private Vector3 _lastPointerPosition;
    private float _rollVelocity;
    private float _flipVelocity;
    private float _yawVelocity;
    private float _pitchVelocity;
    private readonly List<int> _highlightFaces = new List<int>();
    private readonly List<DiceEditSandboxController.SandboxFaceHighlightKind> _highlightKinds = new List<DiceEditSandboxController.SandboxFaceHighlightKind>();

    private void OnDestroy()
    {
        if (_spinner != null && _rollCompleteSubscribed)
            _spinner.onRollComplete -= HandleRollComplete;
    }

    private void Update()
    {
        if (!_bindingsReady)
            EnsureBound();

        if (_dragging || _spinner == null || _spinner.pivot == null)
            return;

        float dt = Time.unscaledDeltaTime;
        if (dt <= 0f)
            return;

        DiceEditDragProfileSO.RotationAxisMode axisMode = dragProfile != null ? dragProfile.rotationAxisMode : rotationAxisMode;
        Transform camTransform = GetMainCameraTransform();

        if (axisMode == DiceEditDragProfileSO.RotationAxisMode.FreeXY)
        {
            if (!Mathf.Approximately(_yawVelocity, 0f))
            {
                float yawStep = _yawVelocity * dt;
                _spinner.pivot.Rotate(camTransform != null ? camTransform.up : Vector3.up, yawStep, Space.World);
                _yawVelocity = Mathf.MoveTowards(_yawVelocity, 0f, GetInertiaDamping() * dt);
            }

            if (!Mathf.Approximately(_pitchVelocity, 0f))
            {
                float pitchStep = _pitchVelocity * dt;
                _spinner.pivot.Rotate(camTransform != null ? camTransform.right : Vector3.right, pitchStep, Space.World);
                _pitchVelocity = Mathf.MoveTowards(_pitchVelocity, 0f, GetFlipInertiaDamping() * dt);
            }

            return;
        }

        if (!Mathf.Approximately(_rollVelocity, 0f))
        {
            float rollStep = _rollVelocity * dt;
            _spinner.pivot.Rotate(Vector3.forward, rollStep, Space.Self);
            _rollVelocity = Mathf.MoveTowards(_rollVelocity, 0f, GetInertiaDamping() * dt);
        }

        if (!Mathf.Approximately(_flipVelocity, 0f))
        {
            float flipStep = _flipVelocity * dt;
            _spinner.pivot.Rotate(Vector3.right, flipStep, Space.Self);
            _flipVelocity = Mathf.MoveTowards(_flipVelocity, 0f, GetFlipInertiaDamping() * dt);
        }
    }

    public void Configure(DiceEditSandboxController controller, DiceSpinnerGeneric spinner)
    {
        _controller = controller;
        _spinner = spinner != null ? spinner : GetComponent<DiceSpinnerGeneric>();

        _selectionMap = GetComponent<DiceFaceSelectionMap>();
        if (_selectionMap == null)
            _selectionMap = gameObject.AddComponent<DiceFaceSelectionMap>();

        _selectionMap.Configure(_spinner);

        _highlightRenderer = GetComponent<DiceFaceHighlightRenderer>();
        if (_highlightRenderer == null)
            _highlightRenderer = gameObject.AddComponent<DiceFaceHighlightRenderer>();

        _highlightRenderer.Configure(_spinner, _selectionMap);
        _cachedMainCamera = Camera.main;
        SubscribeRollComplete();
        _bindingsReady = _controller != null && _spinner != null && _selectionMap != null && _highlightRenderer != null;
        RefreshHighlight();
    }

    public void HandleMouseDown()
    {
        if (!_bindingsReady)
            EnsureBound();
        _dragging = true;
        _pointerDownPosition = Input.mousePosition;
        _lastPointerPosition = _pointerDownPosition;
        _rollVelocity = 0f;
        _flipVelocity = 0f;
        _yawVelocity = 0f;
        _pitchVelocity = 0f;
        _controller?.SetFocusedInteractable(this);
    }

    public void HandleMouseDrag()
    {
        if (!_bindingsReady)
            EnsureBound();
        if (!_dragging || _spinner == null || _spinner.pivot == null)
            return;

        Vector3 currentPointerPosition = Input.mousePosition;
        Vector3 pointerDelta = currentPointerPosition - _lastPointerPosition;
        _lastPointerPosition = currentPointerPosition;

        float deltaX = pointerDelta.x;
        float deltaY = pointerDelta.y;

        if (Mathf.Approximately(deltaX, 0f) && Mathf.Approximately(deltaY, 0f))
            return;

        float speed = dragProfile != null ? dragProfile.rotationSpeed : rotationSpeed;
        float flipSpeed = dragProfile != null ? dragProfile.verticalFlipSpeed : verticalFlipSpeed;
        float flipBias = dragProfile != null ? dragProfile.verticalFlipBias : verticalFlipBias;
        bool invert = dragProfile != null ? dragProfile.invertHorizontalDrag : invertHorizontalDrag;
        bool allowVerticalFlip = dragProfile != null ? dragProfile.allowVerticalFlipInZOnly : allowVerticalFlipInZOnly;
        DiceEditDragProfileSO.RotationAxisMode axisMode = dragProfile != null ? dragProfile.rotationAxisMode : rotationAxisMode;
        float dt = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
        Transform camTransform = GetMainCameraTransform();

        if (axisMode == DiceEditDragProfileSO.RotationAxisMode.ZOnly)
        {
            float roll = (invert ? deltaX : -deltaX) * speed;
            float clampedRollVelocity = Mathf.Clamp(roll / dt, -GetMaxRollVelocity(), GetMaxRollVelocity());

            _spinner.pivot.Rotate(Vector3.forward, roll, Space.Self);
            _rollVelocity = clampedRollVelocity;
            _flipVelocity = 0f;
            _yawVelocity = 0f;
            _pitchVelocity = 0f;

            if (allowVerticalFlip && Mathf.Abs(deltaY) > Mathf.Abs(deltaX) * flipBias)
            {
                float flip = deltaY * flipSpeed;
                float clampedFlipVelocity = Mathf.Clamp(flip / dt, -GetMaxFlipVelocity(), GetMaxFlipVelocity());
                _spinner.pivot.Rotate(Vector3.right, flip, Space.Self);
                _flipVelocity = clampedFlipVelocity;
            }

            return;
        }

        float yaw = (invert ? deltaX : -deltaX) * speed;
        float pitch = deltaY * speed;

        _spinner.pivot.Rotate(camTransform != null ? camTransform.up : Vector3.up, yaw, Space.World);
        _spinner.pivot.Rotate(camTransform != null ? camTransform.right : Vector3.right, pitch, Space.World);
        _rollVelocity = 0f;
        _flipVelocity = 0f;
        _yawVelocity = Mathf.Clamp(yaw / dt, -GetMaxRollVelocity(), GetMaxRollVelocity());
        _pitchVelocity = Mathf.Clamp(pitch / dt, -GetMaxFlipVelocity(), GetMaxFlipVelocity());

        Log($"FreeDrag delta=({deltaX:0.###}, {deltaY:0.###}) yaw={yaw:0.###} pitch={pitch:0.###} pivotEuler={_spinner.pivot.eulerAngles}.");
    }

    public void HandleMouseUp()
    {
        if (!_bindingsReady)
            EnsureBound();
        if (!_dragging)
            return;

        _dragging = false;

        float threshold = dragProfile != null ? dragProfile.clickThresholdPixels : clickThresholdPixels;
        if ((Input.mousePosition - _pointerDownPosition).sqrMagnitude > threshold * threshold)
            return;

        TrySelectClickedFace();
    }

    public void RefreshHighlight()
    {
        if (!_bindingsReady)
            EnsureBound();
        if (_highlightRenderer == null || _controller == null || _spinner == null)
            return;

        _highlightFaces.Clear();
        _highlightKinds.Clear();

        if (_spinner.faces != null)
        {
            for (int i = 0; i < _spinner.faces.Length; i++)
            {
                DiceEditSandboxController.SandboxFaceHighlightKind highlightKind = _controller.GetHighlightKindForFace(_spinner, i);
                if (highlightKind == DiceEditSandboxController.SandboxFaceHighlightKind.None)
                    continue;

                _highlightFaces.Add(i);
                _highlightKinds.Add(highlightKind);
            }
        }

        if (_highlightFaces.Count > 0)
        {
            _highlightRenderer.ShowFacesWithKinds(_highlightFaces, _highlightKinds);
            return;
        }

        _controller.CopySelectedFacesTo(_highlightFaces);
        if (_highlightFaces.Count > 0 && _controller.IsPreviewSelection(_spinner, _highlightFaces[0]))
        {
            _highlightRenderer.ShowFaces(_highlightFaces, committed: false);
            return;
        }

        _highlightRenderer.Clear();
    }

    public void ClearHighlight()
    {
        if (!_bindingsReady)
            EnsureBound();

        _highlightRenderer?.Clear();
    }

    public void FlipInspectOrientation()
    {
        if (!_bindingsReady)
            EnsureBound();
        if (_spinner == null || _spinner.pivot == null)
            return;

        _spinner.pivot.Rotate(_spinner.pivot.forward, 180f, Space.World);
    }

    public bool CanRollInspectDie()
    {
        if (!_bindingsReady)
            EnsureBound();

        return _spinner != null && !_spinner.IsRolling;
    }

    public void RollInspectDie()
    {
        if (!_bindingsReady)
            EnsureBound();
        if (_spinner == null || _spinner.IsRolling)
            return;

        _spinner.RollRandomFace();
    }

    public bool CanAutoUprightInspectDie()
    {
        if (!_bindingsReady)
            EnsureBound();

        return _spinner != null && _spinner.pivot != null && !_spinner.IsRolling;
    }

    public void AutoUprightInspectDie()
    {
        if (!_bindingsReady)
            EnsureBound();
        if (_spinner == null || _spinner.pivot == null)
            return;

        Camera cam = GetMainCamera();
        if (cam == null)
            return;

        int frontMostFace = _spinner.GetBestFacingFaceIndex(cam);
        if (frontMostFace < 0)
            return;

        StopInspectMotion();
        _spinner.SnapToFaceIndexAnimatedQuaternion(frontMostFace, syncRollState: false);
    }

    private void TrySelectClickedFace()
    {
        if (!_bindingsReady)
            EnsureBound();
        if (_controller == null || _spinner == null || _selectionMap == null)
        {
            Debug.Log("[DiceEditSelect] Click failed: missing controller/spinner/selectionMap.");
            return;
        }

        Camera cam = GetMainCamera();
        if (cam == null)
        {
            Debug.Log("[DiceEditSelect] Click failed: no Main Camera.");
            return;
        }

        int logicalFaceIndex = -1;
        Vector2 screenPosition = Input.mousePosition;
        if (!_selectionMap.TryGetNearestVisibleLogicalFace(screenPosition, cam, out logicalFaceIndex))
        {
            Ray ray = cam.ScreenPointToRay(screenPosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, 100f);
            if (hits == null || hits.Length == 0)
            {
                Debug.Log("[DiceEditSelect] Click failed: raycast hit nothing.");
            }

            for (int i = 0; hits != null && i < hits.Length; i++)
            {
                if (_selectionMap.TryResolveLogicalFace(hits[i], cam, out logicalFaceIndex))
                    break;
            }
        }

        if (logicalFaceIndex < 0)
        {
            Debug.Log("[DiceEditSelect] Click failed: no hit resolved to a logical face.");
            return;
        }

        _controller.SetPreviewSelection(_spinner, logicalFaceIndex);
        RefreshHighlight();
    }

    private void EnsureBound()
    {
        if (_controller == null)
            _controller = FindFirstObjectByType<DiceEditSandboxController>(FindObjectsInactive.Include);

        if (_spinner == null)
            _spinner = GetComponent<DiceSpinnerGeneric>();

        if (_spinner != null && _spinner.pivot == null)
            _spinner.pivot = _spinner.transform;

        if (_selectionMap == null)
            _selectionMap = GetComponent<DiceFaceSelectionMap>();

        if (_selectionMap == null)
            _selectionMap = gameObject.AddComponent<DiceFaceSelectionMap>();

        if (_highlightRenderer == null)
            _highlightRenderer = GetComponent<DiceFaceHighlightRenderer>();

        if (_highlightRenderer == null)
            _highlightRenderer = gameObject.AddComponent<DiceFaceHighlightRenderer>();

        if (_selectionMap != null && _spinner != null && !_selectionMap.IsConfiguredFor(_spinner))
            _selectionMap.Configure(_spinner);

        if (_highlightRenderer != null && _spinner != null && _selectionMap != null)
            _highlightRenderer.Configure(_spinner, _selectionMap);

        SubscribeRollComplete();

        if (_cachedMainCamera == null)
            _cachedMainCamera = Camera.main;

        _bindingsReady = _controller != null && _spinner != null && _selectionMap != null && _highlightRenderer != null;
    }

    private static void Log(string message)
    {
        if (DebugLogs)
            Debug.Log($"[DiceEditInteractable] {message}");
    }

    private void SubscribeRollComplete()
    {
        if (_spinner == null || _rollCompleteSubscribed)
            return;

        _spinner.onRollComplete += HandleRollComplete;
        _rollCompleteSubscribed = true;
    }

    private void HandleRollComplete(DiceSpinnerGeneric _)
    {
        _controller?.NotifyInspectDieStateChanged();
    }

    private float GetInertiaDamping()
    {
        return dragProfile != null ? dragProfile.inertiaDamping : inertiaDamping;
    }

    private float GetFlipInertiaDamping()
    {
        return dragProfile != null ? dragProfile.flipInertiaDamping : flipInertiaDamping;
    }

    private float GetMaxRollVelocity()
    {
        return dragProfile != null ? dragProfile.maxRollVelocity : maxRollVelocity;
    }

    private float GetMaxFlipVelocity()
    {
        return dragProfile != null ? dragProfile.maxFlipVelocity : maxFlipVelocity;
    }

    private void StopInspectMotion()
    {
        _dragging = false;
        _rollVelocity = 0f;
        _flipVelocity = 0f;
        _yawVelocity = 0f;
        _pitchVelocity = 0f;
    }

    private Camera GetMainCamera()
    {
        if (_cachedMainCamera == null)
            _cachedMainCamera = Camera.main;

        return _cachedMainCamera;
    }

    private Transform GetMainCameraTransform()
    {
        Camera cam = GetMainCamera();
        return cam != null ? cam.transform : null;
    }
}
