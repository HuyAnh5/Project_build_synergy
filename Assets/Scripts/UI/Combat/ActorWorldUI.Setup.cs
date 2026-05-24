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
        if (statusSlots == null || statusSlots.Length != DefaultStatusSlotCount)
            statusSlots = new StatusIconSlot[DefaultStatusSlotCount];

        if (statusRowRoot == null)
            return;

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

    private void AttachToActorAnchor()
    {
        if (actor == null)
            return;

        // Bỏ qua uiAnchor, luôn ép UI Canvas dính vào đúng gốc của Actor
        Transform anchor = actor.transform;
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

