using UnityEngine;

public class CombatActor : MonoBehaviour
{
    public enum TeamSide { Ally, Enemy }
    public enum RowTag { Front, Back }

    [Header("Identity")]
    public TeamSide team = TeamSide.Enemy;
    public RowTag row = RowTag.Front;

    [Tooltip("Player thường dùng HUD riêng trên Canvas, không cần world-ui.")]
    public bool isPlayer = false;

    [Header("Stats")]
    public int maxHP = 30;
    public int hp = 30;

    public int maxFocus = 9;
    public int focus = 2;

    [Tooltip("Focus khởi đầu mỗi battle (prototype). Nếu muốn focus persist thì đừng gọi ResetForBattle hoặc đổi rule.")]
    public int startingFocus = 2;

    public int guardPool = 0;

    [Header("Refs")]
    public Transform firePoint;
    public StatusController status;

    [Tooltip("Điểm gắn world-ui. Nếu để trống sẽ dùng transform của actor.")]
    public Transform uiAnchor;

    private void Awake()
    {
        if (!status) status = GetComponent<StatusController>();
        if (!uiAnchor) uiAnchor = transform;
    }

    public bool TrySpendFocus(int amount)
    {
        if (amount <= 0) return true;
        if (focus < amount) return false;
        focus -= amount;
        return true;
    }

    public void GainFocus(int amount)
    {
        focus = Mathf.Clamp(focus + amount, 0, maxFocus);
    }

    public void SetGuard(int value)
    {
        guardPool = Mathf.Max(0, value);
    }

    public void AddGuard(int amount)
    {
        if (amount == 0) return;
        guardPool = Mathf.Max(0, guardPool + amount);
    }

    public struct DamageResult
    {
        public int requested;
        public int blocked;
        public int hpLost;
        public bool guardBroken;
    }

    public DamageResult TakeDamageDetailed(int dmg, bool bypassGuard)
    {
        DamageResult r = new DamageResult { requested = Mathf.Max(0, dmg), blocked = 0, hpLost = 0, guardBroken = false };

        int remaining = r.requested;
        int guardBeforeHit = guardPool;

        if (!bypassGuard && guardPool > 0)
        {
            int blocked = Mathf.Min(guardPool, remaining);
            guardPool -= blocked;
            remaining -= blocked;
            r.blocked = blocked;
            r.guardBroken = guardBeforeHit > 0 && guardPool <= 0 && blocked > 0;
        }

        if (remaining > 0)
        {
            int before = hp;
            hp = Mathf.Max(0, hp - remaining);
            r.hpLost = before - hp;
        }

        return r;
    }

    public void TakeDamage(int dmg, bool bypassGuard)
    {
        int remaining = dmg;

        if (!bypassGuard && guardPool > 0)
        {
            int blocked = Mathf.Min(guardPool, remaining);
            guardPool -= blocked;
            remaining -= blocked;
        }

        if (remaining > 0) hp = Mathf.Max(0, hp - remaining);
    }

    public int Heal(int amount)
    {
        if (amount <= 0) return 0;

        int before = hp;
        hp = Mathf.Clamp(hp + amount, 0, maxHP);
        int healed = hp - before;
        if (healed <= 0) return 0;

        // Spawn popup xanh trên chính target được heal
        var pop = Object.FindObjectOfType<DamagePopupSystem>();
        if (pop != null)
        {
            pop.SpawnHeal(this, this, healed);
        }

        return healed;
    }

    public void ResetForBattle(bool resetHp)
    {
        if (resetHp) hp = maxHP;

        int battleStartFocus = isPlayer ? 2 : startingFocus;
        focus = Mathf.Clamp(battleStartFocus, 0, maxFocus);
        guardPool = 0;

        if (status) status.ClearAll();
        PassiveSystem passiveSystem = GetComponent<PassiveSystem>();
        if (passiveSystem != null) passiveSystem.OnCombatStarted();
        SkillCombatState skillCombatState = GetComponent<SkillCombatState>();
        if (skillCombatState != null) skillCombatState.ResetForBattle();
    }

    public bool IsDead => hp <= 0;
}
