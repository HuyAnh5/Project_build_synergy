using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public partial class DiceSpinnerGeneric
{
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
            steps.Add(new RollPopupStep(critText, rollStatePopupColor));
            if (!suppressCritBonus)
            {
                int critAdded = GetCritDisplayAddedValue(LastRolledValue);
                if (critAdded > 0)
                    steps.Add(new RollPopupStep($"+{critAdded}", new Color(0.36f, 0.88f, 1f, 1f)));
            }
        }

        if (LastRollIsFail)
        {
            steps.Add(new RollPopupStep(failText, rollStatePopupColor));
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

    private static string GetEnchantShortLabel(DiceFaceEnchantKind enchant)
    {
        switch (enchant)
        {
            case DiceFaceEnchantKind.Power: return "Pwr";
            case DiceFaceEnchantKind.Guard: return "Guard";
            case DiceFaceEnchantKind.Charge: return "AP";
            case DiceFaceEnchantKind.Gold: return "Gold";
            case DiceFaceEnchantKind.Gum: return "Gum";
            case DiceFaceEnchantKind.Relay: return "Relay";
            case DiceFaceEnchantKind.Double: return "x2";
            case DiceFaceEnchantKind.Repeat: return "Rpt";
            case DiceFaceEnchantKind.Reload: return "Load";
            case DiceFaceEnchantKind.Heavy: return "Heavy";
            case DiceFaceEnchantKind.Echo: return "Echo";
            case DiceFaceEnchantKind.Stone: return "Stone";
            default: return DiceFaceEnchantUtility.GetDisplayName(enchant);
        }
    }

    private void ClearRollStatePopupVisuals(bool clearText)
    {
        _rollStatePopupTween?.Kill();
        _rollStatePopupTween = null;

        if (_rollStatePopupInstance == null)
            return;

        if (clearText)
            _rollStatePopupInstance.text = string.Empty;

        _rollStatePopupInstance.gameObject.SetActive(false);
    }

    private TMP_Text GetOrCreateRollStatePopupInstance()
    {
        Canvas popupCanvas = ResolveRollStatePopupCanvas();
        Transform popupParent = popupCanvas != null ? popupCanvas.transform : transform;
        if (popupParent == null)
            return null;

        bool needsRecreate =
            _rollStatePopupInstance == null ||
            _rollStatePopupCanvas != popupCanvas ||
            _rollStatePopupInstance.transform.parent != popupParent;

        if (!needsRecreate)
            return _rollStatePopupInstance;

        if (_rollStatePopupInstance != null)
        {
            if (Application.isPlaying)
                Destroy(_rollStatePopupInstance.gameObject);
            else
                DestroyImmediate(_rollStatePopupInstance.gameObject);
        }

        _rollStatePopupCanvas = popupCanvas;
        GameObject popupGo = new GameObject("CritFailPopup", typeof(RectTransform), typeof(TextMeshProUGUI));
        popupGo.layer = gameObject.layer;
        popupGo.transform.SetParent(popupParent, false);
        _rollStatePopupInstance = popupGo.GetComponent<TextMeshProUGUI>();
        _rollStatePopupInstance.raycastTarget = false;
        _rollStatePopupInstance.text = string.Empty;
        _rollStatePopupInstance.enableAutoSizing = false;
        _rollStatePopupInstance.fontSize = Mathf.Max(18f, rollStatePopupFontSize);
        _rollStatePopupInstance.enableWordWrapping = false;
        _rollStatePopupInstance.overflowMode = TextOverflowModes.Overflow;
        _rollStatePopupInstance.alignment = TextAlignmentOptions.Center;
        _rollStatePopupInstance.color = rollStatePopupColor;
        if (TMP_Settings.defaultFontAsset != null)
            _rollStatePopupInstance.font = TMP_Settings.defaultFontAsset;
        RectTransform popupRect = _rollStatePopupInstance.rectTransform;
        popupRect.anchorMin = new Vector2(0.5f, 0.5f);
        popupRect.anchorMax = new Vector2(0.5f, 0.5f);
        popupRect.pivot = new Vector2(0.5f, 0.5f);
        popupRect.sizeDelta = new Vector2(240f, Mathf.Max(56f, rollStatePopupFontSize * 1.4f));
        _rollStatePopupInstance.transform.localScale = Vector3.one;
        _rollStatePopupInstance.gameObject.SetActive(false);
        return _rollStatePopupInstance;
    }

    private Canvas ResolveRollStatePopupCanvas()
    {
        if (_rollStatePopupCanvas != null && !_rollStatePopupCanvas.transform.IsChildOf(pivot))
            return _rollStatePopupCanvas;

        RectTransform anchor = GetCritFailPopupAnchor();
        Canvas sourceCanvas = anchor != null ? anchor.GetComponentInParent<Canvas>() : null;
        if (sourceCanvas != null && sourceCanvas.rootCanvas != null && !sourceCanvas.rootCanvas.transform.IsChildOf(pivot))
            return sourceCanvas.rootCanvas;

        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        Canvas fallback = sourceCanvas != null ? sourceCanvas.rootCanvas : null;
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null || !canvas.isRootCanvas)
                continue;

            if (pivot != null && canvas.transform.IsChildOf(pivot))
                continue;

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay || canvas.renderMode == RenderMode.ScreenSpaceCamera)
                return canvas;

            if (fallback == null)
                fallback = canvas;
        }

        return fallback;
    }

    private void PositionRollStatePopup(TMP_Text popup)
    {
        RectTransform sourceAnchor = GetCritFailPopupAnchor();
        if (popup == null || sourceAnchor == null)
            return;

        RectTransform popupRect = popup.rectTransform;
        RectTransform sourceRect = sourceAnchor;
        if (popupRect == null || sourceRect == null)
        {
            popup.transform.position = sourceAnchor.position;
            return;
        }

        Canvas popupCanvas = _rollStatePopupCanvas != null ? _rollStatePopupCanvas : popup.canvas;
        RectTransform popupCanvasRect = popupCanvas != null ? popupCanvas.transform as RectTransform : null;
        if (popupCanvasRect == null)
        {
            popup.transform.position = sourceAnchor.position;
            return;
        }

        popupRect.anchorMin = new Vector2(0.5f, 0.5f);
        popupRect.anchorMax = new Vector2(0.5f, 0.5f);
        popupRect.pivot = sourceRect.pivot;

        Camera sourceCamera = GetCanvasEventCamera(sourceRect.GetComponentInParent<Canvas>());
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(sourceCamera, sourceRect.position);
        Camera popupCamera = GetCanvasEventCamera(popupCanvas);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(popupCanvasRect, screenPoint, popupCamera, out Vector2 localPoint))
            popupRect.anchoredPosition = localPoint;
        else
            popup.transform.position = sourceAnchor.position;
    }

    private static Camera GetCanvasEventCamera(Canvas canvas)
    {
        if (canvas == null)
            return null;

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera;
    }

    private void AutoWireTextReferences()
    {
        valueText = null;
        enchantText = null;
        rollStateText = null;
    }

    private RectTransform GetCritFailPopupAnchor()
    {
        DiceDraggableUI diceUi = GetDiceDraggableUi();
        if (diceUi != null)
            return diceUi.GetCritFailPopupAnchor();
        return null;
    }

    private DiceDraggableUI GetDiceDraggableUi()
    {
        if (_cachedDiceDraggableUi != null && _cachedDiceDraggableUi.dice == this)
            return _cachedDiceDraggableUi;

        DiceDraggableUI[] allDiceUi = FindObjectsOfType<DiceDraggableUI>(true);
        for (int i = 0; i < allDiceUi.Length; i++)
        {
            DiceDraggableUI candidate = allDiceUi[i];
            if (candidate != null && candidate.dice == this)
            {
                _cachedDiceDraggableUi = candidate;
                return candidate;
            }
        }

        return null;
    }
    private static Color GetMaterialColor(Material material)
    {
        if (material == null)
            return Color.white;
        if (material.HasProperty(BaseColorPropertyId))
            return material.GetColor(BaseColorPropertyId);
        if (material.HasProperty(ColorPropertyId))
            return material.GetColor(ColorPropertyId);
        return Color.white;
    }

    private static void SetMaterialColor(Material material, Color color)
    {
        if (material == null)
            return;
        if (material.HasProperty(BaseColorPropertyId))
            material.SetColor(BaseColorPropertyId, color);
        if (material.HasProperty(ColorPropertyId))
            material.SetColor(ColorPropertyId, color);
    }

    private void PlayFailFeedbackShake()
    {
        if (!Application.isPlaying || pivot == null)
            return;

        _feedbackShakeTween?.Kill();
        pivot.localPosition = _pivotBaseLocalPosition;
        _feedbackShakeTween = pivot.DOPunchPosition(
                failShakeStrength,
                Mathf.Max(0.01f, failShakeDuration),
                Mathf.Max(1, failShakeVibrato),
                Mathf.Clamp01(failShakeElasticity),
                snapping: false)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                if (pivot != null)
                    pivot.localPosition = _pivotBaseLocalPosition;
                _feedbackShakeTween = null;
            });
    }

    private GameObject TryCreateOutlineRenderer(Renderer sourceRenderer, Shader outlineShader, out Renderer outlineRenderer, out Material outlineMaterial)
    {
        outlineRenderer = null;
        outlineMaterial = null;

        if (sourceRenderer == null || outlineShader == null)
            return null;

        int materialCount = sourceRenderer.sharedMaterials != null && sourceRenderer.sharedMaterials.Length > 0
            ? sourceRenderer.sharedMaterials.Length
            : 1;

        Material sharedOutlineMaterial = new Material(outlineShader)
        {
            name = $"{sourceRenderer.name}_FeedbackOutline",
            hideFlags = HideFlags.HideAndDontSave
        };

        if (sourceRenderer is MeshRenderer meshRenderer)
        {
            MeshFilter sourceFilter = meshRenderer.GetComponent<MeshFilter>();
            if (sourceFilter == null || sourceFilter.sharedMesh == null)
            {
                if (Application.isPlaying)
                    Destroy(sharedOutlineMaterial);
                else
                    DestroyImmediate(sharedOutlineMaterial);
                return null;
            }

            GameObject outlineGo = new GameObject($"{sourceRenderer.name}__FeedbackOutline", typeof(Transform), typeof(MeshFilter), typeof(MeshRenderer));
            outlineGo.hideFlags = HideFlags.HideAndDontSave;
            outlineGo.layer = sourceRenderer.gameObject.layer;
            outlineGo.transform.SetParent(sourceRenderer.transform, false);
            outlineGo.transform.localPosition = Vector3.zero;
            outlineGo.transform.localRotation = Quaternion.identity;
            outlineGo.transform.localScale = Vector3.one * Mathf.Max(1.001f, outlineScaleMultiplier);

            MeshFilter outlineFilter = outlineGo.GetComponent<MeshFilter>();
            outlineFilter.sharedMesh = sourceFilter.sharedMesh;

            MeshRenderer createdRenderer = outlineGo.GetComponent<MeshRenderer>();
            createdRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            createdRenderer.receiveShadows = false;
            createdRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            createdRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            createdRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;

            Material[] materials = new Material[materialCount];
            for (int i = 0; i < materials.Length; i++)
                materials[i] = sharedOutlineMaterial;
            createdRenderer.sharedMaterials = materials;
            createdRenderer.enabled = false;

            outlineRenderer = createdRenderer;
            outlineMaterial = sharedOutlineMaterial;
            return outlineGo;
        }

        if (sourceRenderer is SkinnedMeshRenderer skinnedRenderer)
        {
            if (skinnedRenderer.sharedMesh == null)
            {
                if (Application.isPlaying)
                    Destroy(sharedOutlineMaterial);
                else
                    DestroyImmediate(sharedOutlineMaterial);
                return null;
            }

            GameObject outlineGo = new GameObject($"{sourceRenderer.name}__FeedbackOutline", typeof(Transform), typeof(SkinnedMeshRenderer));
            outlineGo.hideFlags = HideFlags.HideAndDontSave;
            outlineGo.layer = sourceRenderer.gameObject.layer;
            outlineGo.transform.SetParent(sourceRenderer.transform, false);
            outlineGo.transform.localPosition = Vector3.zero;
            outlineGo.transform.localRotation = Quaternion.identity;
            outlineGo.transform.localScale = Vector3.one * Mathf.Max(1.001f, outlineScaleMultiplier);

            SkinnedMeshRenderer createdRenderer = outlineGo.GetComponent<SkinnedMeshRenderer>();
            createdRenderer.sharedMesh = skinnedRenderer.sharedMesh;
            createdRenderer.rootBone = skinnedRenderer.rootBone;
            createdRenderer.bones = skinnedRenderer.bones;
            createdRenderer.updateWhenOffscreen = true;
            createdRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            createdRenderer.receiveShadows = false;
            createdRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            createdRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            createdRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;

            Material[] materials = new Material[materialCount];
            for (int i = 0; i < materials.Length; i++)
                materials[i] = sharedOutlineMaterial;
            createdRenderer.sharedMaterials = materials;
            createdRenderer.enabled = false;

            outlineRenderer = createdRenderer;
            outlineMaterial = sharedOutlineMaterial;
            return outlineGo;
        }

        if (Application.isPlaying)
            Destroy(sharedOutlineMaterial);
        else
            DestroyImmediate(sharedOutlineMaterial);

        return null;
    }

    private static Vector3 NormalizeEuler(Vector3 e)
    {
        return new Vector3(Norm(e.x), Norm(e.y), Norm(e.z));

        static float Norm(float a)
        {
            a %= 360f;
            if (a < 0f)
                a += 360f;
            return a;
        }
    }

}
