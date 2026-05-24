using UnityEngine;
using UnityEngine.UI;

public partial class ActorWorldUI
{
    private void RefreshIntent()
    {
        if (intentRoot == null)
            return;

        Sprite iconSprite = null;
        string valueText = string.Empty;
        bool showIntent = actor != null && !actor.isPlayer && TryGetIntentPresentation(out iconSprite, out valueText);
        intentRoot.gameObject.SetActive(showIntent);
        if (!showIntent)
            return;

        if (intentCanvasGroup != null)
            intentCanvasGroup.alpha = 1f;

        if (intentIcon != null)
        {
            intentIcon.sprite = iconSprite != null ? iconSprite : intentFallbackSprite;
            intentIcon.enabled = intentIcon.sprite != null;
            intentIcon.color = intentIcon.enabled ? Color.white : new Color(1f, 1f, 1f, 0f);
        }

        if (intentValueText != null)
            intentValueText.text = valueText;
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
            valueText = damage.ToString();
            return true;
        }

        // Guard/heal/status-only intents still show the intent icon, but no number.
        return true;
    }

    private static CombatActor FindPlayerIntentTarget()
    {
        CombatActor[] actors = FindObjectsOfType<CombatActor>(true);
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
        if (statusSlots == null)
            return;

        for (int i = 0; i < statusSlots.Length; i++)
        {
            StatusIconSlot slot = statusSlots[i];
            if (slot == null || slot.root == null)
                continue;

            bool show = i < _statusBuffer.Count;
            slot.root.gameObject.SetActive(show);
            if (!show)
                continue;

            StatusVisualData data = _statusBuffer[i];

            if (slot.background != null)
                slot.background.color = data.backgroundColor;

            if (slot.iconImage != null)
            {
                slot.iconImage.sprite = data.sprite;
                slot.iconImage.enabled = data.sprite != null;
                slot.iconImage.color = Color.white;
            }

            if (slot.shortLabelText != null)
            {
                string label = data.sprite == null ? data.shortLabel : string.Empty;
                slot.shortLabelText.text = label;
                slot.shortLabelText.gameObject.SetActive(!string.IsNullOrEmpty(label));
            }

            if (slot.valueText != null)
            {
                slot.valueText.text = data.valueText;
                slot.valueText.color = Color.white;
                slot.valueText.gameObject.SetActive(!string.IsNullOrEmpty(data.valueText));
            }
        }
    }
}
