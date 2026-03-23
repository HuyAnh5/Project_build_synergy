using System;
using UnityEngine;

public enum ContentRarity
{
    Pending,
    Common,
    Uncommon,
    Rare
}

public enum SpecLockState
{
    Pool,
    RevisedLocked,
    Locked
}

public enum BuildRole
{
    Core,
    Support,
    Bridge,
    Utility,
    Setup,
    Payoff,
    Engine,
    Anchor,
    Conversion,
    Entry,
    Finisher,
    Tech
}

public enum ImplementationCoverage
{
    MetadataOnly,
    PartialRuntime,
    RuntimeReady
}

[Serializable]
public class SkillSpecMetadata
{
    public ContentRarity rarity = ContentRarity.Pending;
    public SpecLockState specState = SpecLockState.Pool;
    public BuildRole buildRole = BuildRole.Support;
    public ImplementationCoverage implementationCoverage = ImplementationCoverage.MetadataOnly;

    [TextArea(2, 4)]
    public string normalizedText;

    [TextArea(2, 4)]
    public string designIntent;
}
