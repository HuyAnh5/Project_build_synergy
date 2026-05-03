using System;

public sealed partial class MapPrototypeController
{
    public event Action<MapPrototypeController> MapStateChanged;
    public event Action<MapPrototypeNodeData> HostileNodeOpened;
    public event Action<MapPrototypeNodeData, RunCombatResult> HostileNodeResolved;

    public MapPrototypeData CurrentMap => _map;
    public string CurrentNodeId => _currentNodeId;
    public int BossHintsCollected => _hintsCollected;
    public bool BossRevealed => _bossRevealed;

    public void WriteStateTo(RunState state)
    {
        if (state == null)
            return;

        state.SetMapSnapshot(_map, _currentNodeId, _hintsCollected, _bossRevealed);
    }

    [Obsolete("Use WriteStateTo instead.")]
    public void WriteProgressTo(RunState state)
    {
        WriteStateTo(state);
    }

    private void NotifyMapStateChanged()
    {
        MapStateChanged?.Invoke(this);
    }

    private void NotifyHostileNodeOpened(MapPrototypeNodeData node)
    {
        HostileNodeOpened?.Invoke(node);
    }

    private void NotifyHostileNodeResolved(MapPrototypeNodeData node, RunCombatResult result)
    {
        HostileNodeResolved?.Invoke(node, result);
    }
}
