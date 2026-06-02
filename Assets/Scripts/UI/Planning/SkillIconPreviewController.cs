using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

internal sealed class SkillIconPreviewController
{
    private static readonly HashSet<DiceSpinnerGeneric> CachedSpentSet = new HashSet<DiceSpinnerGeneric>();
    private static readonly HashSet<DiceSpinnerGeneric> CachedPendingUsedVisualSet = new HashSet<DiceSpinnerGeneric>();

    private readonly TurnManager _turn;
    private readonly SelfCastDropZone _selfCastZone;
    private readonly Camera _uiCamera;
    private readonly Func<ScriptableObject> _getSkillAsset;
    private readonly Func<SkillRuntime, int> _getPreviewDieValue;
    private CombatHUD _cachedHud;
    private bool _resourcePreviewActive;
    private ActorWorldUI _currentPreviewTarget;
    private SkillRuntime _cachedDragRuntime;
    private ActorWorldUI[] _cachedActorWorldUis;
    private bool _overlaysShown;
    private DiceCombatEnchantRuntimeUtility.SimpleEnchantPreview _simpleEnchantPreview;

    public SkillIconPreviewController(
        TurnManager turn,
        SelfCastDropZone selfCastZone,
        Camera uiCamera,
        Func<ScriptableObject> getSkillAsset,
        Func<SkillRuntime, int> getPreviewDieValue)
    {
        _turn = turn;
        _selfCastZone = selfCastZone;
        _uiCamera = uiCamera;
        _getSkillAsset = getSkillAsset;
        _getPreviewDieValue = getPreviewDieValue;
    }

    public void ShowResourcePreview(ScriptableObject asset)
    {
        if (_turn == null || asset == null || asset is SkillPassiveSO)
            return;
        if (!SkillUiMetadataUtility.TryGetSkillCosts(asset, out int focusCost, out int slotsRequired))
            return;

        CombatHUD hud = GetCachedHud();
        _simpleEnchantPreview = ComputeSimpleEnchantPreview(slotsRequired);
        if (hud != null && _turn.player != null)
        {
            bool isInvalid = _turn.player.focus < focusCost;
            hud.ShowFocusPreview(focusCost, _simpleEnchantPreview.focusGain, isInvalid);
        }

        if (_turn.diceRig != null)
        {
            BuildSpentDiceSet();
            _turn.diceRig.ShowConsumePreview(slotsRequired, CachedSpentSet);
        }

        ShowTargetOverlays(asset);
        ShowSelfEnchantGuardPreview();
        _resourcePreviewActive = true;
    }

    public void ClearResourcePreview()
    {
        if (!_resourcePreviewActive)
            return;

        _resourcePreviewActive = false;

        CombatHUD hud = GetCachedHud();
        if (hud != null)
            hud.ClearFocusPreview();

        if (_turn != null && _turn.diceRig != null)
        {
            BuildPendingUsedVisualSet();
            _turn.diceRig.ClearConsumePreview(CachedPendingUsedVisualSet);
        }

        ClearTargetOverlays();
    }

    public void Tick()
    {
        if (_resourcePreviewActive && _turn != null && _turn.diceRig != null)
        {
            BuildSpentDiceSet();
            _turn.diceRig.UpdateConsumePreviewVisuals(CachedSpentSet);
        }
    }

    public void UpdateTargetPreviewUnderCursor(PointerEventData eventData)
    {
        if (_turn == null || _turn.player == null)
            return;

        CombatActor hoveredActor = RaycastForActor(eventData);
        ActorWorldUI hoveredUi = FindActorWorldUi(hoveredActor);

        bool hoveringSelfZone = hoveredActor == null &&
                                DraggableSkillIcon.IsSelfTargetSkill(_getSkillAsset()) &&
                                _selfCastZone != null &&
                                _selfCastZone.ContainsScreenPoint(eventData.position, _uiCamera);

        if (hoveringSelfZone)
        {
            hoveredActor = _turn.player;
            hoveredUi = FindActorWorldUi(_turn.player);
        }

        if (hoveredUi == _currentPreviewTarget && hoveredUi != null)
            return;

        ClearTargetPreviewIfActive();
        if (hoveredUi == null || hoveredActor == null)
        {
            ShowSelfEnchantGuardPreview();
            return;
        }

        if (_cachedDragRuntime == null)
        {
            ScriptableObject asset = _getSkillAsset();
            if (asset != null && !(asset is SkillPassiveSO))
            {
                if (!_turn.TryGetPrototypeSkillTooltipRuntime(asset, out _cachedDragRuntime))
                {
                    if (asset is SkillDamageSO damageSkill)
                        _cachedDragRuntime = SkillRuntime.FromDamage(damageSkill);
                    else if (asset is SkillBuffDebuffSO buffSkill)
                        _cachedDragRuntime = SkillRuntime.FromBuffDebuff(buffSkill);
                }
            }
        }

        if (_cachedDragRuntime == null)
            return;

        if (!TurnManagerTargetingUtility.IsValidTargetForPendingSkill(_cachedDragRuntime, hoveredActor, _turn.player, _turn.party, _turn.enemy))
        {
            ShowSelfEnchantGuardPreview();
            return;
        }

        int dieValue = _getPreviewDieValue(_cachedDragRuntime);
        TargetPreviewBuilder.ActionPreviewBundle bundle =
            TargetPreviewBuilder.BuildActionBundle(_cachedDragRuntime, _turn.player, hoveredActor, dieValue, _turn.party, _turn.enemy);

        if (!bundle.valid)
            return;

        _currentPreviewTarget = hoveredUi;
        ShowActionPreviewBundle(bundle);
    }

    private CombatHUD GetCachedHud()
    {
#if UNITY_2023_1_OR_NEWER
        if (_cachedHud == null)
            _cachedHud = UnityEngine.Object.FindFirstObjectByType<CombatHUD>(FindObjectsInactive.Include);
#else
        if (_cachedHud == null)
            _cachedHud = UnityEngine.Object.FindObjectOfType<CombatHUD>(true);
#endif
        return _cachedHud;
    }

    private void BuildSpentDiceSet()
    {
        CachedSpentSet.Clear();
        if (_turn != null && _turn.SpentDiceThisTurn != null)
        {
            foreach (DiceSpinnerGeneric die in _turn.SpentDiceThisTurn)
                CachedSpentSet.Add(die);
        }
    }

    private void BuildPendingUsedVisualSet()
    {
        CachedPendingUsedVisualSet.Clear();
        if (_turn == null || _turn.SpentDiceThisTurn == null)
            return;

        foreach (DiceSpinnerGeneric die in _turn.SpentDiceThisTurn)
        {
            if (_turn.IsDiePendingUsedVisualThisTurn(die))
                CachedPendingUsedVisualSet.Add(die);
        }
    }

    private ActorWorldUI FindActorWorldUi(CombatActor actor)
    {
        if (actor == null || _cachedActorWorldUis == null)
            return null;

        foreach (ActorWorldUI ui in _cachedActorWorldUis)
        {
            if (ui != null && ui.actor == actor)
                return ui;
        }

        return null;
    }

    private void ClearTargetPreviewIfActive()
    {
        if (_cachedActorWorldUis != null)
        {
            foreach (ActorWorldUI ui in _cachedActorWorldUis)
            {
                if (ui != null)
                    ui.ClearTargetPreview();
            }
        }

        _currentPreviewTarget = null;

        if (_resourcePreviewActive && _turn != null && _turn.player != null)
        {
            ScriptableObject asset = _getSkillAsset();
            if (asset != null && SkillUiMetadataUtility.TryGetSkillCosts(asset, out int focusCost, out _))
            {
                CombatHUD hud = GetCachedHud();
                if (hud != null)
                    hud.ShowFocusPreview(focusCost, _simpleEnchantPreview.focusGain, _turn.player.focus < focusCost);
            }
        }
    }

    private void ShowActionPreviewBundle(TargetPreviewBuilder.ActionPreviewBundle bundle)
    {
        if (_cachedActorWorldUis == null || bundle.targetPreviews == null)
            return;

        foreach (KeyValuePair<CombatActor, TargetPreviewData> kvp in bundle.targetPreviews)
        {
            if (kvp.Key == null || !kvp.Value.valid)
                continue;

            foreach (ActorWorldUI ui in _cachedActorWorldUis)
            {
                if (ui != null && ui.actor == kvp.Key)
                {
                    ui.ShowTargetPreview(kvp.Value);
                    break;
                }
            }
        }

        CombatHUD hud = GetCachedHud();
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
        if (_simpleEnchantPreview.guardGain <= 0 || _turn == null || _turn.player == null)
            return;

        ActorWorldUI playerUi = FindActorWorldUi(_turn.player);
        if (playerUi == null)
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
        _currentPreviewTarget = playerUi;
    }

    private CombatActor RaycastForActor(PointerEventData eventData)
    {
        GameObject hitGo = eventData.pointerCurrentRaycast.gameObject;
        CombatActor actor = GetActorFromGameObject(hitGo);
        if (actor != null)
            return actor;

        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(eventData.position.x, eventData.position.y, -cam.transform.position.z));
            Collider2D hit = Physics2D.OverlapPoint(worldPos);
            if (hit != null)
                return GetActorFromGameObject(hit.gameObject);
        }

        return null;
    }

    private static CombatActor GetActorFromGameObject(GameObject go)
    {
        if (go == null)
            return null;

        TargetClickable2D clickable = go.GetComponent<TargetClickable2D>() ?? go.GetComponentInParent<TargetClickable2D>();
        if (clickable == null)
            return null;

        CombatActor actor = clickable.GetComponent<CombatActor>();
        if (actor == null)
            actor = clickable.GetComponentInParent<CombatActor>();
        return actor;
    }

    private void ShowTargetOverlays(ScriptableObject asset)
    {
        if (_turn == null)
            return;

        SkillRuntime runtime = null;
        if (!(asset is SkillPassiveSO))
            _turn.TryGetPrototypeSkillTooltipRuntime(asset, out runtime);

#if UNITY_2023_1_OR_NEWER
        _cachedActorWorldUis = UnityEngine.Object.FindObjectsByType<ActorWorldUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        _cachedActorWorldUis = UnityEngine.Object.FindObjectsOfType<ActorWorldUI>(true);
#endif

        foreach (ActorWorldUI ui in _cachedActorWorldUis)
        {
            if (ui == null || ui.actor == null)
                continue;

            CombatActor actor = ui.actor;
            bool isValid = false;
            if (!actor.IsDead)
                isValid = IsValidOverlayTarget(runtime, asset, actor);

            if (isValid)
                ui.ShowTargetOverlay(true);
            else
                ui.HideTargetOverlay();
        }

        _overlaysShown = true;
    }

    private bool IsValidOverlayTarget(SkillRuntime runtime, ScriptableObject asset, CombatActor actor)
    {
        if (runtime != null)
            return TurnManagerTargetingUtility.IsValidTargetForPendingSkill(runtime, actor, _turn.player, _turn.party, _turn.enemy);

        if (!SkillUiMetadataUtility.TryGetTargetRule(asset, out SkillTargetRule rule))
            return false;

        bool isEnemySide = SkillTargetRuleUtility.IsEnemySideTarget(rule);
        bool isSelfOnly = rule == SkillTargetRule.Self;
        bool isAllySide = SkillTargetRuleUtility.IsAllySideTarget(rule);

        if (isSelfOnly)
            return actor == _turn.player;
        if (isEnemySide)
            return actor != _turn.player && (_turn.party == null || actor.team != _turn.player.team);
        if (isAllySide)
            return actor == _turn.player || (_turn.party != null && actor.team == _turn.player.team);
        return false;
    }

    private void ClearTargetOverlays()
    {
        if (!_overlaysShown)
            return;

        _overlaysShown = false;
        ClearTargetPreviewIfActive();
        _cachedDragRuntime = null;

        if (_cachedActorWorldUis != null)
        {
            foreach (ActorWorldUI ui in _cachedActorWorldUis)
            {
                if (ui != null)
                    ui.HideTargetOverlay();
            }
        }

        _cachedActorWorldUis = null;
    }
}
