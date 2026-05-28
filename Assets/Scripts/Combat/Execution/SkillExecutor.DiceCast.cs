using System.Collections.Generic;
using UnityEngine;

public partial class SkillExecutor
{
    [Header("Player Dice Cast")]
    public PlayerDiceCastAnimator playerDiceCastAnimator;

    public void ResetPlayerCastVisualState()
    {
        if (playerDiceCastAnimator == null)
        {
            playerDiceCastAnimator = GetComponent<PlayerDiceCastAnimator>();
        }

        if (playerDiceCastAnimator != null)
        {
            playerDiceCastAnimator.ResetAllVisualState();
        }
    }

    private bool ShouldUsePlayerDiceCastAnimation(CombatActor caster, DiceSlotRig castDiceRig, int castStart0, int castSpan)
    {
        if (caster == null || castDiceRig == null)
        {
            return false;
        }

        if (!caster.isPlayer)
        {
            return false;
        }

        if (castStart0 < 0 || castSpan <= 0)
        {
            return false;
        }

        return GetPlayerDiceCastAnimator() != null;
    }

    private PlayerDiceCastAnimator GetPlayerDiceCastAnimator()
    {
        if (playerDiceCastAnimator != null)
        {
            return playerDiceCastAnimator;
        }

        playerDiceCastAnimator = GetComponent<PlayerDiceCastAnimator>();
        if (playerDiceCastAnimator == null)
        {
            playerDiceCastAnimator = gameObject.AddComponent<PlayerDiceCastAnimator>();
        }

        return playerDiceCastAnimator;
    }

    private static PlayerDiceCastAnimator.CastMode ResolveCastMode(
        CombatActor caster,
        CombatActor primaryTarget,
        IReadOnlyList<CombatActor> aoeTargets)
    {
        if (caster == null || primaryTarget == null)
        {
            return PlayerDiceCastAnimator.CastMode.Self;
        }

        if (primaryTarget.team == caster.team)
        {
            return PlayerDiceCastAnimator.CastMode.Self;
        }

        return aoeTargets != null && aoeTargets.Count > 1
            ? PlayerDiceCastAnimator.CastMode.EnemyAoeAnchor
            : PlayerDiceCastAnimator.CastMode.EnemySingle;
    }
}
