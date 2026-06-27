internal static class BuffDebuffFlowTargetResolver
{
    public static CombatActor Resolve(BuffDebuffFlowTarget targetMode, CombatActor caster, CombatActor selectedTarget)
    {
        switch (targetMode)
        {
            case BuffDebuffFlowTarget.SelectedTarget:
                return selectedTarget != null ? selectedTarget : caster;
            case BuffDebuffFlowTarget.Self:
            case BuffDebuffFlowTarget.UsedDice:
            default:
                return caster;
        }
    }
}
