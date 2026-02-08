using UnityEngine;

public class StatusController : MonoBehaviour
{
    // Burn: stacks + duration
    public int burnStacks;
    public int burnTurns;

    // Mark: tồn tại tới hit kế tiếp
    public bool marked;

    // Bleed: stack = số turn còn lại
    public int bleedTurns;

    // Freeze: nếu proc thì skip 1 lượt (duration 1 turn)
    public bool frozen;

    // ✅ Needed by CombatActor (reset between fights / retry)
    public void ClearAll()
    {
        burnStacks = 0;
        burnTurns = 0;
        marked = false;
        bleedTurns = 0;
        frozen = false;
    }

    public void ApplyBurn(int addStacks, int refreshTurns)
    {
        burnStacks += Mathf.Max(0, addStacks);
        burnTurns = Mathf.Max(burnTurns, refreshTurns);
    }

    public void ApplyMark() => marked = true;

    public void ApplyBleed(int turns)
    {
        if (turns <= 0) return;

        // Stack / cộng dồn:
        bleedTurns += turns;

        // (Tuỳ chọn) nếu muốn giới hạn để balance, mở comment:
        // bleedTurns = Mathf.Min(bleedTurns, 99);
    }

    public void ApplyFreeze() => frozen = true;

    // gọi ở "đầu lượt của mục tiêu" (Bleed tick + giảm duration Burn)
    public int TickStartOfTurnDamage()
    {
        int dot = 0;

        // Bleed: -1 HP mỗi đầu lượt của target
        if (bleedTurns > 0)
        {
            dot += 1;
            bleedTurns -= 1;
        }

        // Burn không DoT nhưng giảm turn; hết turn thì mất stacks
        if (burnTurns > 0) burnTurns -= 1;
        if (burnTurns <= 0) burnStacks = 0;

        return dot;
    }

    // ✅ Needed by TurnManager (skip 1 lượt rồi tự gỡ Freeze)
    public int OnTurnStarted(bool consumeFreezeToSkipTurn, out bool skipTurn)
    {
        skipTurn = false;

        int dot = TickStartOfTurnDamage();

        if (consumeFreezeToSkipTurn && frozen)
        {
            frozen = false;
            skipTurn = true; // skip đúng 1 lượt của chính target
        }

        return dot;
    }

    // ✅ NEW: Consume rules + trả focus reward cho attacker (Ice phá Freeze -> +1 Focus)
    public int OnHitByDamageReturnFocusReward(ref DamageInfo info)
    {
        // Effect skills KHÔNG consume status + KHÔNG reward
        if (info.group == DamageGroup.Effect) return 0;

        int reward = 0;

        // Mark: bị xóa bởi bất kỳ damage
        if (marked && info.isDamage)
            marked = false;

        // Burn: bị xóa khi bị Fire damage
        if (burnStacks > 0 && info.element == ElementType.Fire && info.isDamage)
        {
            burnStacks = 0;
            burnTurns = 0;
        }

        // Freeze: bị xóa khi bị Ice damage; phá freeze thưởng focus +1
        if (frozen && info.element == ElementType.Ice && info.isDamage)
        {
            frozen = false;
            reward += 1;
        }

        // Bleed: không xóa bởi hit thường
        return reward;
    }

    // Back-compat: code cũ vẫn gọi được
    public void OnHitByDamage(ref DamageInfo info)
    {
        OnHitByDamageReturnFocusReward(ref info);
    }

    // Chance dạng 0..1
    public bool TryApplyFreeze(float chance01)
    {
        if (chance01 <= 0f) return false;

        if (chance01 >= 1f)
        {
            ApplyFreeze();
            return true;
        }

        if (Random.value < chance01)
        {
            ApplyFreeze();
            return true;
        }

        return false;
    }

    // Chance dạng % 0..100
    public bool TryApplyFreeze(int chancePercent)
    {
        if (chancePercent <= 0) return false;

        if (chancePercent >= 100)
        {
            ApplyFreeze();
            return true;
        }

        if (Random.Range(0, 100) < chancePercent)
        {
            ApplyFreeze();
            return true;
        }

        return false;
    }
}
