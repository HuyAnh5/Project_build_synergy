using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

internal sealed partial class SkillIconPreviewController
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
    private TurnManager.PreviewPaymentPlan _previewPlan;

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
        _previewPlan = default;
        _turn.TryGetPrototypePreviewPaymentPlan(asset, out _previewPlan);
        _simpleEnchantPreview = ComputeSimpleEnchantPreview(slotsRequired);
        if (hud != null && _turn.player != null)
        {
            bool isInvalid = _turn.player.focus < focusCost;
            hud.ShowFocusPreview(focusCost, _simpleEnchantPreview.focusGain, isInvalid);
        }

        if (_turn.diceRig != null)
        {
            BuildSpentDiceSet();
            _turn.diceRig.ShowConsumePreview(slotsRequired, CachedSpentSet, _previewPlan.valid ? _previewPlan.selectedMask : -1);
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

    public bool ReleaseResourcePreviewOwnership()
    {
        if (!_resourcePreviewActive)
            return false;

        _resourcePreviewActive = false;
        ClearTargetOverlays();
        return true;
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

        bool hoveringSelfZone = CanPreviewOnSelf(_getSkillAsset()) &&
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
            TargetingArrowUI.ClearWorldTarget();
            return;
        }

        if (_cachedDragRuntime == null)
        {
            ScriptableObject asset = _getSkillAsset();
            if (asset != null && !(asset is SkillPassiveSO))
            {
                if (_previewPlan.valid && _previewPlan.runtime != null)
                    _cachedDragRuntime = _previewPlan.runtime;
                else if (!_turn.TryGetPrototypeSkillTooltipRuntime(asset, out _cachedDragRuntime))
                {
                    if (asset is SkillDamageSO damageSkill)
                        _cachedDragRuntime = SkillRuntime.FromDamage(damageSkill);
                    else if (asset is SkillBuffDebuffSO buffSkill)
                        _cachedDragRuntime = SkillRuntime.FromBuffDebuff(buffSkill);
                }
            }
        }

        if (_cachedDragRuntime == null)
        {
            TargetingArrowUI.ClearWorldTarget();
            return;
        }

        if (!TurnManagerTargetingUtility.IsValidTargetForPendingSkill(_cachedDragRuntime, hoveredActor, _turn.player, _turn.party, _turn.enemy))
        {
            TargetingArrowUI.ClearWorldTarget();
            return;
        }

        TargetingArrowUI.SetWorldTarget(hoveredActor.transform);

        int rawDieValue = _previewPlan.valid ? _previewPlan.resolvedDieValue : _getPreviewDieValue(_cachedDragRuntime);
        int guardLocalIndex = _previewPlan.valid ? Mathf.Clamp(_previewPlan.anchor0 - _previewPlan.start0, 0, 2) : 0;
        int dieValue = CombatGuardPreviewUtility.ResolveGuardPreviewDieValue(_cachedDragRuntime, rawDieValue, guardLocalIndex);
        int repeatPreviewCount = (_previewPlan.valid ? Mathf.Max(0, _previewPlan.repeatCount) : 0) + GetReadyStatusRepeatCount(_turn.player);
        SkillDamageSO selectedDamageSkill = _getSkillAsset() as SkillDamageSO;
        SkillDamageSO sourceSkill = selectedDamageSkill != null ? selectedDamageSkill : SkillGameplayResolver.GetSourceSkill(_cachedDragRuntime);
        TargetPreviewBuilder.ActionPreviewBundle bundle =
            CombatPreviewBundleUtility.BuildActionBundleWithSelfGuard(
                _cachedDragRuntime,
                sourceSkill,
                _turn.player,
                hoveredActor,
                _turn.player,
                _turn.party,
                _turn.enemy,
                dieValue,
                guardLocalIndex,
                repeatPreviewCount,
                _simpleEnchantPreview.guardGain);

        if (!bundle.valid)
            return;

        _currentPreviewTarget = hoveredUi;
        ShowActionPreviewBundle(bundle);
    }

    private CombatHUD GetCachedHud()
    {
        if (_cachedHud == null)
            _cachedHud = CombatHudRegistry.Get();
        return _cachedHud;
    }

    private bool CanPreviewOnSelf(ScriptableObject asset)
    {
        if (_turn == null || _turn.player == null || asset == null || asset is SkillPassiveSO)
            return false;

        if (DraggableSkillIcon.IsSelfTargetSkill(asset))
            return true;

        if (!_turn.TryGetPrototypeSkillTooltipRuntime(asset, out SkillRuntime runtime) || runtime == null)
        {
            if (asset is SkillDamageSO damageSkill)
                runtime = SkillRuntime.FromDamage(damageSkill);
            else if (asset is SkillBuffDebuffSO buffSkill)
                runtime = SkillRuntime.FromBuffDebuff(buffSkill);
        }

        return runtime != null &&
               TurnManagerTargetingUtility.IsValidTargetForPendingSkill(runtime, _turn.player, _turn.player, _turn.party, _turn.enemy);
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
        return CombatTargetPreviewPresenter.FindWorldUi(actor, _cachedActorWorldUis);
    }

    private void ClearTargetPreviewIfActive()
    {
        CombatTargetPreviewPresenter.ClearAll(_cachedActorWorldUis, GetCachedHud());

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

            ShowSelfEnchantGuardPreview();
        }
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

    private static int GetReadyStatusRepeatCount(CombatActor actor)
        => actor != null && actor.status != null ? actor.status.PeekRepeatFirstSkillReady() : 0;

    private void ShowTargetOverlays(ScriptableObject asset)
    {
        if (_turn == null)
            return;

        SkillRuntime runtime = null;
        if (!(asset is SkillPassiveSO))
            _turn.TryGetPrototypeSkillTooltipRuntime(asset, out runtime);

        _cachedActorWorldUis = ActorWorldUiRegistry.GetAllSnapshot();

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
