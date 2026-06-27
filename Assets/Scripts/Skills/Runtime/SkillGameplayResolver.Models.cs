using System.Collections.Generic;

/// <summary>
/// Bundles the data needed to resolve one authored skill action.
/// </summary>
public sealed class SkillResolveContext
{
    public SkillDamageSO skill;
    public SkillGameplayData gameplay;
    public SkillRuntime runtime;
    public CombatActor caster;
    public CombatActor target;
    public SkillConditionContext conditionContext;
    public int totalAddedValue;
    public int consumedBurnStacks;
    public int consumedBleedStacks;
}

/// <summary>
/// Represents one concrete resolved gameplay effect after values and targets are finalized.
/// </summary>
public sealed class ResolvedEffect
{
    public SkillEffectType type;
    public SkillEffectTarget target;
    public CombatActor targetActor;
    public StatusKind status;
    public int value;
    public bool isBlueValue;
    public bool previewable;
    public bool sameActionFollowUp;
    public SkillEffectData source;
}

/// <summary>
/// Tracks net status changes for tooltip and preview summaries.
/// </summary>
public struct StatusDelta
{
    public SkillEffectTarget target;
    public StatusKind status;
    public int amount;
}

/// <summary>
/// Collects the full result of resolving one skill, including failure state and summarized deltas.
/// </summary>
public sealed class SkillResolvedResult
{
    public bool canCast = true;
    public string failureReason = string.Empty;
    public int resolvedAPCost;
    public int resolvedDiceCost;
    public int executionCount = 1;
    public readonly List<ResolvedEffect> effects = new List<ResolvedEffect>();
    public int damageDelta;
    public int guardDelta;
    public int healDelta;
    public readonly List<StatusDelta> statusDeltas = new List<StatusDelta>();
}
