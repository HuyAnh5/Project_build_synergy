using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public partial class DiceSpinnerGeneric
{
    private void EnsureWholeDieMaterialInstances()
    {
        if (!CanUseWholeDieMaterialInstances())
            return;

        AutoCollectWholeDieRenderers();

        if (wholeDieRenderers == null || wholeDieRenderers.Length == 0)
            return;

        if (_wholeDieMaterialInstances != null &&
            _wholeDieOriginalColors != null &&
            _wholeDieMaterialInstances.Length == wholeDieRenderers.Length &&
            _wholeDieOriginalColors.Length == wholeDieRenderers.Length)
        {
            return;
        }

        ReleaseWholeDieMaterialInstances();

        _wholeDieMaterialInstances = new Material[wholeDieRenderers.Length][];
        _wholeDieOriginalColors = new Color[wholeDieRenderers.Length][];

        for (int rendererIndex = 0; rendererIndex < wholeDieRenderers.Length; rendererIndex++)
        {
            Renderer renderer = wholeDieRenderers[rendererIndex];
            if (renderer == null)
                continue;

            Material[] materials = renderer.materials;
            if (materials == null)
                continue;

            _wholeDieMaterialInstances[rendererIndex] = materials;
            _wholeDieOriginalColors[rendererIndex] = new Color[materials.Length];

            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                _wholeDieOriginalColors[rendererIndex][materialIndex] = GetMaterialColor(materials[materialIndex]);
        }
    }

    private void ApplyWholeDieVisuals()
    {
        EnsureWholeDieMaterialInstances();

        if (_wholeDieMaterialInstances == null || _wholeDieOriginalColors == null)
            return;

        for (int rendererIndex = 0; rendererIndex < _wholeDieMaterialInstances.Length; rendererIndex++)
        {
            Material[] materials = _wholeDieMaterialInstances[rendererIndex];
            Color[] originalColors = rendererIndex < _wholeDieOriginalColors.Length ? _wholeDieOriginalColors[rendererIndex] : null;
            if (materials == null)
                continue;

            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                Material material = materials[materialIndex];
                if (material == null)
                    continue;

                Color targetColor = originalColors != null && materialIndex < originalColors.Length
                    ? originalColors[materialIndex]
                    : Color.white;

                switch (wholeDieTag)
                {
                    case DiceWholeDieTag.Patina:
                        targetColor = patinaColor;
                        break;
                }

                SetMaterialColor(material, targetColor);
            }
        }
    }

    private void EnsureFeedbackOutlineRenderers()
    {
        if (!Application.isPlaying || !enableRollResultOutline)
            return;

        AutoCollectWholeDieRenderers();
        if (wholeDieRenderers == null || wholeDieRenderers.Length == 0)
            return;

        if (_feedbackOutlineRenderers != null &&
            _feedbackOutlineObjects != null &&
            _feedbackOutlineMaterials != null &&
            _feedbackOutlineRenderers.Length == wholeDieRenderers.Length &&
            _feedbackOutlineObjects.Length == wholeDieRenderers.Length &&
            _feedbackOutlineMaterials.Length == wholeDieRenderers.Length)
        {
            return;
        }

        ReleaseFeedbackOutlineRenderers();

        Shader outlineShader = Shader.Find(FeedbackOutlineShaderName);
        if (outlineShader == null)
            return;

        _feedbackOutlineObjects = new GameObject[wholeDieRenderers.Length];
        _feedbackOutlineRenderers = new Renderer[wholeDieRenderers.Length];
        _feedbackOutlineMaterials = new Material[wholeDieRenderers.Length];

        for (int rendererIndex = 0; rendererIndex < wholeDieRenderers.Length; rendererIndex++)
        {
            Renderer sourceRenderer = wholeDieRenderers[rendererIndex];
            if (sourceRenderer == null)
                continue;

            GameObject outlineGo = TryCreateOutlineRenderer(sourceRenderer, outlineShader, out Renderer outlineRenderer, out Material outlineMaterial);
            if (outlineGo == null || outlineRenderer == null || outlineMaterial == null)
                continue;

            _feedbackOutlineObjects[rendererIndex] = outlineGo;
            _feedbackOutlineRenderers[rendererIndex] = outlineRenderer;
            _feedbackOutlineMaterials[rendererIndex] = outlineMaterial;
        }
    }

    private void ApplyFeedbackOutlineVisuals()
    {
        EnsureFeedbackOutlineRenderers();
        if (_feedbackOutlineRenderers == null || _feedbackOutlineMaterials == null)
            return;

        bool showOutline = enableRollResultOutline && (_feedbackCrit || _feedbackFail);
        Color outlineColor = _feedbackFail ? failOutlineColor : critOutlineColor;

        for (int rendererIndex = 0; rendererIndex < _feedbackOutlineRenderers.Length; rendererIndex++)
        {
            Renderer outlineRenderer = _feedbackOutlineRenderers[rendererIndex];
            Material outlineMaterial = rendererIndex < _feedbackOutlineMaterials.Length ? _feedbackOutlineMaterials[rendererIndex] : null;
            GameObject outlineGo = rendererIndex < _feedbackOutlineObjects.Length ? _feedbackOutlineObjects[rendererIndex] : null;
            Renderer sourceRenderer = wholeDieRenderers != null && rendererIndex < wholeDieRenderers.Length ? wholeDieRenderers[rendererIndex] : null;

            if (outlineRenderer == null || outlineMaterial == null || outlineGo == null)
                continue;

            bool active = showOutline && sourceRenderer != null && sourceRenderer.enabled && sourceRenderer.gameObject.activeInHierarchy;
            if (outlineGo.activeSelf != active)
                outlineGo.SetActive(active);
            if (outlineRenderer.enabled != active)
                outlineRenderer.enabled = active;

            if (!active)
                continue;

            outlineGo.transform.localScale = Vector3.one * Mathf.Max(1.001f, outlineScaleMultiplier);
            SetMaterialColor(outlineMaterial, outlineColor);
        }
    }

    private void AutoCollectWholeDieRenderers()
    {
        if (wholeDieRenderers != null && wholeDieRenderers.Length > 0)
            return;

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        List<Renderer> filtered = new List<Renderer>(renderers.Length);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;
            if (renderer.GetComponent<TMP_Text>() != null)
                continue;

            filtered.Add(renderer);
        }

        wholeDieRenderers = filtered.ToArray();
    }

    private void ReleaseWholeDieMaterialInstances()
    {
        if (_wholeDieMaterialInstances == null)
            return;

        for (int rendererIndex = 0; rendererIndex < _wholeDieMaterialInstances.Length; rendererIndex++)
        {
            Material[] materials = _wholeDieMaterialInstances[rendererIndex];
            if (materials == null)
                continue;

            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                Material material = materials[materialIndex];
                if (material == null)
                    continue;

                if (Application.isPlaying)
                    Destroy(material);
                else
                    DestroyImmediate(material);
            }
        }

        _wholeDieMaterialInstances = null;
        _wholeDieOriginalColors = null;
    }

    private void ReleaseFeedbackOutlineRenderers()
    {
        if (_feedbackOutlineMaterials != null)
        {
            for (int i = 0; i < _feedbackOutlineMaterials.Length; i++)
            {
                Material material = _feedbackOutlineMaterials[i];
                if (material == null)
                    continue;

                if (Application.isPlaying)
                    Destroy(material);
                else
                    DestroyImmediate(material);
            }
        }

        if (_feedbackOutlineObjects != null)
        {
            for (int i = 0; i < _feedbackOutlineObjects.Length; i++)
            {
                GameObject outlineGo = _feedbackOutlineObjects[i];
                if (outlineGo == null)
                    continue;

                if (Application.isPlaying)
                    Destroy(outlineGo);
                else
                    DestroyImmediate(outlineGo);
            }
        }

        _feedbackOutlineObjects = null;
        _feedbackOutlineRenderers = null;
        _feedbackOutlineMaterials = null;
    }

    private bool CanUseWholeDieMaterialInstances()
    {
        if (!Application.isPlaying)
            return false;

#if UNITY_EDITOR
        if (EditorUtility.IsPersistent(this) && !PrefabUtility.IsPartOfPrefabInstance(this))
            return false;
#endif
        return true;
    }

}

