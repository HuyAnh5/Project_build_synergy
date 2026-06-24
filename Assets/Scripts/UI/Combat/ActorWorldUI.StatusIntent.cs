using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class ActorWorldUI
{
    private readonly List<ActorWorldKeywordTooltipUI.TooltipContent> _tooltipContents = new List<ActorWorldKeywordTooltipUI.TooltipContent>(8);

    private enum WorldUiTooltipHotspotKind
    {
        None,
        Intent,
        Guard,
        Status
    }

    private readonly struct WorldUiTooltipHotspot
    {
        public readonly WorldUiTooltipHotspotKind kind;
        public readonly RectTransform target;
        public readonly int statusIndex;

        public WorldUiTooltipHotspot(WorldUiTooltipHotspotKind kind, RectTransform target, int statusIndex = -1)
        {
            this.kind = kind;
            this.target = target;
            this.statusIndex = statusIndex;
        }
    }

    private void RefreshIntent()
    {
        if (intentRoot == null)
            return;

        int intentSignature = BuildRuntimeIntentSignature();
        if (_lastRuntimeIntentSignature == intentSignature)
            return;

        _lastRuntimeIntentSignature = intentSignature;

        Sprite iconSprite = null;
        string valueText = string.Empty;
        bool showIntent = actor != null && !actor.isPlayer && TryGetIntentPresentation(out iconSprite, out valueText);
        CombatUiDirtySetUtility.SetActiveIfChanged(intentRoot.gameObject, showIntent);
        if (!showIntent)
            return;

        if (intentCanvasGroup != null)
            intentCanvasGroup.alpha = 1f;

        if (intentIcon != null)
        {
            Sprite nextSprite = iconSprite != null ? iconSprite : intentFallbackSprite;
            if (intentIcon.sprite != nextSprite)
                intentIcon.sprite = nextSprite;

            bool iconEnabled = nextSprite != null;
            if (intentIcon.enabled != iconEnabled)
                intentIcon.enabled = iconEnabled;

            CombatUiDirtySetUtility.SetColorIfChanged(intentIcon, iconEnabled ? Color.white : new Color(1f, 1f, 1f, 0f));
        }

        CombatUiDirtySetUtility.SetTextIfChanged(intentValueText, valueText);
    }

    private int BuildRuntimeIntentSignature()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + (actor != null ? actor.GetInstanceID() : 0);
            hash = hash * 31 + (actor != null && actor.isPlayer ? 1 : 0);
            hash = hash * 31 + BuildRuntimeStatusSignature(actor != null ? actor.status : null);

            if (_brain == null || _brain.definition == null || !_brain.CurrentIntent.hasIntent)
                return hash;

            int moveIndex = _brain.CurrentIntent.moveIndex;
            hash = hash * 31 + moveIndex;
            if (moveIndex >= 0 && moveIndex < _brain.definition.moves.Count)
            {
                EnemyDefinitionSO.EnemyMoveSlot move = _brain.definition.moves[moveIndex];
                hash = hash * 31 + (move?.damageSkill != null ? move.damageSkill.GetInstanceID() : 0);
                hash = hash * 31 + (move?.buffDebuffSkill != null ? move.buffDebuffSkill.GetInstanceID() : 0);
            }

            CombatActor playerTarget = FindPlayerIntentTarget();
            if (playerTarget != null)
            {
                hash = hash * 31 + playerTarget.GetInstanceID();
                hash = hash * 31 + playerTarget.hp;
                hash = hash * 31 + playerTarget.maxHP;
                hash = hash * 31 + playerTarget.guardPool;
                hash = hash * 31 + BuildRuntimeStatusSignature(playerTarget.status);
            }

            return hash;
        }
    }

    private bool TryGetIntentPresentation(out Sprite iconSprite, out string valueText)
    {
        iconSprite = null;
        valueText = string.Empty;

        if (_brain == null || !_brain.CurrentIntent.hasIntent || _brain.definition == null)
            return false;

        int moveIndex = _brain.CurrentIntent.moveIndex;
        if (moveIndex < 0 || moveIndex >= _brain.definition.moves.Count)
            return false;

        EnemyDefinitionSO.EnemyMoveSlot move = _brain.definition.moves[moveIndex];
        if (move == null)
            return false;

        if (move.damageSkill != null)
        {
            iconSprite = move.damageSkill.icon;

            SkillRuntime runtime = SkillRuntime.FromDamage(move.damageSkill);
            if (TryGetNewGameplayIntentValue(runtime, out string resolvedValueText))
            {
                valueText = resolvedValueText;
            }
            else if (runtime.kind == SkillKind.Attack)
            {
                int damage = Mathf.Max(0, runtime.CalculateDamage(0));
                valueText = damage > 0 ? damage.ToString() : string.Empty;
            }
        }
        else if (move.buffDebuffSkill != null)
        {
            iconSprite = move.buffDebuffSkill.icon;
        }

        if (iconSprite == null)
            iconSprite = intentFallbackSprite;

        return iconSprite != null || !string.IsNullOrEmpty(valueText);
    }

    private bool TryGetNewGameplayIntentValue(SkillRuntime runtime, out string valueText)
    {
        valueText = string.Empty;

        SkillDamageSO sourceSkill = SkillGameplayResolver.GetSourceSkill(runtime);
        if (sourceSkill == null || sourceSkill.gameplay == null ||
            !sourceSkill.gameplay.useNewGameplayPipeline)
            return false;

        CombatActor previewTarget = FindPlayerIntentTarget();
        SkillResolvedResult result = SkillGameplayResolver.Resolve(runtime, actor, previewTarget);
        if (result == null || !result.canCast)
            return true;

        int damage = Mathf.Max(0, result.damageDelta);
        if (damage > 0)
        {
            int executionCount = Mathf.Max(1, result.executionCount);
            valueText = executionCount > 1
                ? $"{damage} x {executionCount}"
                : damage.ToString();
            return true;
        }

        // Guard/heal/status-only intents still show the intent icon, but no number.
        return true;
    }

    private static CombatActor FindPlayerIntentTarget()
    {
        CombatActor[] actors = CombatActorRegistry.GetAllSnapshot(includeInactive: true);
        for (int i = 0; i < actors.Length; i++)
        {
            CombatActor candidate = actors[i];
            if (candidate != null && candidate.isPlayer && !candidate.IsDead)
                return candidate;
        }

        return null;
    }

    private void RefreshStatusIcons(StatusController status)
    {
        BuildStatusBuffer(status);
        ApplyStatusBuffer();
    }

    private static int BuildRuntimeStatusSignature(StatusController status)
    {
        if (status == null)
            return 0;

        unchecked
        {
            int hash = 17;
            hash = hash * 31 + (status.frozen ? 1 : 0);
            hash = hash * 31 + status.chilledTurns;
            hash = hash * 31 + (status.marked ? 1 : 0);
            hash = hash * 31 + status.burnStacks;
            hash = hash * 31 + status.bleedStacks;
            hash = hash * 31 + (status.staggered ? 1 : 0);
            if (status.HasAilment(out AilmentType ailment, out int turnsLeft))
            {
                hash = hash * 31 + 1;
                hash = hash * 31 + (int)ailment;
                hash = hash * 31 + turnsLeft;
            }

            return hash;
        }
    }

    private void BuildStatusBuffer(StatusController status)
    {
        _statusBuffer.Clear();
        if (status == null)
            return;

        if (status.frozen)
            AddStatusVisual(CombatUiStatusIconKind.Freeze, "FR", string.Empty, new Color(0.4f, 0.78f, 1f, 0.96f));
        if (status.chilledTurns > 0)
            AddStatusVisual(CombatUiStatusIconKind.Chilled, "CH", status.chilledTurns.ToString(), new Color(0.58f, 0.9f, 1f, 0.96f));
        if (status.marked)
            AddStatusVisual(CombatUiStatusIconKind.Mark, "MK", string.Empty, new Color(1f, 0.88f, 0.28f, 0.96f));
        if (status.burnStacks > 0)
            AddStatusVisual(CombatUiStatusIconKind.Burn, "BU", status.burnStacks.ToString(), new Color(1f, 0.42f, 0.22f, 0.96f));
        if (status.bleedStacks > 0)
            AddStatusVisual(CombatUiStatusIconKind.Bleed, "BL", status.bleedStacks.ToString(), new Color(0.82f, 0.14f, 0.2f, 0.96f));
        if (status.HasAilment(out AilmentType ailment, out int turnsLeft))
            AddStatusVisual(CombatUiStatusIconKind.Ailment, GetAilmentShortLabel(ailment), Mathf.Max(1, turnsLeft).ToString(), new Color(0.72f, 0.5f, 1f, 0.96f));
    }

    private void RefreshEditorPreview()
    {
        bool showPreview = !Application.isPlaying && showEditorPreview;

        if (previewDummyRoot != null)
            previewDummyRoot.gameObject.SetActive(showPreview);

        if (!showPreview)
            return;

        RefreshHpAndGuard(previewHp, previewMaxHp, previewGuard, previewStaggered);
        BuildPreviewStatusBuffer();
        ApplyStatusBuffer();

        if (intentRoot != null)
            intentRoot.gameObject.SetActive(previewShowIntent);

        if (intentCanvasGroup != null)
            intentCanvasGroup.alpha = 1f;

        if (intentIcon != null)
        {
            intentIcon.sprite = previewIntentSprite != null ? previewIntentSprite : intentFallbackSprite;
            intentIcon.enabled = intentIcon.sprite != null;
            intentIcon.color = intentIcon.enabled ? Color.white : new Color(1f, 1f, 1f, 0f);
        }

        if (intentValueText != null)
            intentValueText.text = previewIntentValue > 0 ? previewIntentValue.ToString() : string.Empty;
    }

    private void BuildPreviewStatusBuffer()
    {
        _statusBuffer.Clear();

        if (previewFrozen)
            AddStatusVisual(CombatUiStatusIconKind.Freeze, "FR", string.Empty, new Color(0.4f, 0.78f, 1f, 0.96f));
        if (previewChilledTurns > 0)
            AddStatusVisual(CombatUiStatusIconKind.Chilled, "CH", previewChilledTurns.ToString(), new Color(0.58f, 0.9f, 1f, 0.96f));
        if (previewMarked)
            AddStatusVisual(CombatUiStatusIconKind.Mark, "MK", string.Empty, new Color(1f, 0.88f, 0.28f, 0.96f));
        if (previewBurnStacks > 0)
            AddStatusVisual(CombatUiStatusIconKind.Burn, "BU", previewBurnStacks.ToString(), new Color(1f, 0.42f, 0.22f, 0.96f));
        if (previewBleedStacks > 0)
            AddStatusVisual(CombatUiStatusIconKind.Bleed, "BL", previewBleedStacks.ToString(), new Color(0.82f, 0.14f, 0.2f, 0.96f));
        if (previewHasAilment)
            AddStatusVisual(CombatUiStatusIconKind.Ailment, GetAilmentShortLabel(previewAilment), Mathf.Max(1, previewAilmentTurns).ToString(), new Color(0.72f, 0.5f, 1f, 0.96f));
    }

    private void ApplyStatusBuffer()
    {
        IList<StatusIconSlot> slots = GetResolvedStatusSlots(_statusBuffer.Count);
        CombatStatusRowRenderer.Apply(
            slots,
            statusSlotTemplateRoot,
            _statusBuffer.Count,
            index => _statusBuffer[index],
            data => data.sprite,
            data => data.shortLabel,
            data => data.valueText,
            data => data.backgroundColor);
    }

    private void RefreshWorldUiTooltips()
    {
        if (!Application.isPlaying || actor == null || worldCanvas == null)
        {
            ActorWorldKeywordTooltipUI.Hide();
            return;
        }

        if (!TryGetHoveredWorldUiHotspot(out WorldUiTooltipHotspot hotspot))
        {
            if (ActorWorldKeywordTooltipUI.IsShowingFor(this))
                ActorWorldKeywordTooltipUI.Hide();
            return;
        }

        _tooltipContents.Clear();
        bool showAll = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        if (showAll)
            BuildAllTooltipContents(_tooltipContents);
        else
            BuildHotspotTooltipContents(hotspot, _tooltipContents);

        if (_tooltipContents.Count <= 0)
        {
            if (ActorWorldKeywordTooltipUI.IsShowingFor(this))
                ActorWorldKeywordTooltipUI.Hide();
            return;
        }

        Camera targetCamera = GetWorldUiEventCamera();
        RectTransform tooltipAnchor = tooltipAnchorRoot != null ? tooltipAnchorRoot : hotspot.target;
        RectTransform tooltipBottomLimit = tooltipBottomLimitRoot;
        RectTransform hoverTarget = hotspot.target != null ? hotspot.target : worldCanvasRoot;
        ActorWorldKeywordTooltipUI.Show(
            worldCanvas,
            tooltipAnchor,
            tooltipBottomLimit,
            hoverTarget,
            targetCamera,
            _tooltipContents,
            this,
            tooltipSpawnDirection == WorldUiTooltipSpawnDirection.Right);
    }

    private bool TryGetHoveredWorldUiHotspot(out WorldUiTooltipHotspot hotspot)
    {
        hotspot = default;
        Vector2 screenPoint = Input.mousePosition;
        Camera eventCamera = GetWorldUiEventCamera();

        if (intentRoot != null &&
            intentRoot.gameObject.activeInHierarchy &&
            RectTransformUtility.RectangleContainsScreenPoint(intentRoot, screenPoint, eventCamera))
        {
            hotspot = new WorldUiTooltipHotspot(WorldUiTooltipHotspotKind.Intent, intentRoot);
            return true;
        }

        if (guardRoot != null &&
            guardRoot.gameObject.activeInHierarchy &&
            RectTransformUtility.RectangleContainsScreenPoint(guardRoot, screenPoint, eventCamera))
        {
            hotspot = new WorldUiTooltipHotspot(WorldUiTooltipHotspotKind.Guard, guardRoot);
            return true;
        }

        IList<StatusIconSlot> slots = GetResolvedStatusSlots(_statusBuffer.Count);
        if (slots != null)
        {
            for (int i = 0; i < _statusBuffer.Count && i < slots.Count; i++)
            {
                StatusIconSlot slot = slots[i];
                if (slot?.root == null || !slot.root.gameObject.activeInHierarchy)
                    continue;

                if (!RectTransformUtility.RectangleContainsScreenPoint(slot.root, screenPoint, eventCamera))
                    continue;

                hotspot = new WorldUiTooltipHotspot(WorldUiTooltipHotspotKind.Status, slot.root, i);
                return true;
            }
        }

        return false;
    }

    private Camera GetWorldUiEventCamera()
    {
        if (worldCanvas == null || worldCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        if (worldCanvas.worldCamera != null)
            return worldCanvas.worldCamera;

        if (Camera.main != null)
            return Camera.main;

        return Camera.current;
    }

    private void BuildHotspotTooltipContents(WorldUiTooltipHotspot hotspot, List<ActorWorldKeywordTooltipUI.TooltipContent> contents)
    {
        switch (hotspot.kind)
        {
            case WorldUiTooltipHotspotKind.Intent:
                if (TryBuildIntentTooltip(out ActorWorldKeywordTooltipUI.TooltipContent intent))
                    contents.Add(intent);
                break;
            case WorldUiTooltipHotspotKind.Guard:
                if (TryBuildGuardTooltip(out ActorWorldKeywordTooltipUI.TooltipContent guard))
                    contents.Add(guard);
                break;
            case WorldUiTooltipHotspotKind.Status:
                if (TryBuildStatusTooltip(hotspot.statusIndex, out ActorWorldKeywordTooltipUI.TooltipContent status))
                    contents.Add(status);
                break;
        }
    }

    private void BuildAllTooltipContents(List<ActorWorldKeywordTooltipUI.TooltipContent> contents)
    {
        if (TryBuildIntentTooltip(out ActorWorldKeywordTooltipUI.TooltipContent intent))
            contents.Add(intent);

        if (TryBuildGuardTooltip(out ActorWorldKeywordTooltipUI.TooltipContent guard))
            contents.Add(guard);

        if (TryBuildStaggerTooltip(out ActorWorldKeywordTooltipUI.TooltipContent stagger))
            contents.Add(stagger);

        for (int i = 0; i < _statusBuffer.Count; i++)
        {
            if (TryBuildStatusTooltip(i, out ActorWorldKeywordTooltipUI.TooltipContent status))
                contents.Add(status);
        }
    }

    private bool TryBuildIntentTooltip(out ActorWorldKeywordTooltipUI.TooltipContent content)
    {
        content = default;
        if (_brain == null || !_brain.CurrentIntent.hasIntent || _brain.definition == null)
            return false;

        int moveIndex = _brain.CurrentIntent.moveIndex;
        if (moveIndex < 0 || moveIndex >= _brain.definition.moves.Count)
            return false;

        EnemyDefinitionSO.EnemyMoveSlot move = _brain.definition.moves[moveIndex];
        if (move == null)
            return false;

        string title = "Intent";
        string body = string.Empty;
        Sprite icon = intentIcon != null && intentIcon.enabled ? intentIcon.sprite : intentFallbackSprite;

        if (move.damageSkill != null)
        {
            SkillRuntime runtime = SkillRuntime.FromDamage(move.damageSkill);
            title = string.IsNullOrWhiteSpace(move.damageSkill.displayName) ? move.damageSkill.name : move.damageSkill.displayName;
            body = !string.IsNullOrWhiteSpace(move.damageSkill.description)
                ? move.damageSkill.description.Trim()
                : SkillTooltipFormatter.BuildContent(move.damageSkill, runtime).effectText;
        }
        else if (move.buffDebuffSkill != null)
        {
            title = string.IsNullOrWhiteSpace(move.buffDebuffSkill.displayName) ? move.buffDebuffSkill.name : move.buffDebuffSkill.displayName;
            body = !string.IsNullOrWhiteSpace(move.buffDebuffSkill.description)
                ? move.buffDebuffSkill.description.Trim()
                : SkillTooltipFormatter.BuildContent(move.buffDebuffSkill).effectText;
        }

        if (string.IsNullOrWhiteSpace(body))
            body = "This is the enemy's next planned action.";

        content = new ActorWorldKeywordTooltipUI.TooltipContent(title, body, icon);
        return true;
    }

    private bool TryBuildGuardTooltip(out ActorWorldKeywordTooltipUI.TooltipContent content)
    {
        content = default;
        int guard = actor != null ? Mathf.Max(0, actor.guardPool) : 0;
        if (guard <= 0)
            return false;

        string body = $"Current Guard: {guard}\n\nBlocks incoming damage before HP. If Guard is broken, the target becomes Staggered.";
        content = new ActorWorldKeywordTooltipUI.TooltipContent("Guard", body, guardIcon != null ? guardIcon.sprite : null);
        return true;
    }

    private bool TryBuildStaggerTooltip(out ActorWorldKeywordTooltipUI.TooltipContent content)
    {
        content = default;
        if (actor == null || actor.status == null || !actor.status.staggered)
            return false;

        content = new ActorWorldKeywordTooltipUI.TooltipContent(
            "Staggered",
            "This target's Guard is broken and it is currently exposed.",
            null);
        return true;
    }

    private bool TryBuildStatusTooltip(int statusIndex, out ActorWorldKeywordTooltipUI.TooltipContent content)
    {
        content = default;
        if (statusIndex < 0 || statusIndex >= _statusBuffer.Count)
            return false;

        StatusVisualData data = _statusBuffer[statusIndex];
        string title = data.shortLabel;
        string body = string.Empty;
        Sprite icon = data.sprite;

        switch (data.shortLabel)
        {
            case "FR":
                title = "Freeze";
                body = "Frozen targets cannot act until the freeze is removed.";
                break;
            case "CH":
                title = "Chilled";
                body = $"Chilled for {GetTooltipValueText(data.valueText)} turn(s).";
                break;
            case "MK":
                title = "Mark";
                body = "Marked targets are primed for mark payoff effects.";
                break;
            case "BU":
                title = "Burn";
                body = $"Burn stacks: {GetTooltipValueText(data.valueText)}.";
                break;
            case "BL":
                title = "Bleed";
                body = $"Bleed stacks: {GetTooltipValueText(data.valueText)}.";
                break;
            default:
                if (actor != null && actor.status != null && actor.status.HasAilment(out AilmentType ailment, out int turnsLeft))
                {
                    title = ailment.ToString();
                    body = $"{ailment} for {Mathf.Max(1, turnsLeft)} turn(s).";
                }
                break;
        }

        if (string.IsNullOrWhiteSpace(body))
            return false;

        content = new ActorWorldKeywordTooltipUI.TooltipContent(title, body, icon);
        return true;
    }

    private static string GetTooltipValueText(string valueText)
    {
        return string.IsNullOrWhiteSpace(valueText) ? "0" : valueText.Trim();
    }
}

internal static class CombatStatusRowRenderer
{
    public static void Apply<TVisual>(
        IList<ActorWorldUI.StatusIconSlot> slots,
        RectTransform templateRoot,
        int visualCount,
        System.Func<int, TVisual> getVisual,
        System.Func<TVisual, Sprite> getSprite,
        System.Func<TVisual, string> getShortLabel,
        System.Func<TVisual, string> getValueText,
        System.Func<TVisual, Color> getBackgroundColor)
    {
        if (slots == null)
            return;

        if (templateRoot != null)
            templateRoot.gameObject.SetActive(false);

        for (int i = 0; i < slots.Count; i++)
        {
            ActorWorldUI.StatusIconSlot slot = slots[i];
            if (slot == null || slot.root == null)
                continue;

            bool show = i < visualCount;
            slot.root.gameObject.SetActive(show);
            if (!show)
                continue;

            TVisual data = getVisual(i);
            Sprite sprite = getSprite(data);

            if (slot.background != null)
                slot.background.color = getBackgroundColor(data);

            if (slot.iconImage != null)
            {
                slot.iconImage.sprite = sprite;
                slot.iconImage.enabled = sprite != null;
                slot.iconImage.color = Color.white;
            }

            if (slot.shortLabelText != null)
            {
                string label = sprite == null ? getShortLabel(data) : string.Empty;
                slot.shortLabelText.text = label;
                slot.shortLabelText.gameObject.SetActive(!string.IsNullOrEmpty(label));
            }

            if (slot.valueText != null)
            {
                string valueText = getValueText(data);
                slot.valueText.text = valueText;
                slot.valueText.color = Color.white;
                slot.valueText.gameObject.SetActive(!string.IsNullOrEmpty(valueText));
            }
        }
    }
}
