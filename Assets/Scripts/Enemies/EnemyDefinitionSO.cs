// EnemyDefinitionSO.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

[CreateAssetMenu(menuName = "GameData/Enemies/Enemy Definition", fileName = "EnemyDef_")]
public class EnemyDefinitionSO : ScriptableObject
{
    // -------------------- Tags --------------------

    public enum EnemyRoleTag
    {
        Bruiser,
        Controller,
        Assassin,
        Support
    }

    [Flags]
    public enum EnemyMoveTag
    {
        None = 0,
        Attack = 1 << 0,
        Heavy = 1 << 1,
        Guard = 1 << 2,
        GuardPunish = 1 << 3,
        Debuff = 1 << 4,
        Ailment = 1 << 5,
        Heal = 1 << 6,
        Buff = 1 << 7,
        Summon = 1 << 8,
        Setup = 1 << 9,
        Finisher = 1 << 10,
        Special = 1 << 11,
    }

    public enum EnemyIntentTag
    {
        Attack,
        Defend,
        Buff,
        Debuff,
        Summon,
        Special
    }

    // -------------------- Identity --------------------

    [BoxGroup("Identity")]
    [LabelText("Enemy ID")]
    [InfoBox("EnemyId dùng để spawn theo id + tag. Bắt buộc unique trong EnemyDatabase/Registry.", InfoMessageType.Info)]
    [ValidateInput(nameof(ValidateEnemyId), "EnemyId không được rỗng.")]
    public string enemyId = "enemy_";

    [BoxGroup("Identity")]
    public string displayName = "Enemy";

    [BoxGroup("Identity")]
    public EnemyRoleTag role = EnemyRoleTag.Bruiser;

    [BoxGroup("Identity")]
    [Tooltip("Tags phụ nếu bạn muốn filter pool theo thuộc tính khác (Flying/Undead/Beast...).")]
    public List<string> extraTags = new List<string>();

    private bool ValidateEnemyId(string id) => !string.IsNullOrWhiteSpace(id);

    // -------------------- Stats --------------------

    [BoxGroup("Stats")]
    [MinValue(1)]
    public int maxHP = 30;

    [BoxGroup("Stats")]
    [MinValue(0)]
    public int startingGuard = 0;

    [BoxGroup("Stats")]
    [Tooltip("Bạn có thể dùng scalar này sau cho balance (chưa cần dùng ngay).")]
    [MinValue(0f)]
    public float damageScalar = 1f;

    // -------------------- AI Knobs --------------------

    [BoxGroup("AI"), LabelText("Burst Cap %")]
    [Range(0.5f, 1.0f)]
    [Tooltip("STS-style safety: selector nên tránh chọn move gây burst vượt % HP player. (Selector sẽ dùng sau).")]
    public float burstCapPercent = 0.80f;

    [BoxGroup("AI"), LabelText("No Repeat Twice")]
    [Tooltip("Global anti-spam: không chọn cùng 1 move 2 lần liên tiếp (nếu possible).")]
    public bool noRepeatTwice = true;

    [BoxGroup("AI"), LabelText("History Window")]
    [MinValue(1)]
    [Tooltip("Số turn gần nhất để anti-spam nâng cao (nếu bạn muốn dùng).")]
    public int historyWindow = 2;

    // -------------------- Moves --------------------

    [Serializable]
    public class EnemyMoveSlot
    {
        [HorizontalGroup("A", Width = 0.35f)]
        [LabelText("Move ID")]
        [ValidateInput(nameof(ValidateMoveId), "MoveId không được rỗng.")]
        public string moveId = "move_";

        [HorizontalGroup("A")]
        [LabelText("Name")]
        public string displayName = "Move";

        [Space(6)]
        [LabelText("Intent UI")]
        public EnemyIntentTag intent = EnemyIntentTag.Attack;

        [LabelText("Move Tags")]
        public EnemyMoveTag tags = EnemyMoveTag.Attack;

        [Space(6)]
        [LabelText("Weight")]
        [MinValue(0)]
        [Tooltip("Weighted selection (STS-ish). 0 = never pick (trừ khi pattern).")]
        public int weight = 10;

        [LabelText("Cooldown Turns")]
        [MinValue(0)]
        public int cooldownTurns = 0;

        [LabelText("Max Consecutive")]
        [MinValue(1)]
        [Tooltip("Anti-spam: tối đa bao nhiêu lần liên tiếp có thể dùng move này.")]
        public int maxConsecutive = 2;

        [Space(6)]
        [LabelText("HP% Min (Self)")]
        [Range(0f, 1f)]
        [Tooltip("Gate theo HP% của chính enemy.")]
        public float hpPctMin = 0f;

        [LabelText("HP% Max (Self)")]
        [Range(0f, 1f)]
        public float hpPctMax = 1f;

        [LabelText("Ignore No Repeat Rule")]
        [Tooltip("Nếu bật, move này có thể lặp 2 turn liên tiếp dù NoRepeatTwice đang bật.")]
        public bool ignoreNoRepeat = false;

        [BoxGroup("Turn Rules"), LabelText("Min Turn (>=)")]
        [MinValue(1)]
        [Tooltip("Move chỉ được phép dùng từ turn này trở đi. Ví dụ 2 = không dùng được ở turn 1.")]
        public int minTurnIndex = 1;

        [BoxGroup("Turn Rules"), LabelText("Force On Turn (0=off)")]
        [MinValue(0)]
        [Tooltip("Nếu = N (>0) thì move này bắt buộc dùng ở turn N (nếu hợp lệ).")]
        public int forceOnTurn = 0;

        [Space(8)]
        [Title("Skills (kéo skill của bạn vào đây)", TitleAlignment = TitleAlignments.Left)]
        [InfoBox("Chỉ cần kéo SkillDamageSO hoặc SkillBuffDebuffSO. (Bạn tự set nội dung skill).", InfoMessageType.Info)]
        [InlineEditor(InlineEditorObjectFieldModes.Boxed)]
        public SkillDamageSO damageSkill;

        [InlineEditor(InlineEditorObjectFieldModes.Boxed)]
        public SkillBuffDebuffSO buffDebuffSkill;

        public bool HasAnySkill => damageSkill != null || buffDebuffSkill != null;

        private bool ValidateMoveId(string id) => !string.IsNullOrWhiteSpace(id);
    }

    // 1) Đổi info box + validate message
    [BoxGroup("Moves")]
    [InfoBox("Template mặc định là 6 move. Bạn có thể xóa bớt để còn 3–4 move.", InfoMessageType.Info)]
    [ListDrawerSettings(Expanded = true, DraggableItems = true, ShowIndexLabels = true, NumberOfItemsPerPage = 10)]
    public List<EnemyMoveSlot> moves = new List<EnemyMoveSlot>();

    // 2) Đổi validate thành == 6
    private bool ValidateMovesCount(List<EnemyMoveSlot> list) => list != null && list.Count == 6;

#if UNITY_EDITOR
    // -------------------- Quick Add Templates --------------------

    [BoxGroup("Quick Add (Templates)")]
    [Button(ButtonSizes.Large)]
    [GUIColor(0.35f, 0.85f, 0.45f)]
    [LabelText("Quick Fill: By Role (Clear & Fill 5–6 Moves)")]
    private void QuickFillByRole()
    {
        moves ??= new List<EnemyMoveSlot>();
        moves.Clear();

        switch (role)
        {
            case EnemyRoleTag.Bruiser: FillBruiserTemplate(); break;
            case EnemyRoleTag.Controller: FillControllerTemplate(); break;
            case EnemyRoleTag.Assassin: FillAssassinTemplate(); break;
            case EnemyRoleTag.Support: FillSupportTemplate(); break;
        }
    }

    [BoxGroup("Quick Add (Templates)")]
    [Button(ButtonSizes.Medium)]
    [LabelText("Quick Append: By Role (Add 5–6 Moves)")]
    private void QuickAppendByRole()
    {
        moves ??= new List<EnemyMoveSlot>();

        switch (role)
        {
            case EnemyRoleTag.Bruiser: AppendBruiserTemplate(); break;
            case EnemyRoleTag.Controller: AppendControllerTemplate(); break;
            case EnemyRoleTag.Assassin: AppendAssassinTemplate(); break;
            case EnemyRoleTag.Support: AppendSupportTemplate(); break;
        }

        // đảm bảo đúng 6
        if (moves.Count > 6) moves.RemoveRange(6, moves.Count - 6);
    }

    private EnemyMoveSlot M(string id, string name, EnemyIntentTag intent, EnemyMoveTag tags,
        int weight = 10, int cd = 0, int maxCon = 2, float hpMin = 0f, float hpMax = 1f)
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

    // --- BRUISER: thay "Rage" bằng 1 "Bruiser move" (Special/Crush) ---
    private void FillBruiserTemplate()
    {
        // 6 moves: bạn sẽ chọn lại còn 3–4
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

    // --- CONTROLLER ---
    private void FillControllerTemplate()
    {
        moves.Add(M("ctl_poke", "Small Strike", EnemyIntentTag.Attack, EnemyMoveTag.Attack, weight: 12, cd: 0, maxCon: 3));
        moves.Add(M("ctl_ail", "Apply Ailment", EnemyIntentTag.Debuff, EnemyMoveTag.Debuff | EnemyMoveTag.Ailment, weight: 10, cd: 2, maxCon: 1));
        moves.Add(M("ctl_debuff", "Apply Debuff", EnemyIntentTag.Debuff, EnemyMoveTag.Debuff, weight: 11, cd: 1, maxCon: 1));
        moves.Add(M("ctl_collapse", "Slot Collapse", EnemyIntentTag.Debuff, EnemyMoveTag.Debuff | EnemyMoveTag.Special, weight: 9, cd: 2, maxCon: 1));
        moves.Add(M("ctl_mark", "Mark Target", EnemyIntentTag.Debuff, EnemyMoveTag.Debuff, weight: 8, cd: 2, maxCon: 1));
        moves.Add(M("ctl_guard", "Guard (light)", EnemyIntentTag.Defend, EnemyMoveTag.Guard, weight: 6, cd: 2, maxCon: 1));
    }

    private void AppendControllerTemplate() => FillControllerTemplate();

    // --- ASSASSIN ---
    private void FillAssassinTemplate()
    {
        moves.Add(M("asn_burst", "Burst Strike", EnemyIntentTag.Attack, EnemyMoveTag.Attack | EnemyMoveTag.Heavy, weight: 12, cd: 2, maxCon: 1));
        moves.Add(M("asn_punish", "Guard Punish", EnemyIntentTag.Attack, EnemyMoveTag.Attack | EnemyMoveTag.GuardPunish, weight: 10, cd: 2, maxCon: 1));
        moves.Add(M("asn_stab", "Quick Stab", EnemyIntentTag.Attack, EnemyMoveTag.Attack, weight: 11, cd: 0, maxCon: 3));
        moves.Add(M("asn_evade", "Evade / Guard", EnemyIntentTag.Defend, EnemyMoveTag.Buff | EnemyMoveTag.Guard, weight: 8, cd: 2, maxCon: 1, hpMin: 0f, hpMax: 0.6f));
        moves.Add(M("asn_setup", "Setup", EnemyIntentTag.Special, EnemyMoveTag.Setup | EnemyMoveTag.Special, weight: 7, cd: 3, maxCon: 1));
        moves.Add(M("asn_finish", "Finisher", EnemyIntentTag.Attack, EnemyMoveTag.Attack | EnemyMoveTag.Finisher, weight: 6, cd: 3, maxCon: 1, hpMin: 0f, hpMax: 0.5f));
    }

    private void AppendAssassinTemplate() => FillAssassinTemplate();

    // --- SUPPORT ---
    private void FillSupportTemplate()
    {
        moves.Add(M("sup_light", "Light Strike", EnemyIntentTag.Attack, EnemyMoveTag.Attack, weight: 12, cd: 0, maxCon: 3));
        moves.Add(M("sup_heal", "Heal Ally", EnemyIntentTag.Buff, EnemyMoveTag.Heal | EnemyMoveTag.Buff, weight: 10, cd: 2, maxCon: 1));
        moves.Add(M("sup_buff", "Buff Ally", EnemyIntentTag.Buff, EnemyMoveTag.Buff, weight: 10, cd: 2, maxCon: 1));
        moves.Add(M("sup_guard", "Guard Ally", EnemyIntentTag.Defend, EnemyMoveTag.Guard | EnemyMoveTag.Buff, weight: 9, cd: 2, maxCon: 1));
        moves.Add(M("sup_summon", "Summon", EnemyIntentTag.Summon, EnemyMoveTag.Summon, weight: 8, cd: 3, maxCon: 1));
        moves.Add(M("sup_cleanse", "Cleanse", EnemyIntentTag.Buff, EnemyMoveTag.Buff | EnemyMoveTag.Special, weight: 6, cd: 3, maxCon: 1));
    }

    private void AppendSupportTemplate() => FillSupportTemplate();
#endif

    // -------------------- Helpers --------------------

    public int FindMoveIndexById(string moveId)
    {
        if (moves == null) return -1;
        for (int i = 0; i < moves.Count; i++)
            if (string.Equals(moves[i].moveId, moveId, StringComparison.Ordinal))
                return i;
        return -1;
    }

}