using UnityEngine;

internal struct TurnManagerQueuedPlayerCommand
{
    public ScriptableObject asset;
    public SkillRuntime runtime;
    public CombatActor target;
    public int resolvedSum;
    public int maxFace;
    public int start0;
    public int span;
}
