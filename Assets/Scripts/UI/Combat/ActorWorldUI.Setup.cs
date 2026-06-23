using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public partial class ActorWorldUI
{
    private void ResolveReferences()
    {
        worldCanvasRoot = ResolveRectTransform(worldCanvasRoot, "WorldCanvasRoot");
        if (worldCanvasRoot == null)
            return;

        if (worldCanvas == null)
            worldCanvas = worldCanvasRoot.GetComponent<Canvas>();
        if (worldCanvasGroup == null)
            worldCanvasGroup = worldCanvasRoot.GetComponent<CanvasGroup>();
        tooltipAnchorRoot = ResolveRectTransform(tooltipAnchorRoot, "WorldCanvasRoot/TooltipAnchor");
        tooltipBottomLimitRoot = ResolveRectTransform(tooltipBottomLimitRoot, "WorldCanvasRoot/TooltipBottomLimit");

        previewDummyRoot = ResolveRectTransform(previewDummyRoot, "WorldCanvasRoot/PreviewDummy");
        if (previewDummyImage == null && previewDummyRoot != null)
            previewDummyImage = previewDummyRoot.GetComponent<Image>();

        intentRoot = ResolveRectTransform(intentRoot, "WorldCanvasRoot/IntentRoot");
        if (intentCanvasGroup == null && intentRoot != null)
            intentCanvasGroup = intentRoot.GetComponent<CanvasGroup>();
        if (intentIcon == null && intentRoot != null)
            intentIcon = FindChildComponent<Image>(intentRoot, "Icon");
        if (intentValueText == null && intentRoot != null)
            intentValueText = FindChildComponent<TMP_Text>(intentRoot, "Value");

        hpBarRoot = ResolveRectTransform(hpBarRoot, "WorldCanvasRoot/HpBarRoot");
        if (hpBarBackground == null && hpBarRoot != null)
            hpBarBackground = FindChildComponent<Image>(hpBarRoot, "Background");
        if (hpBarOutline == null && hpBarBackground != null)
            hpBarOutline = hpBarBackground.GetComponent<Outline>();
        if (hpBarFill == null && hpBarBackground != null)
            hpBarFill = FindChildComponent<Image>(hpBarBackground.rectTransform, "Fill");
        if (hpText == null && hpBarBackground != null)
            hpText = FindChildComponent<TMP_Text>(hpBarBackground.rectTransform, "HpText");

        guardRoot = ResolveRectTransform(guardRoot, "WorldCanvasRoot/HpBarRoot/GuardRoot");
        if (guardIcon == null && guardRoot != null)
            guardIcon = FindChildComponent<Image>(guardRoot, "Icon");
        if (guardText == null && guardRoot != null)
            guardText = FindChildComponent<TMP_Text>(guardRoot, "Value");

        statusRowRoot = ResolveRectTransform(statusRowRoot, "WorldCanvasRoot/StatusRow");
        if (statusRowRoot == null)
            return;

        statusSlotTemplateRoot = ResolveStatusSlotTemplateRoot();
        EnsureStatusSlotsArray();

        for (int i = 0; i < statusSlots.Length; i++)
        {
            StatusIconSlot slot = statusSlots[i] ?? new StatusIconSlot();
            RectTransform root = ResolveRectTransform(slot.root, $"WorldCanvasRoot/StatusRow/Status_{i + 1}");
            slot.root = root;
            if (slot.root != null)
            {
                if (slot.background == null)
                    slot.background = slot.root.GetComponent<Image>();
                if (slot.iconImage == null)
                    slot.iconImage = FindChildComponent<Image>(slot.root, "Icon");
                if (slot.shortLabelText == null)
                    slot.shortLabelText = FindChildComponent<TMP_Text>(slot.root, "ShortLabel");
                if (slot.valueText == null)
                    slot.valueText = FindChildComponent<TMP_Text>(slot.root, "Value");
            }

            statusSlots[i] = slot;
        }

        if (guardIcon != null && TryGetStatusVisual(CombatUiStatusIconKind.Guard, out StatusVisualData guardData))
        {
            guardIcon.sprite = guardData.sprite;
            guardIcon.color = Color.white;
        }
    }

    private bool AreRuntimeReferencesMissing()
    {
        if (worldCanvasRoot == null || worldCanvas == null || worldCanvasGroup == null)
            return true;
        if (intentRoot == null || intentIcon == null || intentValueText == null)
            return true;
        if (hpBarRoot == null || hpBarBackground == null || hpBarFill == null || hpText == null)
            return true;
        if (guardRoot == null || guardIcon == null || guardText == null)
            return true;
        if (statusRowRoot == null || statusSlots == null || statusSlots.Length == 0)
            return true;

        for (int i = 0; i < statusSlots.Length; i++)
        {
            StatusIconSlot slot = statusSlots[i];
            if (slot == null || slot.root == null)
                return true;
        }

        return false;
    }

    private void AttachToActorAnchor()
    {
        if (actor == null)
            return;

        Transform anchor = actor.uiAnchor != null ? actor.uiAnchor : actor.transform;
        if (anchor == null)
            return;

        if (transform.parent != anchor)
            transform.SetParent(anchor, false);

        // Đã bỏ dòng ghi đè localPosition để tôn trọng Transform Y = 1.2
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
    }

    private void HidePreviewDummyRuntime()
    {
        if (previewDummyRoot != null)
            previewDummyRoot.gameObject.SetActive(false);
    }

    private void DisableAllGraphicRaycasts()
    {
        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
            graphics[i].raycastTarget = false;
    }

    private void DisableLegacyChildren()
    {
        DisableLegacyChild("HP_Text");
        DisableLegacyChild("Guard_Text");
        DisableLegacyChild("Status_Text");
        DisableLegacyChild("Intent_Text");
    }

    private void DisableLegacyChild(string childName)
    {
        Transform child = transform.Find(childName);
        if (child != null && child.gameObject.activeSelf)
            child.gameObject.SetActive(false);
    }

    private void ForceSortingOrder(int order)
    {
        if (worldCanvas != null)
        {
            worldCanvas.overrideSorting = true;
            worldCanvas.sortingOrder = order;
        }
    }

    private RectTransform ResolveRectTransform(RectTransform current, string path)
    {
        if (current != null)
            return current;

        Transform found = transform.Find(path);
        return found as RectTransform;
    }

    private static T FindChildComponent<T>(Transform parent, string childName) where T : Component
    {
        if (parent == null)
            return null;

        Transform child = parent.Find(childName);
        return child != null ? child.GetComponent<T>() : null;
    }

    private void EnsureStatusSlotsArray()
    {
        if (statusSlots == null || statusSlots.Length != DefaultStatusSlotCount)
            statusSlots = new StatusIconSlot[DefaultStatusSlotCount];
    }

    private RectTransform ResolveStatusSlotTemplateRoot()
    {
        if (statusSlotTemplateRoot != null)
            return statusSlotTemplateRoot;

        if (statusSlots != null)
        {
            for (int i = 0; i < statusSlots.Length; i++)
            {
                if (statusSlots[i] != null && statusSlots[i].root != null)
                    return statusSlots[i].root;
            }
        }

        if (statusRowRoot == null)
            return null;

        Transform namedTemplate = statusRowRoot.Find("Status_1");
        if (namedTemplate is RectTransform namedTemplateRect)
            return namedTemplateRect;

        for (int i = 0; i < statusRowRoot.childCount; i++)
        {
            if (statusRowRoot.GetChild(i) is RectTransform childRect)
                return childRect;
        }

        return null;
    }

    private StatusIconSlot CreateSlotFromRoot(RectTransform root)
    {
        if (root == null)
            return null;

        return new StatusIconSlot
        {
            root = root,
            background = root.GetComponent<Image>(),
            iconImage = FindChildComponent<Image>(root, "Icon"),
            shortLabelText = FindChildComponent<TMP_Text>(root, "ShortLabel"),
            valueText = FindChildComponent<TMP_Text>(root, "Value")
        };
    }

    private void EnsureSpawnedStatusSlotCapacity(int requiredCount)
    {
        if (statusSlotTemplateRoot == null)
            return;

        for (int i = _spawnedStatusSlots.Count; i < requiredCount; i++)
        {
            RectTransform cloneRoot = Instantiate(statusSlotTemplateRoot, statusRowRoot);
            cloneRoot.name = $"Status_{i + 1}";
            cloneRoot.gameObject.SetActive(false);
            _spawnedStatusSlots.Add(CreateSlotFromRoot(cloneRoot));
        }

        for (int i = 0; i < _spawnedStatusSlots.Count; i++)
        {
            StatusIconSlot slot = _spawnedStatusSlots[i];
            if (slot?.root != null)
                slot.root.name = $"Status_{i + 1}";
        }
    }

    private void CleanupSpawnedStatusSlots()
    {
        for (int i = _spawnedStatusSlots.Count - 1; i >= 0; i--)
        {
            StatusIconSlot slot = _spawnedStatusSlots[i];
            if (slot?.root == null)
                continue;

            if (Application.isPlaying)
                Destroy(slot.root.gameObject);
            else
                DestroyImmediate(slot.root.gameObject);
        }

        _spawnedStatusSlots.Clear();

        if (statusRowRoot == null || statusSlotTemplateRoot == null)
            return;

        for (int i = statusRowRoot.childCount - 1; i >= 0; i--)
        {
            RectTransform child = statusRowRoot.GetChild(i) as RectTransform;
            if (child == null || child == statusSlotTemplateRoot)
                continue;

            if (!child.name.StartsWith("Status_", StringComparison.Ordinal))
                continue;

            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
    }

    private IList<StatusIconSlot> GetResolvedStatusSlots(int requiredCount)
    {
        if (statusSlotTemplateRoot != null)
        {
            if (!Application.isPlaying)
            {
                CleanupSpawnedStatusSlots();
                EnsureStatusSlotsArray();
                return statusSlots;
            }

            EnsureSpawnedStatusSlotCapacity(requiredCount);
            return _spawnedStatusSlots;
        }

        EnsureStatusSlotsArray();
        return statusSlots;
    }

    private static string GetAilmentShortLabel(AilmentType ailment)
    {
        string text = ailment.ToString().ToUpperInvariant();
        return text.Length <= 2 ? text : text.Substring(0, 2);
    }

    private bool CanRefreshInEditor()
    {
#if UNITY_EDITOR
        if (EditorUtility.IsPersistent(this) && !PrefabUtility.IsPartOfPrefabInstance(this))
            return false;
#endif
        return true;
    }
}

