using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public partial class DraggableSkillIcon
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        _pointerInside = true;

        if (UiDragState.IsDragging)
            return;

        ScriptableObject asset = GetSkillAsset();
        if (asset == null)
            return;

        SkillTooltipUI.Show(this);
        ShowResourcePreview(asset);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _pointerInside = false;

        if (UiDragState.IsDragging)
            return;

        if (IsPointerInsidePreviewHoverContainer(eventData))
            return;

        SkillTooltipUI.HideCurrentUnlessPointerOverTooltip(eventData != null ? eventData.pointerCurrentRaycast.gameObject : null);

        DraggableSkillIcon selected = UiDragState.SelectedSkill;

        // Náº¿u báº£n thÃ¢n Ä‘ang Ä‘Æ°á»£c chá»n, KHÃ”NG BAO GIá»œ clear resource preview khi chuá»™t rá»i Ä‘i
        if (selected == this)
            return;

        // Náº¿u cÃ³ skill khÃ¡c Ä‘ang Ä‘Æ°á»£c chá»n, khÃ´i phá»¥c preview cá»§a nÃ³
        if (selected != null)
        {
            ReleaseResourcePreviewOwnership();
            ScriptableObject selectedAsset = selected.GetSkillAsset();
            if (selectedAsset != null)
                selected.ShowResourcePreview(selectedAsset);
            return;
        }

        // Náº¿u khÃ´ng cÃ³ gÃ¬ Ä‘Æ°á»£c chá»n, clear bÃ¬nh thÆ°á»ng
        ClearResourcePreview();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!turn || !turn.CanInteractWithSkills) return;
        var a = GetSkillAsset();
        if (a == null) return;
        if (a is SkillPassiveSO passive)
        {
            HandleTesterPassivePointerClick(passive);
            return;
        }

        if (!CanDragCurrentSkill())
        {
            RejectActionFeedback();
            return;
        }

        // Toggle select/deselect
        if (_selected)
        {
            UiDragState.DeselectSkill();
        }
        else
        {
            UiDragState.SelectSkill(this);
        }
    }

    public void OnSelected()
    {
        _selected = true;
        StartBlinkCoroutine();
        var a = GetSkillAsset();
        if (a != null)
            ShowResourcePreview(a);
    }

    public void OnDeselected()
    {
        _selected = false;
        StopBlinkCoroutine();
        ClearResourcePreview();
        if (_img != null)
            _img.color = Color.white;
    }

    public static void PulseAffectedSkillIconsOnce(TurnManager turn)
    {
        if (turn == null)
            return;

        int frame = Time.frameCount;
        if (_lastAffectedPulseFrame == frame)
            return;

        DraggableSkillIcon[] icons = DraggableSkillIconRegistry.GetAllSnapshot();
        bool pulsedAny = false;
        for (int i = 0; i < icons.Length; i++)
        {
            DraggableSkillIcon icon = icons[i];
            if (icon == null || !icon.gameObject.activeInHierarchy)
                continue;

            ScriptableObject asset = icon.GetSkillAsset();
            if (asset == null || asset is SkillPassiveSO)
                continue;

            if (!turn.TryGetPrototypeSkillTooltipRuntime(asset, out SkillRuntime runtime) || runtime == null)
                continue;

            if (!IsSkillAffectedByDice(runtime))
                continue;

            icon.PlayTransientAffectedAuraPulse();
            pulsedAny = true;
        }

        if (pulsedAny)
            _lastAffectedPulseFrame = frame;
    }

    public static void PulseSkillAssetIcons(ScriptableObject asset)
    {
        if (asset == null)
            return;

        DraggableSkillIcon[] icons = DraggableSkillIconRegistry.GetAllSnapshot();
        for (int i = 0; i < icons.Length; i++)
        {
            DraggableSkillIcon icon = icons[i];
            if (icon == null || !icon.gameObject.activeInHierarchy)
                continue;

            if (icon.GetSkillAsset() == asset)
                icon.PlayTransientAffectedAuraPulse();
        }
    }

    private static bool IsSkillAffectedByDice(SkillRuntime runtime)
    {
        if (runtime == null)
            return false;

        if (runtime.kind == SkillKind.Guard || runtime.guardValueMode == BaseEffectValueMode.X)
            return true;

        if (SkillOutputValueUtility.GetTotalActionAddedValue(runtime) > 0)
            return true;

        if (runtime.localFailPenaltyAny)
            return true;

        if (runtime.conditionMet)
        {
            if (runtime.conditionalOutcomeEnabled)
                return true;

            if (runtime.applyBurn || runtime.applyBleed || runtime.applyFreeze || runtime.applyMark)
                return true;
        }

        return false;
    }

    public void PlayTransientAffectedAuraPulse()
    {
        if (!enableActiveAura)
            return;

        EnsureActiveAuraUi();
        if (_activeAuraRim == null)
            return;

        _transientAffectedAuraSequence?.Kill();
        _transientAffectedAuraSequence = null;
        _transientAffectedAuraRunning = true;

        StopActiveAuraTweens();
        int waveCount = GetActiveAuraWaveCount();
        EnsureActiveAuraWavePool(waveCount);
        for (int i = 0; i < _activeAuraWaves.Count; i++)
            SetAuraLayerVisible(_activeAuraWaves[i], i < waveCount);
        SetAuraLayerVisible(_activeAuraRim, true);

        float duration = Mathf.Max(0.08f, transientAffectedAuraDuration);
        float halfDuration = duration * 0.5f;

        Sequence seq = DOTween.Sequence().SetUpdate(true);
        seq.AppendCallback(() =>
        {
            ApplyAuraRim(_activeAuraRim, transientAffectedAuraBrightColor, 1f);
            for (int i = 0; i < waveCount && i < _activeAuraWaves.Count; i++)
            {
                Image wave = _activeAuraWaves[i];
                LayoutAuraLayer(wave, 0f);
                wave.color = transientAffectedAuraWaveColor;
            }
        });
        seq.Append(DOVirtual.Float(0f, 1f, halfDuration, t =>
        {
            for (int i = 0; i < waveCount && i < _activeAuraWaves.Count; i++)
            {
                Image wave = _activeAuraWaves[i];
                if (wave == null)
                    continue;

                float expand = Mathf.Lerp(0f, transientAffectedAuraWaveSize, t);
                LayoutAuraLayer(wave, expand);
                Color c = transientAffectedAuraWaveColor;
                c.a *= (1f - t);
                wave.color = c;
            }
        }).SetEase(Ease.OutQuad));
        seq.Append(DOVirtual.Float(1f, 0f, halfDuration, t =>
        {
            if (_activeAuraRim != null)
                ApplyAuraRim(_activeAuraRim, transientAffectedAuraBrightColor, t);
        }).SetEase(Ease.OutQuad));
        seq.OnComplete(() =>
        {
            _transientAffectedAuraRunning = false;
            _transientAffectedAuraSequence = null;
            ApplyActiveAuraVisibility();
        });
        _transientAffectedAuraSequence = seq;
    }

    private void StartBlinkCoroutine()
    {
        StopBlinkCoroutine();
        _blinkCoroutine = StartCoroutine(BlinkRoutine());
    }

    private void StopBlinkCoroutine()
    {
        if (_blinkCoroutine != null)
        {
            StopCoroutine(_blinkCoroutine);
            _blinkCoroutine = null;
        }
    }

    private System.Collections.IEnumerator BlinkRoutine()
    {
        while (_selected)
        {
            float t = Mathf.PingPong(UnityEngine.Time.time * 3f, 1f);
            if (_img != null)
                _img.color = Color.Lerp(SelectedBlinkColorB, SelectedBlinkColorA, t);
            yield return null;
        }
    }

    private void RejectActionFeedback()
    {
        transform.DOKill(complete: true);
        transform.DOShakePosition(0.3f, new Vector3(10f, 0, 0), 30, 90f, false, true).SetUpdate(true);
    }
}
