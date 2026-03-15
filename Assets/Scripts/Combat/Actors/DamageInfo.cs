using UnityEngine;

public enum SkillKind { Attack, Guard, Utility }
public enum TargetRule { Enemy, Self }

public enum DamageGroup { Strike, Sunder, Effect }   // Effect = skill áp effect (dmg thấp) KHÔNG consume status
public enum ElementType { Neutral, Fire, Ice, Lightning, Physical }
public enum RangeType { Melee, Ranged }

public struct DamageInfo
{
    public DamageGroup group;
    public ElementType element;
    public bool bypassGuard;          // Sunder
    public bool clearsGuard;          // Sunder
    public bool canUseMarkMultiplier; // có thể tắt cho Sunder
    public bool isDamage;             // true nếu damage > 0
}
