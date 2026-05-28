using UnityEngine;

internal enum TurnManagerQueuedPlayerCommandKind
{
    Skill,
    EndTurn
}

internal struct TurnManagerQueuedPlayerCommand
{
    public TurnManagerQueuedPlayerCommandKind kind;
    public ScriptableObject asset;
    public SkillRuntime runtime;
    public CombatActor target;
    public int resolvedSum;
    public int maxFace;
    public int start0;
    public int span;
}
