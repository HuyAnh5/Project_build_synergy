public partial class PassiveSystem
{
    partial void OverrideTesterPassiveRuntimeEnabled(SkillPassiveSO passive, ref bool enabled)
    {
        if (!TesterPassiveToggleState.IsControlledPassive(passive))
            return;

        enabled = TesterPassiveToggleState.IsEnabled(passive);
    }
}
