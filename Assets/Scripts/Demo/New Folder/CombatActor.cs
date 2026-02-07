using UnityEngine;

public class CombatActor : MonoBehaviour
{
    public int maxHP = 30;
    public int hp = 30;

    public int maxFocus = 9;
    public int focus = 3;

    public int guardPool = 0;

    public Transform firePoint;
    public StatusController status;

    private void Awake()
    {
        if (!status) status = GetComponent<StatusController>();
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

    public bool IsDead => hp <= 0;
}
