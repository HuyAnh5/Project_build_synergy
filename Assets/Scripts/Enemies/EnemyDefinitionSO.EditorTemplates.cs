using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public partial class EnemyDefinitionSO
{
#if UNITY_EDITOR
    [BoxGroup("Quick Add (Templates)")]
    [Button(ButtonSizes.Large)]
    [GUIColor(0.35f, 0.85f, 0.45f)]
    [LabelText("Quick Fill: By Role (Clear & Fill 5-6 Moves)")]
    private void QuickFillByRole()
    {
        moves ??= new List<EnemyMoveSlot>();
        moves.Clear();

        switch (role)
        {
            case EnemyRoleTag.Bruiser:
                FillBruiserTemplate();
                break;
            case EnemyRoleTag.Controller:
                FillControllerTemplate();
                break;
            case EnemyRoleTag.Assassin:
                FillAssassinTemplate();
                break;
            case EnemyRoleTag.Support:
                FillSupportTemplate();
                break;
        }
    }

    [BoxGroup("Quick Add (Templates)")]
    [Button(ButtonSizes.Medium)]
    [LabelText("Quick Append: By Role (Add 5-6 Moves)")]
    private void QuickAppendByRole()
    {
        moves ??= new List<EnemyMoveSlot>();

        switch (role)
        {
            case EnemyRoleTag.Bruiser:
                AppendBruiserTemplate();
                break;
            case EnemyRoleTag.Controller:
                AppendControllerTemplate();
                break;
            case EnemyRoleTag.Assassin:
                AppendAssassinTemplate();
                break;
            case EnemyRoleTag.Support:
                AppendSupportTemplate();
                break;
        }

        if (moves.Count > 6)
        {
            moves.RemoveRange(6, moves.Count - 6);
        }
    }

    /// <summary>
    /// Builds a lightweight move template entry so designers can quickly scaffold role patterns.
    /// </summary>
    private EnemyMoveSlot M(
        string id,
        string name,
        EnemyIntentTag intent,
        EnemyMoveTag tags,
        int weight = 10,
        int cd = 0,
        int maxCon = 2,
        float hpMin = 0f,
        float hpMax = 1f)
    {
        return new EnemyMoveSlot
        {
            moveId = id,
            displayName = name,
            intent = intent,
            tags = tags,
            weight = weight,
            cooldownTurns = cd,
            maxConsecutive = Mathf.Max(1, maxCon),
            hpPctMin = Mathf.Clamp01(hpMin),
            hpPctMax = Mathf.Clamp01(hpMax),
            damageSkill = null,
            buffDebuffSkill = null
        };
    }

    private void FillBruiserTemplate()
    {
        moves.Add(M("bru_basic", "Basic Strike", EnemyIntentTag.Attack, EnemyMoveTag.Attack, weight: 12, cd: 0, maxCon: 3));
        moves.Add(M("bru_heavy", "Heavy Smash", EnemyIntentTag.Attack, EnemyMoveTag.Attack | EnemyMoveTag.Heavy, weight: 10, cd: 2, maxCon: 1));
        moves.Add(M("bru_crush", "Bruiser Crush", EnemyIntentTag.Special, EnemyMoveTag.Attack | EnemyMoveTag.Special, weight: 8, cd: 2, maxCon: 1));
        moves.Add(M("bru_guard", "Guard Up", EnemyIntentTag.Defend, EnemyMoveTag.Guard, weight: 10, cd: 1, maxCon: 1));
        moves.Add(M("bru_sunder", "Sunder Break", EnemyIntentTag.Attack, EnemyMoveTag.Attack | EnemyMoveTag.GuardPunish, weight: 9, cd: 2, maxCon: 1));
        moves.Add(M("bru_press", "Pressure Jab", EnemyIntentTag.Attack, EnemyMoveTag.Attack, weight: 11, cd: 0, maxCon: 3));
    }

    private void AppendBruiserTemplate()
    {
        FillBruiserTemplate();
    }

    private void FillControllerTemplate()
    {
        moves.Add(M("ctl_poke", "Small Strike", EnemyIntentTag.Attack, EnemyMoveTag.Attack, weight: 12, cd: 0, maxCon: 3));
        moves.Add(M("ctl_ail", "Apply Ailment", EnemyIntentTag.Debuff, EnemyMoveTag.Debuff | EnemyMoveTag.Ailment, weight: 10, cd: 2, maxCon: 1));
        moves.Add(M("ctl_debuff", "Apply Debuff", EnemyIntentTag.Debuff, EnemyMoveTag.Debuff, weight: 11, cd: 1, maxCon: 1));
        moves.Add(M("ctl_collapse", "Slot Collapse", EnemyIntentTag.Debuff, EnemyMoveTag.Debuff | EnemyMoveTag.Special, weight: 9, cd: 2, maxCon: 1));
        moves.Add(M("ctl_mark", "Mark Target", EnemyIntentTag.Debuff, EnemyMoveTag.Debuff, weight: 8, cd: 2, maxCon: 1));
        moves.Add(M("ctl_guard", "Guard (light)", EnemyIntentTag.Defend, EnemyMoveTag.Guard, weight: 6, cd: 2, maxCon: 1));
    }

    private void AppendControllerTemplate()
    {
        FillControllerTemplate();
    }

    private void FillAssassinTemplate()
    {
        moves.Add(M("asn_burst", "Burst Strike", EnemyIntentTag.Attack, EnemyMoveTag.Attack | EnemyMoveTag.Heavy, weight: 12, cd: 2, maxCon: 1));
        moves.Add(M("asn_punish", "Guard Punish", EnemyIntentTag.Attack, EnemyMoveTag.Attack | EnemyMoveTag.GuardPunish, weight: 10, cd: 2, maxCon: 1));
        moves.Add(M("asn_stab", "Quick Stab", EnemyIntentTag.Attack, EnemyMoveTag.Attack, weight: 11, cd: 0, maxCon: 3));
        moves.Add(M("asn_evade", "Evade / Guard", EnemyIntentTag.Defend, EnemyMoveTag.Buff | EnemyMoveTag.Guard, weight: 8, cd: 2, maxCon: 1, hpMin: 0f, hpMax: 0.6f));
        moves.Add(M("asn_setup", "Setup", EnemyIntentTag.Special, EnemyMoveTag.Setup | EnemyMoveTag.Special, weight: 7, cd: 3, maxCon: 1));
        moves.Add(M("asn_finish", "Finisher", EnemyIntentTag.Attack, EnemyMoveTag.Attack | EnemyMoveTag.Finisher, weight: 6, cd: 3, maxCon: 1, hpMin: 0f, hpMax: 0.5f));
    }

    private void AppendAssassinTemplate()
    {
        FillAssassinTemplate();
    }

    private void FillSupportTemplate()
    {
        moves.Add(M("sup_light", "Light Strike", EnemyIntentTag.Attack, EnemyMoveTag.Attack, weight: 12, cd: 0, maxCon: 3));
        moves.Add(M("sup_heal", "Heal Ally", EnemyIntentTag.Buff, EnemyMoveTag.Heal | EnemyMoveTag.Buff, weight: 10, cd: 2, maxCon: 1));
        moves.Add(M("sup_buff", "Buff Ally", EnemyIntentTag.Buff, EnemyMoveTag.Buff, weight: 10, cd: 2, maxCon: 1));
        moves.Add(M("sup_guard", "Guard Ally", EnemyIntentTag.Defend, EnemyMoveTag.Guard | EnemyMoveTag.Buff, weight: 9, cd: 2, maxCon: 1));
        moves.Add(M("sup_summon", "Summon", EnemyIntentTag.Summon, EnemyMoveTag.Summon, weight: 8, cd: 3, maxCon: 1));
        moves.Add(M("sup_cleanse", "Cleanse", EnemyIntentTag.Buff, EnemyMoveTag.Buff | EnemyMoveTag.Special, weight: 6, cd: 3, maxCon: 1));
    }

    private void AppendSupportTemplate()
    {
        FillSupportTemplate();
    }
#endif
}
