using UnityEngine;

public sealed class CombatActorRuntimeContext
{
    private CombatActor _actor;
    private PassiveSystem _passiveSystem;
    private SkillCombatState _skillCombatState;

    public CombatActor Actor => _actor;
    public StatusController Status => _actor != null ? _actor.status : null;
    public PassiveSystem PassiveSystem => _passiveSystem;
    public SkillCombatState SkillCombatState => _skillCombatState;

    public void Bind(CombatActor actor)
    {
        if (_actor == actor)
            return;

        _actor = actor;
        _passiveSystem = actor != null ? actor.GetComponent<PassiveSystem>() : null;
        _skillCombatState = actor != null ? actor.GetComponent<SkillCombatState>() : null;
    }

    public void HandleBasicStrikeUse(DiceSlotRig diceRig, int startIndex)
    {
        if (_passiveSystem != null)
            _passiveSystem.TryHandleBasicStrikeUse(diceRig, startIndex);
    }

    public bool ShouldRetainGuardAtEndOfTurn()
    {
        return _passiveSystem != null && _passiveSystem.ShouldRetainGuardAtEndOfTurn();
    }
}
