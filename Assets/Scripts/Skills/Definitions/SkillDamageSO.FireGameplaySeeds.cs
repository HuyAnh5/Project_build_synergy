using System.Collections.Generic;
using Sirenix.OdinInspector;

public partial class SkillDamageSO
{
    private void EnsureDefaultGameplayDescription()
    {
        if (gameplay == null || !string.IsNullOrWhiteSpace(gameplay.descriptionTemplate))
        {
            return;
        }

        if (string.Equals(displayName, "Fire Slash", System.StringComparison.OrdinalIgnoreCase) || fireBehaviorId == FireDamageBehaviorId.FireSlash)
        {
            gameplay.descriptionTemplate = "Deal {base1} damage. If {Odd}, apply {cond1_1} {Burn}.";
        }
        else if (string.Equals(displayName, "Ignite", System.StringComparison.OrdinalIgnoreCase) || fireBehaviorId == FireDamageBehaviorId.Ignite)
        {
            gameplay.descriptionTemplate = "Apply {base1} {Burn}. If {Odd}, apply {cond1_1} more {Burn}.";
        }
        else if (string.Equals(displayName, "Hellfire", System.StringComparison.OrdinalIgnoreCase) || fireBehaviorId == FireDamageBehaviorId.Hellfire)
        {
            gameplay.descriptionTemplate = "{Consume} all {Burn}. Deal {base2} damage.";
        }
        else if (string.Equals(displayName, "Bite the Dust", System.StringComparison.OrdinalIgnoreCase) || fireBehaviorId == FireDamageBehaviorId.BiteTheDust)
        {
            gameplay.descriptionTemplate = "{Consume} all {Burn}. Deal {base2} damage. Heal {base3} HP.";
        }
        else if (string.Equals(displayName, "Burn Consume", System.StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(displayName, "Consume Burn", System.StringComparison.OrdinalIgnoreCase) ||
                 (!string.IsNullOrEmpty(name) && name.ToLowerInvariant().Contains("consume_burn")))
        {
            gameplay.descriptionTemplate = "{Consume} all {Burn}. Deal {base2} damage.";
        }
    }

    [TabGroup("Tabs", "Gameplay")]
    [Button("Seed Fire Slash Gameplay Data")]
    [ShowIf("@displayName == \"Fire Slash\" || fireBehaviorId == FireDamageBehaviorId.FireSlash")]
    private void SeedFireSlashGameplayData()
    {
        if (gameplay == null)
        {
            gameplay = new SkillGameplayData();
        }

        gameplay.useNewGameplayPipeline = true;
        gameplay.descriptionTemplate = "Deal {base1} damage. If {Odd}, apply {cond1_1} {Burn}.";
        if (gameplay.requirements == null) gameplay.requirements = new List<SkillRequirementData>();
        if (gameplay.baseEffects == null) gameplay.baseEffects = new List<SkillEffectData>();
        if (gameplay.conditionalOutcomes == null) gameplay.conditionalOutcomes = new List<SkillConditionalOutcomeDataV2>();

        gameplay.requirements.Clear();
        gameplay.baseEffects.Clear();
        gameplay.conditionalOutcomes.Clear();

        gameplay.baseEffects.Add(new SkillEffectData
        {
            type = SkillEffectType.DealDamage,
            target = SkillEffectTarget.SelectedEnemy,
            value = new SkillValueData
            {
                baseAmount = 2,
                mode = SkillValueMode.AddedValueScaled
            },
            previewable = true
        });

        gameplay.conditionalOutcomes.Add(new SkillConditionalOutcomeDataV2
        {
            condition = new SkillConditionData
            {
                scope = SkillConditionScope.SlotBound,
                logic = SkillConditionLogic.All,
                clauses = new List<SkillConditionClause>
                {
                    new SkillConditionClause
                    {
                        reference = SkillConditionReference.AnyBaseValue,
                        comparison = SkillConditionComparison.IsOdd,
                        value = 0
                    }
                }
            },
            effects = new List<SkillEffectData>
            {
                new SkillEffectData
                {
                    type = SkillEffectType.ApplyStatus,
                    target = SkillEffectTarget.SelectedEnemy,
                    status = StatusKind.Burn,
                    value = new SkillValueData
                    {
                        baseAmount = 6,
                        mode = SkillValueMode.Fixed
                    },
                    previewable = true
                }
            }
        });
    }

    [TabGroup("Tabs", "Gameplay")]
    [Button("Seed Consume Burn Gameplay Data")]
    [ShowIf("@displayName == \"Burn Consume\" || displayName == \"Consume Burn\"")]
    private void SeedConsumeBurnGameplayData()
    {
        if (gameplay == null)
        {
            gameplay = new SkillGameplayData();
        }

        gameplay.useNewGameplayPipeline = true;
        gameplay.descriptionTemplate = "{Consume} all {Burn}. Deal {base2} damage.";
        if (gameplay.requirements == null) gameplay.requirements = new List<SkillRequirementData>();
        if (gameplay.baseEffects == null) gameplay.baseEffects = new List<SkillEffectData>();
        if (gameplay.conditionalOutcomes == null) gameplay.conditionalOutcomes = new List<SkillConditionalOutcomeDataV2>();

        gameplay.requirements.Clear();
        gameplay.baseEffects.Clear();
        gameplay.conditionalOutcomes.Clear();

        gameplay.requirements.Add(BuildTargetHasBurnRequirement());

        gameplay.baseEffects.Add(new SkillEffectData
        {
            type = SkillEffectType.ConsumeStatus,
            target = SkillEffectTarget.SelectedEnemy,
            status = StatusKind.Burn,
            previewable = true
        });

        gameplay.baseEffects.Add(new SkillEffectData
        {
            type = SkillEffectType.DealDamage,
            target = SkillEffectTarget.SelectedEnemy,
            value = new SkillValueData
            {
                baseAmount = 2,
                mode = SkillValueMode.ConsumedStatusStacksScaled,
                status = StatusKind.Burn
            },
            previewable = true
        });
    }

    [TabGroup("Tabs", "Gameplay")]
    [Button("Seed Ignite Gameplay Data")]
    [ShowIf("@displayName == \"Ignite\" || fireBehaviorId == FireDamageBehaviorId.Ignite")]
    private void SeedIgniteGameplayData()
    {
        EnsureGameplayCollections();
        gameplay.useNewGameplayPipeline = true;
        gameplay.descriptionTemplate = "Apply {base1} {Burn}. If {Odd}, apply {cond1_1} more {Burn}.";

        gameplay.requirements.Clear();
        gameplay.baseEffects.Clear();
        gameplay.conditionalOutcomes.Clear();

        gameplay.baseEffects.Add(new SkillEffectData
        {
            type = SkillEffectType.ApplyStatus,
            target = SkillEffectTarget.SelectedEnemy,
            status = StatusKind.Burn,
            value = new SkillValueData
            {
                baseAmount = 3,
                mode = SkillValueMode.AddedValueScaled
            },
            previewable = true
        });

        gameplay.conditionalOutcomes.Add(new SkillConditionalOutcomeDataV2
        {
            condition = BuildParityCondition(SkillConditionComparison.IsOdd),
            effects = new List<SkillEffectData>
            {
                new SkillEffectData
                {
                    type = SkillEffectType.ApplyStatus,
                    target = SkillEffectTarget.SelectedEnemy,
                    status = StatusKind.Burn,
                    value = new SkillValueData
                    {
                        baseAmount = 2,
                        mode = SkillValueMode.Fixed
                    },
                    previewable = true
                }
            }
        });
    }

    [TabGroup("Tabs", "Gameplay")]
    [Button("Seed Hellfire Gameplay Data")]
    [ShowIf("@displayName == \"Hellfire\" || fireBehaviorId == FireDamageBehaviorId.Hellfire")]
    private void SeedHellfireGameplayData()
    {
        EnsureGameplayCollections();
        gameplay.useNewGameplayPipeline = true;
        gameplay.descriptionTemplate = "{Consume} all {Burn}. Deal {base2} damage.";

        gameplay.requirements.Clear();
        gameplay.baseEffects.Clear();
        gameplay.conditionalOutcomes.Clear();

        gameplay.requirements.Add(BuildTargetHasBurnRequirement());

        gameplay.baseEffects.Add(new SkillEffectData
        {
            type = SkillEffectType.ConsumeStatus,
            target = SkillEffectTarget.SelectedEnemy,
            status = StatusKind.Burn,
            previewable = true
        });

        gameplay.baseEffects.Add(new SkillEffectData
        {
            type = SkillEffectType.DealDamage,
            target = SkillEffectTarget.SelectedEnemy,
            value = new SkillValueData
            {
                baseAmount = 3,
                mode = SkillValueMode.ConsumedStatusStacksScaled,
                status = StatusKind.Burn
            },
            previewable = true
        });

        gameplay.conditionalOutcomes.Add(new SkillConditionalOutcomeDataV2
        {
            condition = new SkillConditionData
            {
                scope = SkillConditionScope.SlotBound,
                logic = SkillConditionLogic.All,
                clauses = new List<SkillConditionClause>
                {
                    new SkillConditionClause
                    {
                        reference = SkillConditionReference.AnyBaseValue,
                        comparison = SkillConditionComparison.Equals,
                        value = 7
                    }
                }
            },
            effects = new List<SkillEffectData>
            {
                new SkillEffectData
                {
                    type = SkillEffectType.ApplyStatus,
                    target = SkillEffectTarget.SelectedEnemy,
                    status = StatusKind.Burn,
                    value = new SkillValueData
                    {
                        baseAmount = 7,
                        mode = SkillValueMode.MatchingBaseValueCountScaled,
                        matchBaseValue = 7
                    },
                    previewable = true
                }
            }
        });
    }

    private void MigrateHellfireMatchSevenEffectToCondition()
    {
        if (gameplay == null || gameplay.baseEffects == null)
        {
            return;
        }

        SkillEffectData matchSevenEffect = null;
        for (int i = gameplay.baseEffects.Count - 1; i >= 0; i--)
        {
            SkillEffectData effect = gameplay.baseEffects[i];
            if (effect == null ||
                effect.type != SkillEffectType.ApplyStatus ||
                effect.status != StatusKind.Burn ||
                effect.value == null ||
                effect.value.mode != SkillValueMode.MatchingBaseValueCountScaled ||
                effect.value.matchBaseValue != 7)
            {
                continue;
            }

            matchSevenEffect = effect;
            gameplay.baseEffects.RemoveAt(i);
        }

        if (matchSevenEffect == null)
        {
            return;
        }

        if (gameplay.conditionalOutcomes == null)
        {
            gameplay.conditionalOutcomes = new List<SkillConditionalOutcomeDataV2>();
        }

        bool alreadyHasMatchSevenCondition = false;
        for (int i = 0; i < gameplay.conditionalOutcomes.Count; i++)
        {
            SkillConditionalOutcomeDataV2 branch = gameplay.conditionalOutcomes[i];
            if (branch == null || branch.condition == null || branch.condition.clauses == null)
            {
                continue;
            }

            for (int clauseIndex = 0; clauseIndex < branch.condition.clauses.Count; clauseIndex++)
            {
                SkillConditionClause clause = branch.condition.clauses[clauseIndex];
                if (clause != null &&
                    clause.reference == SkillConditionReference.AnyBaseValue &&
                    clause.comparison == SkillConditionComparison.Equals &&
                    clause.value == 7)
                {
                    alreadyHasMatchSevenCondition = true;
                    if (branch.effects == null)
                    {
                        branch.effects = new List<SkillEffectData>();
                    }

                    branch.effects.Add(matchSevenEffect);
                    break;
                }
            }
        }

        if (!alreadyHasMatchSevenCondition)
        {
            gameplay.conditionalOutcomes.Add(new SkillConditionalOutcomeDataV2
            {
                condition = new SkillConditionData
                {
                    scope = SkillConditionScope.SlotBound,
                    logic = SkillConditionLogic.All,
                    clauses = new List<SkillConditionClause>
                    {
                        new SkillConditionClause
                        {
                            reference = SkillConditionReference.AnyBaseValue,
                            comparison = SkillConditionComparison.Equals,
                            value = 7
                        }
                    }
                },
                effects = new List<SkillEffectData> { matchSevenEffect }
            });
        }

        gameplay.descriptionTemplate = "{Consume} all {Burn}. Deal {base2} damage.";
    }

    [TabGroup("Tabs", "Gameplay")]
    [Button("Seed Bite The Dust Gameplay Data")]
    [ShowIf("@displayName == \"Bite the Dust\" || fireBehaviorId == FireDamageBehaviorId.BiteTheDust")]
    private void SeedBiteTheDustGameplayData()
    {
        EnsureGameplayCollections();
        gameplay.useNewGameplayPipeline = true;
        gameplay.descriptionTemplate = "{Consume} all {Burn}. Deal {base2} damage. Heal {base3} HP.";

        gameplay.requirements.Clear();
        gameplay.baseEffects.Clear();
        gameplay.conditionalOutcomes.Clear();

        gameplay.requirements.Add(BuildTargetHasBurnRequirement());

        gameplay.baseEffects.Add(new SkillEffectData
        {
            type = SkillEffectType.ConsumeStatus,
            target = SkillEffectTarget.SelectedEnemy,
            status = StatusKind.Burn,
            previewable = true
        });

        gameplay.baseEffects.Add(new SkillEffectData
        {
            type = SkillEffectType.DealDamage,
            target = SkillEffectTarget.SelectedEnemy,
            value = new SkillValueData
            {
                baseAmount = 1,
                mode = SkillValueMode.ConsumedStatusStacksScaled,
                status = StatusKind.Burn
            },
            previewable = true
        });

        gameplay.baseEffects.Add(new SkillEffectData
        {
            type = SkillEffectType.Heal,
            target = SkillEffectTarget.Self,
            value = new SkillValueData
            {
                baseAmount = 3,
                mode = SkillValueMode.ConsumedStatusStacksDividedScaled,
                status = StatusKind.Burn,
                divisor = 5
            },
            previewable = true
        });
    }

    private void EnsureGameplayCollections()
    {
        if (gameplay == null)
        {
            gameplay = new SkillGameplayData();
        }

        if (gameplay.requirements == null) gameplay.requirements = new List<SkillRequirementData>();
        if (gameplay.baseEffects == null) gameplay.baseEffects = new List<SkillEffectData>();
        if (gameplay.conditionalOutcomes == null) gameplay.conditionalOutcomes = new List<SkillConditionalOutcomeDataV2>();
    }

    private static SkillConditionData BuildParityCondition(SkillConditionComparison comparison)
    {
        return new SkillConditionData
        {
            scope = SkillConditionScope.SlotBound,
            logic = SkillConditionLogic.All,
            clauses = new List<SkillConditionClause>
            {
                new SkillConditionClause
                {
                    reference = SkillConditionReference.AnyBaseValue,
                    comparison = comparison,
                    value = 0
                }
            }
        };
    }

    private static SkillRequirementData BuildTargetHasBurnRequirement()
    {
        return new SkillRequirementData
        {
            type = SkillRequirementType.Condition,
            failureText = "Target needs Burn.",
            condition = new SkillConditionData
            {
                scope = SkillConditionScope.SlotBound,
                logic = SkillConditionLogic.All,
                clauses = new List<SkillConditionClause>
                {
                    new SkillConditionClause
                    {
                        reference = SkillConditionReference.TargetHasBurn,
                        comparison = SkillConditionComparison.IsTrue,
                        value = 0
                    }
                }
            }
        };
    }
}
