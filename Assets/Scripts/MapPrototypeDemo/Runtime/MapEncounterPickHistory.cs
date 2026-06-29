using System.Collections.Generic;

public sealed class MapEncounterPickHistory
{
    private const int CombatCooldownPickCount = 2;

    private readonly Queue<string> _recentCombatIds = new Queue<string>(CombatCooldownPickCount);
    private readonly HashSet<string> _recentCombatLookup = new HashSet<string>();
    private readonly HashSet<string> _usedEventIds = new HashSet<string>();
    private readonly HashSet<string> _usedEliteIds = new HashSet<string>();
    private readonly HashSet<string> _usedBossIds = new HashSet<string>();

    public bool IsOnCooldown(MapEncounterDefinitionSO encounter)
    {
        return encounter != null
            && encounter.Kind == MapEncounterKind.Combat
            && _recentCombatLookup.Contains(encounter.EncounterId);
    }

    public bool IsUsedOncePerAct(MapEncounterDefinitionSO encounter)
    {
        if (encounter == null)
            return false;

        switch (encounter.Kind)
        {
            case MapEncounterKind.Event:
                return _usedEventIds.Contains(encounter.EncounterId);
            case MapEncounterKind.Elite:
                return _usedEliteIds.Contains(encounter.EncounterId);
            case MapEncounterKind.Boss:
                return _usedBossIds.Contains(encounter.EncounterId);
            default:
                return false;
        }
    }

    public bool CanPick(MapEncounterDefinitionSO encounter, bool enforceCombatCooldown, bool enforceOncePerAct)
    {
        if (encounter == null)
            return false;
        if (enforceCombatCooldown && IsOnCooldown(encounter))
            return false;
        if (enforceOncePerAct && IsUsedOncePerAct(encounter))
            return false;

        return true;
    }

    public void Record(MapEncounterDefinitionSO encounter)
    {
        if (encounter == null)
            return;

        switch (encounter.Kind)
        {
            case MapEncounterKind.Combat:
                RecordRecentCombat(encounter.EncounterId);
                break;
            case MapEncounterKind.Event:
                _usedEventIds.Add(encounter.EncounterId);
                break;
            case MapEncounterKind.Elite:
                _usedEliteIds.Add(encounter.EncounterId);
                break;
            case MapEncounterKind.Boss:
                _usedBossIds.Add(encounter.EncounterId);
                break;
        }
    }

    public void Clear()
    {
        _recentCombatIds.Clear();
        _recentCombatLookup.Clear();
        _usedEventIds.Clear();
        _usedEliteIds.Clear();
        _usedBossIds.Clear();
    }

    private void RecordRecentCombat(string encounterId)
    {
        if (string.IsNullOrWhiteSpace(encounterId))
            return;

        _recentCombatIds.Enqueue(encounterId);
        _recentCombatLookup.Add(encounterId);

        while (_recentCombatIds.Count > CombatCooldownPickCount)
        {
            string removed = _recentCombatIds.Dequeue();
            if (!_recentCombatIds.Contains(removed))
                _recentCombatLookup.Remove(removed);
        }
    }
}
