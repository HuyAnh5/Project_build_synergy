using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Authoring asset that defines an enemy's identity, combat stats, and move-selection rules.
/// </summary>
[CreateAssetMenu(menuName = "GameData/Enemies/Enemy Definition", fileName = "EnemyDef_")]
public partial class EnemyDefinitionSO : ScriptableObject
{
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

    public enum EnemyIntentSelectionMode
    {
        WeightedPercent,
        ScriptedLoop
    }

    /// <summary>
    /// Authoring data for one enemy move slot in the move list.
    /// </summary>
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
        public bool ignoreNoRepeat;

        [BoxGroup("Turn Rules"), LabelText("Min Turn (>=)")]
        [MinValue(1)]
        [Tooltip("Move chỉ được phép dùng từ turn này trở đi. Ví dụ 2 = không dùng được ở turn 1.")]
        public int minTurnIndex = 1;

        [BoxGroup("Turn Rules"), LabelText("Force On Turn (0=off)")]
        [MinValue(0)]
        [Tooltip("Nếu = N (>0) thì move này bắt buộc dùng ở turn N nếu hợp lệ.")]
        public int forceOnTurn;

        [Space(8)]
        [Title("Skills (kéo skill của bạn vào đây)", TitleAlignment = TitleAlignments.Left)]
        [InfoBox("Chỉ cần kéo SkillDamageSO hoặc SkillBuffDebuffSO. Nội dung skill được đọc từ asset của chính skill đó.", InfoMessageType.Info)]
        [InlineEditor(InlineEditorObjectFieldModes.Boxed)]
        public SkillDamageSO damageSkill;

        [InlineEditor(InlineEditorObjectFieldModes.Boxed)]
        public SkillBuffDebuffSO buffDebuffSkill;

        public bool HasAnySkill
        {
            get { return damageSkill != null || buffDebuffSkill != null; }
        }

        private bool ValidateMoveId(string id)
        {
            return !string.IsNullOrWhiteSpace(id);
        }
    }

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
    [Tooltip("Tags phụ nếu bạn muốn filter pool theo thuộc tính khác như Flying/Undead/Beast.")]
    public List<string> extraTags = new List<string>();

    [BoxGroup("Stats")]
    [MinValue(1)]
    public int maxHP = 30;

    [BoxGroup("Stats")]
    [MinValue(0)]
    public int startingGuard;

    [BoxGroup("Stats")]
    [Tooltip("Balance knob dự phòng nếu sau này muốn scale damage từ definition.")]
    [MinValue(0f)]
    public float damageScalar = 1f;

    [BoxGroup("AI"), LabelText("Intent Selection Mode")]
    [Tooltip("WeightedPercent = chọn theo weight. ScriptedLoop = chạy Move 0 -> 1 -> ... rồi quay lại Loop Back To Move Index.")]
    public EnemyIntentSelectionMode intentSelectionMode = EnemyIntentSelectionMode.WeightedPercent;

    [BoxGroup("AI/Scripted Loop"), LabelText("Loop Back To Move Index")]
    [ShowIf(nameof(IsScriptedLoopMode))]
    [MinValue(0)]
    [Tooltip("Dùng khi Intent Selection Mode = ScriptedLoop. Đây là index quay lại sau khi đi hết danh sách move.")]
    public int loopBackToMoveNumber;

    [BoxGroup("AI/Weighted Percent"), LabelText("Burst Cap %")]
    [ShowIf(nameof(IsWeightedPercentMode))]
    [Range(0.5f, 1.0f)]
    [Tooltip("Safety knob cho weighted selector nếu sau này muốn hạn chế burst vượt ngưỡng HP player.")]
    public float burstCapPercent = 0.80f;

    [BoxGroup("AI/Weighted Percent"), LabelText("No Repeat Twice")]
    [ShowIf(nameof(IsWeightedPercentMode))]
    [Tooltip("Global anti-spam: tránh dùng cùng một move 2 turn liên tiếp nếu còn lựa chọn hợp lệ khác.")]
    public bool noRepeatTwice = true;

    [BoxGroup("AI/Weighted Percent"), LabelText("History Window")]
    [ShowIf(nameof(IsWeightedPercentMode))]
    [MinValue(1)]
    [Tooltip("Số turn history gần nhất dành cho rule anti-spam mở rộng.")]
    public int historyWindow = 2;

    [BoxGroup("Moves")]
    [InfoBox("Template mặc định là 6 move. Bạn có thể xóa bớt để còn 3-4 move nếu encounter chỉ cần ít hành vi hơn.", InfoMessageType.Info)]
    [ListDrawerSettings(DefaultExpandedState = true, DraggableItems = true, ShowIndexLabels = true, NumberOfItemsPerPage = 10)]
    public List<EnemyMoveSlot> moves = new List<EnemyMoveSlot>();

    private bool IsScriptedLoopMode
    {
        get { return intentSelectionMode == EnemyIntentSelectionMode.ScriptedLoop; }
    }

    private bool IsWeightedPercentMode
    {
        get { return intentSelectionMode == EnemyIntentSelectionMode.WeightedPercent; }
    }

    private bool ValidateEnemyId(string id)
    {
        return !string.IsNullOrWhiteSpace(id);
    }

    private bool ValidateMovesCount(List<EnemyMoveSlot> list)
    {
        return list != null && list.Count == 6;
    }

    /// <summary>
    /// Finds the first move whose id matches the provided authoring id.
    /// </summary>
    public int FindMoveIndexById(string moveId)
    {
        if (moves == null)
        {
            return -1;
        }

        for (int i = 0; i < moves.Count; i++)
        {
            if (string.Equals(moves[i].moveId, moveId, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }
}
