using UnityEngine;
using UnityEngine.EventSystems;

public class TargetClickable2D : MonoBehaviour, IPointerClickHandler, IDropHandler
{
    public TurnManager turn;
    private CombatActor _actor;

    void Awake()
    {
        _actor = GetComponent<CombatActor>();
        if (turn == null)
            turn = FindObjectOfType<TurnManager>(true);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!turn || !_actor) return;
        turn.OnTargetClicked(_actor);
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (!turn) return;
        if (eventData == null || eventData.pointerDrag == null) return;

        DraggableSkillIcon drag = eventData.pointerDrag.GetComponent<DraggableSkillIcon>();
        if (drag == null) return;

        ScriptableObject asset = drag.GetSkillAsset();
        if (asset == null) return;

        bool accepted = false;
        if (_actor != null)
            accepted = turn.TryCastDraggedSkillToTarget(asset, _actor);

        if (accepted)
            drag.NotifyDropAccepted();
    }
}
