using System.Collections.Generic;
using UnityEngine;

public static class TesterPassiveToggleState
{
    // Set false when you want new builds to ship without tester-only passive toggles.
    public const bool PassiveToggleEnabled = true;

    private static readonly HashSet<SkillPassiveSO> EnabledPassives = new HashSet<SkillPassiveSO>();

    public static bool IsFeatureEnabled => PassiveToggleEnabled;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSessionState()
    {
        EnabledPassives.Clear();
    }

    public static bool IsControlledPassive(SkillPassiveSO passive)
    {
        return IsFeatureEnabled && passive != null;
    }

    public static bool IsEnabled(SkillPassiveSO passive)
    {
        if (passive == null)
            return false;

        if (!IsFeatureEnabled)
            return true;

        return EnabledPassives.Contains(passive);
    }

    public static bool Toggle(SkillPassiveSO passive)
    {
        if (!IsControlledPassive(passive))
            return false;

        if (EnabledPassives.Contains(passive))
        {
            EnabledPassives.Remove(passive);
            return false;
        }

        EnabledPassives.Add(passive);
        return true;
    }
}
