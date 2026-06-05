using UnityEngine;

// Handles scene pointer input for selecting, rotating, and dragging dice in the combat edit flow.
public partial class GameplayDiceEditController
{
    // Routes mouse input either to the scene dice selection mode or the modal inspect dice.
    private void Update()
    {
        AutoResolveReferences();
        if (!CanReceiveSceneInput())
            return;

        Camera cam = Camera.main;
        if (cam == null)
            return;

        bool pointerOverUi = UnityEngine.EventSystems.EventSystem.current != null &&
                             UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

        if (Input.GetMouseButtonDown(0))
        {
            if (IsPanelOpen)
                CapturePointerForOpenPanel(cam);
            else
                CapturePointerForSceneDie(cam, pointerOverUi);

            if (_activeDragInteractable != null)
                _activeDragInteractable.HandleMouseDown();
        }

        if (_activeDragInteractable != null)
            _activeDragInteractable.HandleMouseDrag();

        if (Input.GetMouseButtonUp(0) && _activeDragInteractable != null)
        {
            _activeDragInteractable.HandleMouseUp();
            _activeDragInteractable = null;
        }

        RefreshFaceEnchantTooltip(cam, pointerOverUi);
    }

    // Only the inspect clone remains interactive while the edit panel is modal.
    public bool CanManipulateInteractable(GameplayDiceEditInteractable interactable)
    {
        return IsPanelOpen && interactable != null && interactable == _activeInteractable;
    }

    // Called by interactables before consuming pointer events.
    public bool CanReceivePointer(GameplayDiceEditInteractable interactable)
    {
        if (interactable == null)
            return false;

        if (!IsPanelOpen)
            return true;

        return CanManipulateInteractable(interactable);
    }

    // Combat dice editing is available in Planning, plus while the modal edit panel is already open.
    private bool CanReceiveSceneInput()
    {
        if (turnManager == null)
            return true;

        return turnManager.phase == TurnManager.Phase.Planning || IsPanelOpen;
    }

    // Chooses the active drag target when the modal panel is open.
    private void CapturePointerForOpenPanel(Camera cam)
    {
        bool overPanelUi = panelUi != null && panelUi.IsPointerBlockedByModal(Input.mousePosition);
        GameplayDiceEditInteractable clickedInspectDie = RaycastInteractable(cam);

        if (clickedInspectDie != null && clickedInspectDie == _activeInteractable)
        {
            _activeDragInteractable = clickedInspectDie;
            Log($"MouseDown routed to active inspect die '{_activeDragInteractable.name}'.");
        }
        else if (overPanelUi)
        {
            _activeDragInteractable = null;
            Log("MouseDown consumed by modal edit UI.");
        }
        else
        {
            _activeDragInteractable = null;
            Log("MouseDown ignored because edit mode is modal and pointer was outside inspect die/panel.");
        }
    }

    // Chooses the active drag target when the player is selecting a scene dice.
    private void CapturePointerForSceneDie(Camera cam, bool pointerOverUi)
    {
        _activeDragInteractable = RaycastInteractable(cam);
        if (_activeDragInteractable == null && pointerOverUi)
        {
            Log("MouseDown ignored because pointer is over UI and no dice was hit.");
            return;
        }

        Log(_activeDragInteractable != null
            ? $"MouseDown hit dice '{_activeDragInteractable.name}'."
            : "MouseDown did not hit any GameplayDiceEditInteractable.");
    }

    // Finds the dice interactable under the current mouse position.
    private static GameplayDiceEditInteractable RaycastInteractable(Camera cam)
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 100f))
            return null;

        if (hit.collider == null)
            return null;

        return hit.collider.GetComponentInParent<GameplayDiceEditInteractable>();
    }
}
