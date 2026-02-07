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

    public void ApplyBurn(int addStacks, int refreshTurns)
    {
        burnStacks += Mathf.Max(0, addStacks);
        burnTurns = Mathf.Max(burnTurns, refreshTurns);
    }

    public void ApplyMark()
    {
        marked = true;
    }

    public void ApplyBleed(int turns)
    {
        bleedTurns = Mathf.Max(bleedTurns, turns);
    }

    public void ApplyFreeze()
    {
        frozen = true; // duration 1 turn (đến khi enemy mất lượt xong)
    }

    // gọi ở "đầu lượt của mục tiêu" (Bleed tick)
    public int TickStartOfTurnDamage()
    {
        int dot = 0;
        if (bleedTurns > 0)
        {
            dot += 1;
            bleedTurns -= 1;
        }

        // Burn không DoT
        if (burnTurns > 0) burnTurns -= 1;
        if (burnTurns <= 0) burnStacks = 0;

        return dot;
    }

    // CORE: consume rules khi mục tiêu bị dính đòn gây sát thương thuộc Strike/Sunder/(sau này relic)
    public void OnHitByDamage(ref DamageInfo info)
    {
        // Effect skills KHÔNG consume status
        if (info.group == DamageGroup.Effect) return;

        // Mark: bị xóa bởi BẤT KỲ damage Strike/Sunder
        if (marked && info.isDamage)
            marked = false;

        // Burn: bị xóa khi bị Fire damage (Strike/Sunder)
        if (burnStacks > 0 && info.element == ElementType.Fire && info.isDamage)
        {
            burnStacks = 0;
            burnTurns = 0;
        }

        // Freeze: bị xóa khi bị Ice damage
        if (frozen && info.element == ElementType.Ice && info.isDamage)
            frozen = false;

        // Bleed: không xóa bởi Strike/Sunder thường (chỉ hết turn hoặc relic consume)
    }

    // Chance dạng 0..1 (đúng với freezeChance hiện tại của bạn)
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

    // Chance dạng % 0..100 (phòng khi code khác gọi kiểu int)
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
