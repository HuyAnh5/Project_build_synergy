using UnityEngine;
using UnityEngine.EventSystems;

public class TargetClickable2D : MonoBehaviour, IPointerClickHandler
{
    public TurnManager turn;
    private CombatActor _actor;

    void Awake()
    {
        _actor = GetComponent<CombatActor>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!turn || !_actor) return;
        turn.OnTargetClicked(_actor);
    }
}
