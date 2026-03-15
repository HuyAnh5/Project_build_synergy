using System;

[Serializable]
public class StatusPendingBuffDebuff
{
    public BuffDebuffEffectEntry entry;
    public int delayTurns;
    public CombatActor applier;
    public int rolledValue;
    public int maxFaceValue;
}

[Serializable]
public class StatusActiveBuffDebuff
{
    public BuffDebuffEffectEntry entry;
    public int remainingTurns;
    public CombatActor applier;
}

[Serializable]
public class StatusPendingAilment
{
    public AilmentType type;
    public int delayTurns;
    public int durationTurns;
    public float chanceMultiplier;
    public CombatActor applier;
    public int rolledValue;
    public int maxFaceValue;
}
