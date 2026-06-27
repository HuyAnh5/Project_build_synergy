using System.Collections.Generic;
using UnityEngine;

internal sealed partial class SkillIconPreviewController
{
    private void ShowActionPreviewBundle(TargetPreviewBuilder.ActionPreviewBundle bundle)
    {
        CombatHUD hud = GetCachedHud();
        CombatTargetPreviewPresenter.ShowBundle(bundle, _cachedActorWorldUis, hud);

        ScriptableObject asset = _getSkillAsset();
        if (hud != null && _turn != null && _turn.player != null && asset != null &&
            SkillUiMetadataUtility.TryGetSkillCosts(asset, out int focusCost, out _))
        {
            hud.ShowFocusPreview(focusCost, Mathf.Max(0, bundle.totalSelfFocusGain + _simpleEnchantPreview.focusGain), _turn.player.focus < focusCost);
        }
    }

    private DiceCombatEnchantRuntimeUtility.SimpleEnchantPreview ComputeSimpleEnchantPreview(int slotsRequired)
    {
        if (_turn == null || _turn.diceRig == null || _turn.player == null || !_turn.diceRig.HasRolledThisTurn)
            return default;

        ScriptableObject asset = _getSkillAsset();
        return asset != null && _turn.TryGetPrototypeSkillSimpleEnchantPreview(asset, slotsRequired, out var preview)
            ? preview
            : default;
    }

    private void ShowSelfEnchantGuardPreview()
    {
        if (_turn == null || _turn.player == null)
            return;

        ActorWorldUI playerUi = FindActorWorldUi(_turn.player);
        if (playerUi == null)
            return;

        if (_simpleEnchantPreview.guardGain <= 0)
            return;

        CombatActor player = _turn.player;
        TargetPreviewData data = new TargetPreviewData
        {
            valid = true,
            currentHp = player.hp,
            currentMaxHp = player.maxHP,
            currentGuard = player.guardPool,
            previewHpAfter = player.hp,
            previewGuardAfter = player.guardPool + _simpleEnchantPreview.guardGain,
            currentlyStaggered = player.status != null && player.status.staggered,
            currentBurn = player.status != null ? player.status.burnStacks : 0,
            currentBleed = player.status != null ? player.status.bleedStacks : 0,
            currentMarked = player.status != null && player.status.marked,
            currentFrozen = player.status != null && player.status.frozen,
            previewBurnAfter = player.status != null ? player.status.burnStacks : 0,
            previewBleedAfter = player.status != null ? player.status.bleedStacks : 0,
            previewMarkedAfter = player.status != null && player.status.marked,
            previewFrozenAfter = player.status != null && player.status.frozen,
            isSelfTarget = true,
            selfGuardGain = _simpleEnchantPreview.guardGain
        };

        playerUi.ShowTargetPreview(data);
    }
}
