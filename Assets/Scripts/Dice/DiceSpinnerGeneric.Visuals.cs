using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public partial class DiceSpinnerGeneric
{
    private void PlayRollStatePopupIfNeeded()
    {
        if (!Application.isPlaying || !animateCritFailPopup)
            return;

        string label = null;
        if (LastRollIsCrit)
            label = critText;
        else if (LastRollIsFail)
            label = failText;

        if (string.IsNullOrWhiteSpace(label))
            return;

        TMP_Text popup = GetOrCreateRollStatePopupInstance();
        if (popup == null)
            return;

        _rollStatePopupTween?.Kill();

        PositionRollStatePopup(popup);
        popup.gameObject.SetActive(true);
        popup.text = label;
        Color popupColor = rollStatePopupColor;
        popupColor.a = 1f;
        popup.color = popupColor;

        Sequence seq = DOTween.Sequence();
        if (popup.rectTransform != null)
        {
            Vector2 start = popup.rectTransform.anchoredPosition;
            seq.Append(popup.rectTransform.DOAnchorPosY(start.y + rollStatePopupRiseDistance, Mathf.Max(0.01f, rollStatePopupDuration)).SetEase(Ease.OutQuad));
        }
        else
        {
            Vector3 start = popup.transform.position;
            seq.Append(popup.transform.DOMoveY(start.y + rollStatePopupRiseDistance, Mathf.Max(0.01f, rollStatePopupDuration)).SetEase(Ease.OutQuad));
        }

        seq.Join(popup.DOFade(0f, Mathf.Max(0.01f, rollStatePopupDuration)).SetEase(Ease.OutQuad));
        seq.SetUpdate(true);
        seq.OnComplete(() =>
        {
            ClearRollStatePopupVisuals(clearText: true);
            _rollStatePopupTween = null;
        });
        _rollStatePopupTween = seq;
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

    private static string GetEnchantShortLabel(DiceFaceEnchantKind enchant)
    {
        switch (enchant)
        {
            case DiceFaceEnchantKind.ValuePlusN:
                return "Plus";
            case DiceFaceEnchantKind.GuardBoost:
                return "Guard";
            case DiceFaceEnchantKind.Fire:
                return "Fire";
            case DiceFaceEnchantKind.Bleed:
                return "Bleed";
            case DiceFaceEnchantKind.Ice:
                return "Ice";
            case DiceFaceEnchantKind.Lightning:
                return "Bolt";
            case DiceFaceEnchantKind.GoldProc:
                return "Gold";
            default:
                return DiceFaceEnchantUtility.GetDisplayName(enchant);
        }
    }
}
