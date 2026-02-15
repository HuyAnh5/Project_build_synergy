// SkillBuffDebuffSO.cs (UI-friendly Odin layout)
using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public enum AilmentType
{
    Sleep,
    Dizzy,
    Confuse,
    Fear,
    Forget,
    Rage,
    Despair,
    Brainwash
}

public enum BuffDebuffIdentity
{
    Buff,
    Debuff
}

/// <summary>
/// ONLY the standardized effects you locked in.
/// </summary>
public enum BuffDebuffEffectId
{
    // Persona-style buff: x1.5 / x2 for X turns (ceil damage later in runtime)
    DamageMultiplier,

    // Focus delayed: +3 on next turn or next 2 turns (controlled by applyDelayTurns)
    FocusDelayed,

    // Heal:
    // - Enemy/Ally: fixed number
    HealFlat,
    // - Player: heal by dice sum (runtime sums occupied slot dice values)
    HealByDiceSum,

    // Dice-layer (Enemy debuff -> Player/Ally, Ally buff -> Player/Ally)
    DiceAllDelta,        // -2 or +2 (duration usually 1)
    ParityFocusDelta,    // even +/-2, odd +/-1 (duration usually 1)
    SlotCollapse         // random keep 1 slot, lock others (duration usually 1)
}

[Serializable]
public class AilmentConfig
{
    [HorizontalGroup("Row1", Width = 260)]
    [HideLabel, EnumToggleButtons]
    public AilmentType ailment = AilmentType.Sleep;

    [HorizontalGroup("Row1", Width = 120)]
    [LabelText("Turns")]
    [Min(1)]
    public int durationTurns = 1;

    [HorizontalGroup("Row1")]
    [LabelText("Chance x")]
    [Range(0f, 2f)]
    [Tooltip("Tuning multiplier applied after dice-based chance rules.")]
    public float chanceTuningMultiplier = 1f;

    [PropertySpace(6)]
    [ShowIf("@ailment == AilmentType.Sleep")]
    [BoxGroup("Sleep")]
    [ToggleLeft]
    [LabelText("Break On Damage")]
    public bool breakOnDamage = true;

    [BoxGroup("Sleep")]
    [ShowIf("@ailment == AilmentType.Sleep")]
    [LabelText("Wake Damage Bonus (+50% = 0.5)")]
    public float sleep_damageBonusOnWake = 0.5f;

    [BoxGroup("Sleep")]
    [ShowIf("@ailment == AilmentType.Sleep")]
    [LabelText("Heal % MaxHP On Turn Start (20% = 0.2)")]
    public float sleep_healPercentOnTurnStart = 0.2f;

    [ShowIf("@ailment == AilmentType.Dizzy")]
    [BoxGroup("Dizzy")]
    [LabelText("Damage Reduction (25% = 0.25)")]
    public float dizzy_damageReductionPercent = 0.25f;

    [ShowIf("@ailment == AilmentType.Dizzy")]
    [BoxGroup("Dizzy")]
    [ToggleLeft]
    [LabelText("Use Accuracy Penalty Instead")]
    public bool dizzy_useAccuracyPenalty = false;

    [ShowIf("@ailment == AilmentType.Dizzy && dizzy_useAccuracyPenalty")]
    [BoxGroup("Dizzy")]
    [LabelText("Accuracy Reduction (30% = 0.3)")]
    public float dizzy_accuracyReductionPercent = 0.30f;

    [ShowIf("@ailment == AilmentType.Confuse")]
    [BoxGroup("Confuse")]
    [LabelText("Self Hit Chance")]
    [Range(0f, 1f)]
    public float confuse_selfHitChance = 0.5f;

    [ShowIf("@ailment == AilmentType.Fear")]
    [BoxGroup("Fear")]
    [LabelText("Skip Chance")]
    [Range(0f, 1f)]
    public float fear_skipChance = 0.5f;

    [ShowIf("@ailment == AilmentType.Fear")]
    [BoxGroup("Fear")]
    [LabelText("Flee Chance (enemy/ally only)")]
    [Range(0f, 1f)]
    public float fear_fleeChance = 0.05f;

    [ShowIf("@ailment == AilmentType.Fear")]
    [BoxGroup("Fear")]
    [LabelText("Damage Reduction (25% = 0.25)")]
    public float fear_damageReductionPercent = 0.25f;

    [ShowIf("@ailment == AilmentType.Forget")]
    [BoxGroup("Forget")]
    [ToggleLeft]
    [LabelText("Disable Advanced Skills")]
    public bool forget_disableAdvancedSkills = true;

    [ShowIf("@ailment == AilmentType.Rage")]
    [BoxGroup("Rage")]
    [LabelText("Damage Bonus (30% = 0.3)")]
    public float rage_damageBonusPercent = 0.30f;

    [ShowIf("@ailment == AilmentType.Despair")]
    [BoxGroup("Despair")]
    [LabelText("HP Loss % MaxHP (10% = 0.1)")]
    public float despair_hpLossPercent = 0.10f;

    [ShowIf("@ailment == AilmentType.Despair")]
    [BoxGroup("Despair")]
    [LabelText("Focus Loss")]
    public int despair_focusLoss = 1;

    [ShowIf("@ailment == AilmentType.Brainwash")]
    [BoxGroup("Brainwash")]
    [ToggleLeft]
    [LabelText("Forced Basic Only")]
    public bool brainwash_forcedBasicOnly = true;
}

[Serializable, InlineProperty]
public class BuffDebuffEffectEntry
{
    [HorizontalGroup("H", Width = 240)]
    [HideLabel]
    public BuffDebuffEffectId id = BuffDebuffEffectId.DamageMultiplier;

    [HorizontalGroup("H", Width = 90)]
    [LabelText("Turns")]
    [Min(0)]
    public int durationTurns = 1;

    [HorizontalGroup("H")]
    [ShowIf(nameof(ShowCompactParam))]
    [LabelText("@CompactLabel")]
    public float compactParam = 1.5f;

    // ---- ParityFocusDelta ----
    [ShowIf(nameof(ShowParityParams))]
    [HorizontalGroup("ParityRow", LabelWidth = 55)]
    [LabelText("Even")]
    public int parityEvenDelta = -2;

    [ShowIf(nameof(ShowParityParams))]
    [HorizontalGroup("ParityRow", LabelWidth = 55)]
    [LabelText("Odd")]
    public int parityOddDelta = -1;

    [ShowIf(nameof(ShowSlotCollapseHelp))]
    [InfoBox("SlotCollapse: runtime sẽ random chọn 1 slot giữ lại và khoá các slot còn lại (thường 1 turn).", InfoMessageType.Warning)]
    [HideInInspector]
    public bool _slotCollapseHelp = true;

    [ShowIf(nameof(ShowHealByDiceHelp))]
    [InfoBox("HealByDiceSum: runtime sẽ heal theo tổng dice value của những slot skill đang chiếm.", InfoMessageType.Info)]
    [HideInInspector]
    public bool _healByDiceHelp = true;

    // ---- UI helpers ----
    private bool ShowCompactParam =>
        id == BuffDebuffEffectId.DamageMultiplier ||
        id == BuffDebuffEffectId.FocusDelayed ||
        id == BuffDebuffEffectId.HealFlat ||
        id == BuffDebuffEffectId.DiceAllDelta;

    private string CompactLabel
    {
        get
        {
            switch (id)
            {
                case BuffDebuffEffectId.DamageMultiplier: return "xDMG";
                case BuffDebuffEffectId.FocusDelayed: return "+FOCUS";
                case BuffDebuffEffectId.HealFlat: return "HEAL";
                case BuffDebuffEffectId.DiceAllDelta: return "ΔDICE";
                default: return "VAL";
            }
        }
    }

    private bool ShowParityParams => id == BuffDebuffEffectId.ParityFocusDelta;
    private bool ShowSlotCollapseHelp => id == BuffDebuffEffectId.SlotCollapse;
    private bool ShowHealByDiceHelp => id == BuffDebuffEffectId.HealByDiceSum;

    // ---- Value mapping (compactParam) ----
    public void SetDefaultsForId(BuffDebuffIdentity identity)
    {
        durationTurns = Mathf.Max(1, durationTurns);

        switch (id)
        {
            case BuffDebuffEffectId.DamageMultiplier:
                compactParam = 1.5f; // x1.5 default
                break;

            case BuffDebuffEffectId.FocusDelayed:
                compactParam = 3f; // +3 default
                durationTurns = 0; // instant add when delayed triggers (duration not used)
                break;

            case BuffDebuffEffectId.HealFlat:
                compactParam = 6f; // heal 6 default
                durationTurns = 0;
                break;

            case BuffDebuffEffectId.DiceAllDelta:
                compactParam = (identity == BuffDebuffIdentity.Buff) ? +2f : -2f;
                durationTurns = 1;
                break;

            case BuffDebuffEffectId.ParityFocusDelta:
                parityEvenDelta = (identity == BuffDebuffIdentity.Buff) ? +2 : -2;
                parityOddDelta = (identity == BuffDebuffIdentity.Buff) ? +1 : -1;
                durationTurns = 1;
                break;

            case BuffDebuffEffectId.SlotCollapse:
                durationTurns = 1;
                break;

            case BuffDebuffEffectId.HealByDiceSum:
                durationTurns = 0;
                break;
        }
    }

    // Read values back in a consistent way
    public float GetDamageMultiplier() => (id == BuffDebuffEffectId.DamageMultiplier) ? compactParam : 1f;
    public int GetFocusAmount() => (id == BuffDebuffEffectId.FocusDelayed) ? Mathf.RoundToInt(compactParam) : 0;
    public int GetHealAmount() => (id == BuffDebuffEffectId.HealFlat) ? Mathf.RoundToInt(compactParam) : 0;
    public int GetDiceAllDelta() => (id == BuffDebuffEffectId.DiceAllDelta) ? Mathf.RoundToInt(compactParam) : 0;
}

[CreateAssetMenu(menuName = "Game/Skill/BuffDebuff", fileName = "SkillBuffDebuff_")]
public class SkillBuffDebuffSO : ScriptableObject
{
    [Title("Skill (Buff/Debuff)", bold: true)]
    [HorizontalGroup("Top", Width = 70)]
    [HideLabel, PreviewField(70, ObjectFieldAlignment.Left)]
    public Sprite icon;

    [VerticalGroup("Top/Info")]
    [LabelText("Display Name")]
    public string displayName;

    [VerticalGroup("Top/Info")]
    [TextArea(2, 4)]
    public string description;

    // ---------------- Tabs ----------------
    [TabGroup("Tabs", "Core")]
    [BoxGroup("Tabs/Core/Identity")]
    [EnumToggleButtons]
    public BuffDebuffIdentity identity = BuffDebuffIdentity.Buff;

    [TabGroup("Tabs", "Core")]
    [BoxGroup("Tabs/Core/Targeting")]
    [EnumToggleButtons]
    public SkillTargetRule target = SkillTargetRule.Self;

    [TabGroup("Tabs", "Core")]
    [BoxGroup("Tabs/Core/Timing")]
    [Min(0)]
    [LabelText("Apply Delay Turns")]
    [Tooltip("0 = apply now, 1 = apply next turn, 2 = apply after 2 turns.")]
    public int applyDelayTurns = 0;

    [TabGroup("Tabs", "Core")]
    [BoxGroup("Tabs/Core/Slots & Cost")]
    [MinValue(1), MaxValue(3)]
    public int slotsRequired = 1;

    [TabGroup("Tabs", "Core")]
    [BoxGroup("Tabs/Core/Slots & Cost")]
    [Min(0)]
    public int focusCost = 0;

    [TabGroup("Tabs", "Core")]
    [BoxGroup("Tabs/Core/Slots & Cost")]
    public int focusGainOnCast = 0;

    // ---------------- Effects tab ----------------
    [TabGroup("Tabs", "Effects")]
    [TitleGroup("Tabs/Effects/Quick Add", "Click to add a pre-configured entry", Alignment = TitleAlignments.Centered)]
    [ButtonGroup("Tabs/Effects/Quick Add/Row1")]
    [Button("DMG x1.5 (2T)", ButtonSizes.Medium)]
    private void Add_Dmg15_2T()
    {
        AddEffect(BuffDebuffEffectId.DamageMultiplier, durationTurns: 2, init: e => e.compactParam = 1.5f);
    }

    [ButtonGroup("Tabs/Effects/Quick Add/Row1")]
    [Button("DMG x2 (1T)", ButtonSizes.Medium)]
    private void Add_Dmg2_1T()
    {
        AddEffect(BuffDebuffEffectId.DamageMultiplier, durationTurns: 1, init: e => e.compactParam = 2.0f);
    }

    [ButtonGroup("Tabs/Effects/Quick Add/Row2")]
    [Button("+3 Focus (Delay1)", ButtonSizes.Medium)]
    private void Add_FocusPlus3_D1()
    {
        applyDelayTurns = 1;
        AddEffect(BuffDebuffEffectId.FocusDelayed, durationTurns: 0, init: e => e.compactParam = 3f);
    }

    [ButtonGroup("Tabs/Effects/Quick Add/Row2")]
    [Button("+3 Focus (Delay2)", ButtonSizes.Medium)]
    private void Add_FocusPlus3_D2()
    {
        applyDelayTurns = 2;
        AddEffect(BuffDebuffEffectId.FocusDelayed, durationTurns: 0, init: e => e.compactParam = 3f);
    }

    [ButtonGroup("Tabs/Effects/Quick Add/Row3")]
    [Button("Heal Flat", ButtonSizes.Medium)]
    private void Add_HealFlat()
    {
        AddEffect(BuffDebuffEffectId.HealFlat, durationTurns: 0);
    }

    [ButtonGroup("Tabs/Effects/Quick Add/Row3")]
    [Button("Heal = Dice Sum", ButtonSizes.Medium)]
    private void Add_HealDiceSum()
    {
        AddEffect(BuffDebuffEffectId.HealByDiceSum, durationTurns: 0);
    }

    [ButtonGroup("Tabs/Effects/Quick Add/Row4")]
    [Button("ΔDice All (-2/+2)", ButtonSizes.Medium)]
    private void Add_DiceAllDelta()
    {
        AddEffect(BuffDebuffEffectId.DiceAllDelta, durationTurns: 1);
    }

    [ButtonGroup("Tabs/Effects/Quick Add/Row4")]
    [Button("Parity Focus (±)", ButtonSizes.Medium)]
    private void Add_ParityFocus()
    {
        AddEffect(BuffDebuffEffectId.ParityFocusDelta, durationTurns: 1);
    }

    [ButtonGroup("Tabs/Effects/Quick Add/Row4")]
    [Button("Slot Collapse", ButtonSizes.Medium)]
    private void Add_SlotCollapse()
    {
        AddEffect(BuffDebuffEffectId.SlotCollapse, durationTurns: 1);
    }

    [TabGroup("Tabs", "Effects")]
    [PropertySpace(10)]
    [InfoBox("Tip: FocusDelayed nên set ApplyDelayTurns = 1 hoặc 2 (bạn có thể bấm preset).", InfoMessageType.Info)]
    [ShowIf(nameof(HasFocusDelayedButDelayIsZero))]
    [InfoBox("Warning: Skill có FocusDelayed nhưng ApplyDelayTurns đang = 0. Nếu bạn muốn 'turn sau/turn sau 2', hãy set ApplyDelayTurns.", InfoMessageType.Warning)]
    public bool _focusDelayedDelayWarning = true;

    [TabGroup("Tabs", "Effects")]
    [TitleGroup("Tabs/Effects/Effect List", "Each entry is compact: ID + Turns + Param", Alignment = TitleAlignments.Centered)]
    [ListDrawerSettings(Expanded = true, DraggableItems = true, ShowIndexLabels = true)]
    [HideLabel]
    public List<BuffDebuffEffectEntry> effects = new();

    // ---------------- Ailment tab ----------------
    [TabGroup("Tabs", "Ailment")]
    [ToggleLeft]
    [LabelText("Apply Ailment")]
    public bool applyAilment = false;

    [TabGroup("Tabs", "Ailment")]
    [ShowIf(nameof(applyAilment))]
    [HideLabel]
    public AilmentConfig ailment = new AilmentConfig();

    // ---------------- Helpers ----------------
    private bool HasFocusDelayedButDelayIsZero()
    {
        if (applyDelayTurns != 0) return false;
        if (effects == null) return false;
        for (int i = 0; i < effects.Count; i++)
            if (effects[i] != null && effects[i].id == BuffDebuffEffectId.FocusDelayed)
                return true;
        return false;
    }

    private void AddEffect(BuffDebuffEffectId id, int durationTurns, Action<BuffDebuffEffectEntry> init = null)
    {
        if (effects == null) effects = new List<BuffDebuffEffectEntry>();

        var e = new BuffDebuffEffectEntry { id = id, durationTurns = durationTurns };
        e.SetDefaultsForId(identity);

        init?.Invoke(e);
        effects.Add(e);
    }

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(displayName))
            displayName = name;

        slotsRequired = Mathf.Clamp(slotsRequired, 1, 3);
        focusCost = Mathf.Max(0, focusCost);
        applyDelayTurns = Mathf.Max(0, applyDelayTurns);

        if (applyAilment && ailment != null)
        {
            // Only Sleep breaks on hit
            if (ailment.ailment != AilmentType.Sleep)
                ailment.breakOnDamage = false;
        }
    }
}
