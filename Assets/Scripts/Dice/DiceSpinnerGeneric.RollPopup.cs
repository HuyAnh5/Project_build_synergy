using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

public partial class DiceSpinnerGeneric
{
    private Color GetCritPopupColor()
    {
        Color color = critOutlineColor;
        color.a = 1f;
        return color;
    }

    private Color GetFailPopupColor()
    {
        Color color = failOutlineColor;
        color.a = 1f;
        return color;
    }

    private readonly struct RollPopupStep
    {
        public readonly string Text;
        public readonly Color Color;

        public RollPopupStep(string text, Color color)
        {
            Text = text;
            Color = color;
        }
    }

    private void PlayRollStatePopupIfNeeded()
    {
        if (_previewSandboxMode || !Application.isPlaying || !animateCritFailPopup)
            return;

        List<RollPopupStep> steps = BuildRollPopupSteps();
        if (steps.Count == 0)
            return;

        TMP_Text popup = GetOrCreateRollStatePopupInstance();
        if (popup == null)
            return;

        _rollStatePopupTween?.Kill();

        PositionRollStatePopup(popup);
        popup.gameObject.SetActive(true);

        Color baseColor = rollStatePopupColor;
        baseColor.a = 1f;
        popup.color = baseColor;

        Sequence seq = DOTween.Sequence().SetUpdate(true);
        RectTransform popupRect = popup.rectTransform;
        if (popupRect != null)
        {
            Vector2 start = popupRect.anchoredPosition;
            float perStepDuration = Mathf.Max(0.12f, rollStatePopupDuration);
            float risePerStep = rollStatePopupRiseDistance / Mathf.Max(1, steps.Count);
            popupRect.anchoredPosition = start;

            for (int i = 0; i < steps.Count; i++)
            {
                RollPopupStep step = steps[i];
                seq.AppendCallback(() =>
                {
                    popup.text = step.Text;
                    Color c = step.Color;
                    c.a = 1f;
                    popup.color = c;
                });
                seq.Append(popupRect.DOAnchorPosY(start.y + (risePerStep * (i + 1)), perStepDuration).SetEase(Ease.OutQuad));
                seq.Join(popup.DOFade(0f, perStepDuration).From(1f).SetEase(Ease.OutQuad));
                if (i < steps.Count - 1)
                    seq.AppendInterval(0.03f);
            }
        }
        else
        {
            Vector3 start = popup.transform.position;
            float perStepDuration = Mathf.Max(0.12f, rollStatePopupDuration);
            float risePerStep = rollStatePopupRiseDistance / Mathf.Max(1, steps.Count);
            popup.transform.position = start;

            for (int i = 0; i < steps.Count; i++)
            {
                RollPopupStep step = steps[i];
                seq.AppendCallback(() =>
                {
                    popup.text = step.Text;
                    Color c = step.Color;
                    c.a = 1f;
                    popup.color = c;
                });
                seq.Append(popup.transform.DOMoveY(start.y + (risePerStep * (i + 1)), perStepDuration).SetEase(Ease.OutQuad));
                seq.Join(popup.DOFade(0f, perStepDuration).From(1f).SetEase(Ease.OutQuad));
                if (i < steps.Count - 1)
                    seq.AppendInterval(0.03f);
            }
        }

        seq.OnComplete(() =>
        {
            ClearRollStatePopupVisuals(clearText: true);
            _rollStatePopupTween = null;
        });
        _rollStatePopupTween = seq;
    }

    private List<RollPopupStep> BuildRollPopupSteps()
    {
        List<RollPopupStep> steps = new List<RollPopupStep>(4);
        DiceFaceEnchantKind enchant = GetCurrentFaceEnchant();
        bool suppressCritBonus = DiceFaceEnchantUtility.SuppressesCritBonus(enchant);
        bool suppressFailPenalty = DiceFaceEnchantUtility.SuppressesFailPenalty(enchant);

        if (LastRollIsCrit)
        {
            steps.Add(new RollPopupStep(critText, GetCritPopupColor()));
            if (!suppressCritBonus)
            {
                int critAdded = GetCritDisplayAddedValue(LastRolledValue);
                if (critAdded > 0)
                    steps.Add(new RollPopupStep($"+{critAdded}", new Color(0.36f, 0.88f, 1f, 1f)));
            }
        }

        if (LastRollIsFail)
        {
            steps.Add(new RollPopupStep(failText, GetFailPopupColor()));
            if (!suppressFailPenalty)
                steps.Add(new RollPopupStep("/2", new Color(1f, 0.45f, 0.45f, 1f)));
        }

        return steps;
    }

    public void PlayFaceEnchantPopup(DiceFaceEnchantKind enchant, string effectText = null)
    {
        if (_previewSandboxMode || !Application.isPlaying || enchant == DiceFaceEnchantKind.None || enchant == DiceFaceEnchantKind.Gum)
            return;

        List<RollPopupStep> steps = new List<RollPopupStep>(2)
        {
            new RollPopupStep(DiceFaceEnchantUtility.GetDisplayName(enchant), new Color(0.36f, 0.88f, 1f, 1f))
        };

        if (!string.IsNullOrWhiteSpace(effectText))
            steps.Add(new RollPopupStep(effectText, new Color(0.36f, 0.88f, 1f, 1f)));

        PlayPopupSteps(steps);
    }

    public void PlayFaceEnchantEffectPopup(string effectText)
    {
        if (_previewSandboxMode || !Application.isPlaying || string.IsNullOrWhiteSpace(effectText))
            return;

        List<RollPopupStep> steps = new List<RollPopupStep>(1)
        {
            new RollPopupStep(effectText, new Color(0.36f, 0.88f, 1f, 1f))
        };

        PlayPopupSteps(steps);
    }

    private void PlayPopupSteps(List<RollPopupStep> steps)
    {
        if (steps == null || steps.Count == 0)
            return;

        TMP_Text popup = GetOrCreateRollStatePopupInstance();
        if (popup == null)
            return;

        _rollStatePopupTween?.Kill();

        PositionRollStatePopup(popup);
        popup.gameObject.SetActive(true);

        Sequence seq = DOTween.Sequence().SetUpdate(true);
        RectTransform popupRect = popup.rectTransform;
        if (popupRect != null)
        {
            Vector2 start = popupRect.anchoredPosition;
            float perStepDuration = Mathf.Max(0.12f, rollStatePopupDuration);
            float risePerStep = rollStatePopupRiseDistance / Mathf.Max(1, steps.Count);
            popupRect.anchoredPosition = start;

            for (int i = 0; i < steps.Count; i++)
            {
                RollPopupStep step = steps[i];
                seq.AppendCallback(() =>
                {
                    popup.text = step.Text;
                    Color c = step.Color;
                    c.a = 1f;
                    popup.color = c;
                });
                seq.Append(popupRect.DOAnchorPosY(start.y + (risePerStep * (i + 1)), perStepDuration).SetEase(Ease.OutQuad));
                seq.Join(popup.DOFade(0f, perStepDuration).From(1f).SetEase(Ease.OutQuad));
                if (i < steps.Count - 1)
                    seq.AppendInterval(0.03f);
            }
        }
        else
        {
            Vector3 start = popup.transform.position;
            float perStepDuration = Mathf.Max(0.12f, rollStatePopupDuration);
            float risePerStep = rollStatePopupRiseDistance / Mathf.Max(1, steps.Count);
            popup.transform.position = start;

            for (int i = 0; i < steps.Count; i++)
            {
                RollPopupStep step = steps[i];
                seq.AppendCallback(() =>
                {
                    popup.text = step.Text;
                    Color c = step.Color;
                    c.a = 1f;
                    popup.color = c;
                });
                seq.Append(popup.transform.DOMoveY(start.y + (risePerStep * (i + 1)), perStepDuration).SetEase(Ease.OutQuad));
                seq.Join(popup.DOFade(0f, perStepDuration).From(1f).SetEase(Ease.OutQuad));
                if (i < steps.Count - 1)
                    seq.AppendInterval(0.03f);
            }
        }

        seq.OnComplete(() =>
        {
            ClearRollStatePopupVisuals(clearText: true);
            _rollStatePopupTween = null;
        });
        _rollStatePopupTween = seq;
    }
}
