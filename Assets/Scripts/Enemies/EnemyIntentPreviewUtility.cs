using UnityEngine;

internal static class EnemyIntentPreviewUtility
{
    public static string BuildBasicPreview(EnemyDefinitionSO.EnemyMoveSlot move)
    {
        if ((move.tags & EnemyDefinitionSO.EnemyMoveTag.Heal) != 0)
            return "Heal";
        if ((move.tags & EnemyDefinitionSO.EnemyMoveTag.Buff) != 0)
            return "Buff";
        if ((move.tags & EnemyDefinitionSO.EnemyMoveTag.Debuff) != 0)
            return "Debuff";
        if ((move.tags & EnemyDefinitionSO.EnemyMoveTag.Ailment) != 0)
            return "Ail";

        if ((move.tags & EnemyDefinitionSO.EnemyMoveTag.Special) != 0 &&
            move.intent == EnemyDefinitionSO.EnemyIntentTag.Special)
            return "Special";

        if (move.damageSkill != null)
        {
            SkillRuntime runtime = SkillRuntime.FromDamage(move.damageSkill);
            if (runtime.kind == SkillKind.Attack)
                return $"Attack:{Mathf.Max(0, runtime.CalculateDamage(0))}";
            if (runtime.kind == SkillKind.Guard)
                return $"Guard:{Mathf.Max(0, runtime.CalculateGuard(0))}";
        }

        switch (move.intent)
        {
            case EnemyDefinitionSO.EnemyIntentTag.Attack:
                return "Attack";
            case EnemyDefinitionSO.EnemyIntentTag.Defend:
                return "Guard";
            case EnemyDefinitionSO.EnemyIntentTag.Buff:
                return "Buff";
            case EnemyDefinitionSO.EnemyIntentTag.Debuff:
                return "Debuff";
            default:
                return move.intent.ToString();
        }
    }

    public static CombatActor PickMostInjuredEnemyAlly(CombatActor self, BattlePartyManager2D party, bool includeSelf)
    {
        if (party == null)
            return null;

        var allies = party.GetAliveEnemies(frontOnly: false);
        if (allies == null || allies.Count == 0)
            return null;

        CombatActor best = null;
        float bestHpPct = 999f;
        int bestMissingHp = -1;

        for (int i = 0; i < allies.Count; i++)
        {
            CombatActor ally = allies[i];
            if (ally == null || ally.IsDead)
                continue;
            if (!includeSelf && ally == self)
                continue;

            int maxHp = Mathf.Max(1, ally.maxHP);
            if (ally.hp >= maxHp)
                continue;

            float hpPct = (float)ally.hp / maxHp;
            int missingHp = maxHp - ally.hp;
            if (best == null || hpPct < bestHpPct || (Mathf.Approximately(hpPct, bestHpPct) && missingHp > bestMissingHp))
            {
                best = ally;
                bestHpPct = hpPct;
                bestMissingHp = missingHp;
            }
        }

        return best;
    }

    public static bool IsHealIntent(EnemyBrainController.IntentData intentData)
    {
        if (!intentData.hasIntent)
            return false;

        return (intentData.moveTags & EnemyDefinitionSO.EnemyMoveTag.Heal) != 0;
    }
}
