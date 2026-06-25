using Sirenix.OdinInspector;
using UnityEngine;

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

[CreateAssetMenu(menuName = "Game/Skill/BuffDebuff", fileName = "SkillBuffDebuff_")]
public partial class SkillBuffDebuffSO : ScriptableObject
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

    [TabGroup("Tabs", "Spec")]
    [BoxGroup("Tabs/Spec/Metadata")]
    [InlineProperty]
    public SkillSpecMetadata spec = new SkillSpecMetadata();

    [TabGroup("Tabs", "Core")]
    [BoxGroup("Tabs/Core/Identity")]
    [EnumToggleButtons]
    public BuffDebuffIdentity identity = BuffDebuffIdentity.Buff;

    [TabGroup("Tabs", "Core")]
    [BoxGroup("Tabs/Core/Targeting")]
    [EnumToggleButtons]
    public SkillTargetRule target = SkillTargetRule.Self;

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

    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(displayName))
            displayName = name;

        slotsRequired = Mathf.Clamp(slotsRequired, 1, 3);
        focusCost = Mathf.Max(0, focusCost);
        NormalizeLegacyFlowEffects();
    }

    private void NormalizeLegacyFlowEffects()
    {
        if (gameplay == null)
            return;

        NormalizeEffectList(gameplay.baseEffects);

        if (gameplay.conditionalOutcomes == null)
            return;

        for (int i = 0; i < gameplay.conditionalOutcomes.Count; i++)
        {
            BuffDebuffFlowConditionalOutcomeData branch = gameplay.conditionalOutcomes[i];
            if (branch == null)
                continue;

            NormalizeEffectList(branch.effects);
        }
    }

    private static void NormalizeEffectList(System.Collections.Generic.List<BuffDebuffFlowEffectData> effects)
    {
        if (effects == null)
            return;

        for (int i = 0; i < effects.Count; i++)
            effects[i]?.NormalizeLegacyType();
    }
}
