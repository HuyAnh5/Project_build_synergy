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
    public int focus = 3;

    [Tooltip("Focus khởi đầu mỗi battle (prototype). Nếu muốn focus persist thì đừng gọi ResetForBattle hoặc đổi rule.")]
    public int startingFocus = 3;

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

    public void Heal(int amount)
    {
        if (amount <= 0) return;
        hp = Mathf.Clamp(hp + amount, 0, maxHP);
    }

    public void ResetForBattle(bool resetHp)
    {
        if (resetHp) hp = maxHP;

        focus = Mathf.Clamp(startingFocus, 0, maxFocus);
        guardPool = 0;

        if (status) status.ClearAll();
    }

    public bool IsDead => hp <= 0;
}
