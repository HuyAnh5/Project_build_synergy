using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class ActorWorldUiSetupTool
{
    private const string PrefabPath = "Assets/Prefabs/Entities/world-ui.prefab";
    private const string IconLibraryAssetPath = "Assets/GameData/UI/SkillUiIconLibrary.asset";
    private const int StatusSlotCount = 8;

    [MenuItem("Tools/Build Synergy/Legacy/Setup World UI Prefab")]
    public static void SetupWorldUiPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        if (root == null)
        {
            Debug.LogError($"Failed to load prefab at {PrefabPath}.");
            return;
        }

        try
        {
            ActorWorldUI ui = root.GetComponent<ActorWorldUI>();
            if (ui == null)
            {
                Debug.LogError($"Prefab at {PrefabPath} does not contain {nameof(ActorWorldUI)}.");
                return;
            }

            BuildHierarchy(root.transform, ui);
            ui.iconLibrary = AssetDatabase.LoadAssetAtPath<SkillUiIconLibrarySO>(IconLibraryAssetPath);
            ui.SetupWorldUiLayout();
            EditorUtility.SetDirty(ui);
            EditorUtility.SetDirty(root);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log("World UI prefab setup complete.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void BuildHierarchy(Transform root, ActorWorldUI ui)
    {
        RectTransform canvasRoot = FindOrCreateRect(root, "WorldCanvasRoot");
        Canvas canvas = canvasRoot.GetComponent<Canvas>() ?? canvasRoot.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        CanvasGroup canvasGroup = canvasRoot.GetComponent<CanvasGroup>() ?? canvasRoot.gameObject.AddComponent<CanvasGroup>();

        canvasRoot.anchorMin = new Vector2(0.5f, 0.5f);
        canvasRoot.anchorMax = new Vector2(0.5f, 0.5f);
        canvasRoot.pivot = new Vector2(0.5f, 0.5f);
        canvasRoot.anchoredPosition = Vector2.zero;
        canvasRoot.sizeDelta = ui.rootSize;
        canvasRoot.localScale = ui.worldCanvasScale;

        RectTransform previewDummy = FindOrCreateRect(canvasRoot, "PreviewDummy");
        Image previewDummyImage = previewDummy.GetComponent<Image>() ?? previewDummy.gameObject.AddComponent<Image>();
        previewDummy.anchorMin = new Vector2(0.5f, 0.5f);
        previewDummy.anchorMax = new Vector2(0.5f, 0.5f);
        previewDummy.pivot = new Vector2(0.5f, 0.5f);
        previewDummy.anchoredPosition = Vector2.zero;
        previewDummy.sizeDelta = ui.previewDummySize;
        previewDummyImage.color = ui.previewDummyColor;
        previewDummyImage.raycastTarget = false;
        previewDummyImage.preserveAspect = true;

        RectTransform intentRoot = FindOrCreateRect(canvasRoot, "IntentRoot");
        CanvasGroup intentCanvasGroup = intentRoot.GetComponent<CanvasGroup>() ?? intentRoot.gameObject.AddComponent<CanvasGroup>();
        intentRoot.anchorMin = new Vector2(0.5f, 0.5f);
        intentRoot.anchorMax = new Vector2(0.5f, 0.5f);
        intentRoot.pivot = new Vector2(0.5f, 0.5f);
        intentRoot.anchoredPosition = new Vector2(0f, 48f);
        intentRoot.sizeDelta = new Vector2(64f, 48f);

        RectTransform intentIconRect = FindOrCreateRect(intentRoot, "Icon");
        Image intentIcon = intentIconRect.GetComponent<Image>() ?? intentIconRect.gameObject.AddComponent<Image>();
        intentIconRect.anchorMin = new Vector2(0.5f, 0.5f);
        intentIconRect.anchorMax = new Vector2(0.5f, 0.5f);
        intentIconRect.pivot = new Vector2(0.5f, 0.5f);
        intentIconRect.anchoredPosition = new Vector2(0f, 6f);
        intentIconRect.sizeDelta = ui.intentSize;
        intentIcon.raycastTarget = false;
        intentIcon.preserveAspect = true;

        RectTransform intentValueRect = FindOrCreateRect(intentRoot, "Value");
        TextMeshProUGUI intentValueText = intentValueRect.GetComponent<TextMeshProUGUI>() ?? intentValueRect.gameObject.AddComponent<TextMeshProUGUI>();
        SetupText(intentValueText, 12f, TextAlignmentOptions.Center);
        intentValueRect.anchorMin = new Vector2(0.5f, 0f);
        intentValueRect.anchorMax = new Vector2(0.5f, 0f);
        intentValueRect.pivot = new Vector2(0.5f, 0f);
        intentValueRect.anchoredPosition = new Vector2(0f, -2f);
        intentValueRect.sizeDelta = new Vector2(64f, 18f);

        RectTransform hpBarRoot = FindOrCreateRect(canvasRoot, "HpBarRoot");
        hpBarRoot.anchorMin = new Vector2(0.5f, 0.5f);
        hpBarRoot.anchorMax = new Vector2(0.5f, 0.5f);
        hpBarRoot.pivot = new Vector2(0.5f, 0.5f);
        hpBarRoot.anchoredPosition = new Vector2(0f, -40f);
        hpBarRoot.sizeDelta = new Vector2(196f, 20f);

        RectTransform hpBackgroundRect = FindOrCreateRect(hpBarRoot, "Background");
        Image hpBackground = hpBackgroundRect.GetComponent<Image>() ?? hpBackgroundRect.gameObject.AddComponent<Image>();
        Outline hpOutline = hpBackgroundRect.GetComponent<Outline>() ?? hpBackgroundRect.gameObject.AddComponent<Outline>();
        hpBackgroundRect.anchorMin = new Vector2(0.5f, 0.5f);
        hpBackgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
        hpBackgroundRect.pivot = new Vector2(0.5f, 0.5f);
        hpBackgroundRect.anchoredPosition = Vector2.zero;
        hpBackgroundRect.sizeDelta = ui.hpBarSize;
        hpBackground.color = ui.hpBarBackgroundColor;
        hpBackground.raycastTarget = false;
        hpOutline.effectColor = ui.hpOutlineColor;
        hpOutline.effectDistance = new Vector2(1f, -1f);
        hpOutline.useGraphicAlpha = false;

        RectTransform hpFillRect = FindOrCreateRect(hpBackgroundRect, "Fill");
        Image hpFill = hpFillRect.GetComponent<Image>() ?? hpFillRect.gameObject.AddComponent<Image>();
        hpFillRect.anchorMin = Vector2.zero;
        hpFillRect.anchorMax = Vector2.one;
        hpFillRect.offsetMin = Vector2.zero;
        hpFillRect.offsetMax = Vector2.zero;
        hpFill.type = Image.Type.Filled;
        hpFill.fillMethod = Image.FillMethod.Horizontal;
        hpFill.fillOrigin = 0;
        hpFill.fillAmount = 1f;
        hpFill.color = ui.hpFillColor;
        hpFill.raycastTarget = false;

        RectTransform hpTextRect = FindOrCreateRect(hpBackgroundRect, "HpText");
        TextMeshProUGUI hpText = hpTextRect.GetComponent<TextMeshProUGUI>() ?? hpTextRect.gameObject.AddComponent<TextMeshProUGUI>();
        SetupText(hpText, 12f, TextAlignmentOptions.Center);
        hpTextRect.anchorMin = Vector2.zero;
        hpTextRect.anchorMax = Vector2.one;
        hpTextRect.offsetMin = Vector2.zero;
        hpTextRect.offsetMax = Vector2.zero;

        RectTransform guardRoot = FindOrCreateRect(hpBarRoot, "GuardRoot");
        guardRoot.anchorMin = new Vector2(0f, 0.5f);
        guardRoot.anchorMax = new Vector2(0f, 0.5f);
        guardRoot.pivot = new Vector2(0f, 0.5f);
        guardRoot.anchoredPosition = new Vector2(-90f, 0f);
        guardRoot.sizeDelta = new Vector2(42f, 18f);

        RectTransform guardIconRect = FindOrCreateRect(guardRoot, "Icon");
        Image guardIcon = guardIconRect.GetComponent<Image>() ?? guardIconRect.gameObject.AddComponent<Image>();
        guardIconRect.anchorMin = new Vector2(0f, 0.5f);
        guardIconRect.anchorMax = new Vector2(0f, 0.5f);
        guardIconRect.pivot = new Vector2(0f, 0.5f);
        guardIconRect.anchoredPosition = Vector2.zero;
        guardIconRect.sizeDelta = new Vector2(14f, 14f);
        guardIcon.color = new Color(0.78f, 0.9f, 1f, 1f);
        guardIcon.raycastTarget = false;
        guardIcon.preserveAspect = true;

        RectTransform guardValueRect = FindOrCreateRect(guardRoot, "Value");
        TextMeshProUGUI guardValueText = guardValueRect.GetComponent<TextMeshProUGUI>() ?? guardValueRect.gameObject.AddComponent<TextMeshProUGUI>();
        SetupText(guardValueText, 10f, TextAlignmentOptions.MidlineLeft);
        guardValueRect.anchorMin = new Vector2(0f, 0.5f);
        guardValueRect.anchorMax = new Vector2(0f, 0.5f);
        guardValueRect.pivot = new Vector2(0f, 0.5f);
        guardValueRect.anchoredPosition = new Vector2(16f, 0f);
        guardValueRect.sizeDelta = new Vector2(26f, 16f);

        RectTransform statusRow = FindOrCreateRect(canvasRoot, "StatusRow");
        HorizontalLayoutGroup statusLayout = statusRow.GetComponent<HorizontalLayoutGroup>() ?? statusRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        statusRow.anchorMin = new Vector2(0.5f, 0.5f);
        statusRow.anchorMax = new Vector2(0.5f, 0.5f);
        statusRow.pivot = new Vector2(0.5f, 0.5f);
        statusRow.anchoredPosition = new Vector2(0f, -62f);
        statusRow.sizeDelta = new Vector2(184f, 18f);
        statusLayout.spacing = 2f;
        statusLayout.childAlignment = TextAnchor.MiddleCenter;
        statusLayout.childControlHeight = false;
        statusLayout.childControlWidth = false;
        statusLayout.childForceExpandHeight = false;
        statusLayout.childForceExpandWidth = false;

        ActorWorldUI.StatusIconSlot[] slots = new ActorWorldUI.StatusIconSlot[StatusSlotCount];
        for (int i = 0; i < StatusSlotCount; i++)
        {
            RectTransform slotRoot = FindOrCreateRect(statusRow, $"Status_{i + 1}");
            Image slotBackground = slotRoot.GetComponent<Image>() ?? slotRoot.gameObject.AddComponent<Image>();
            LayoutElement slotLayout = slotRoot.GetComponent<LayoutElement>() ?? slotRoot.gameObject.AddComponent<LayoutElement>();
            slotLayout.preferredWidth = ui.statusIconSize.x;
            slotLayout.preferredHeight = ui.statusIconSize.y;
            slotRoot.sizeDelta = ui.statusIconSize;
            slotBackground.color = new Color(0.2f, 0.24f, 0.3f, 0.96f);
            slotBackground.raycastTarget = false;

            RectTransform slotIconRect = FindOrCreateRect(slotRoot, "Icon");
            Image slotIcon = slotIconRect.GetComponent<Image>() ?? slotIconRect.gameObject.AddComponent<Image>();
            slotIconRect.anchorMin = new Vector2(0.5f, 0.5f);
            slotIconRect.anchorMax = new Vector2(0.5f, 0.5f);
            slotIconRect.pivot = new Vector2(0.5f, 0.5f);
            slotIconRect.anchoredPosition = Vector2.zero;
            slotIconRect.sizeDelta = new Vector2(ui.statusIconSize.x - 4f, ui.statusIconSize.y - 4f);
            slotIcon.raycastTarget = false;
            slotIcon.preserveAspect = true;

            RectTransform slotShortRect = FindOrCreateRect(slotRoot, "ShortLabel");
            TextMeshProUGUI slotShort = slotShortRect.GetComponent<TextMeshProUGUI>() ?? slotShortRect.gameObject.AddComponent<TextMeshProUGUI>();
            SetupText(slotShort, 8f, TextAlignmentOptions.Center);
            slotShortRect.anchorMin = Vector2.zero;
            slotShortRect.anchorMax = Vector2.one;
            slotShortRect.offsetMin = Vector2.zero;
            slotShortRect.offsetMax = Vector2.zero;

            RectTransform slotValueRect = FindOrCreateRect(slotRoot, "Value");
            TextMeshProUGUI slotValue = slotValueRect.GetComponent<TextMeshProUGUI>() ?? slotValueRect.gameObject.AddComponent<TextMeshProUGUI>();
            SetupText(slotValue, 8f, TextAlignmentOptions.BottomRight);
            slotValueRect.anchorMin = Vector2.zero;
            slotValueRect.anchorMax = Vector2.one;
            slotValueRect.offsetMin = new Vector2(1f, 1f);
            slotValueRect.offsetMax = new Vector2(-1f, -1f);

            slots[i] = new ActorWorldUI.StatusIconSlot
            {
                root = slotRoot,
                background = slotBackground,
                iconImage = slotIcon,
                shortLabelText = slotShort,
                valueText = slotValue
            };
        }

        ui.worldCanvasRoot = canvasRoot;
        ui.worldCanvas = canvas;
        ui.worldCanvasGroup = canvasGroup;
        ui.previewDummyRoot = previewDummy;
        ui.previewDummyImage = previewDummyImage;
        ui.intentRoot = intentRoot;
        ui.intentCanvasGroup = intentCanvasGroup;
        ui.intentIcon = intentIcon;
        ui.intentValueText = intentValueText;
        ui.hpBarRoot = hpBarRoot;
        ui.hpBarBackground = hpBackground;
        ui.hpBarOutline = hpOutline;
        ui.hpBarFill = hpFill;
        ui.hpText = hpText;
        ui.guardRoot = guardRoot;
        ui.guardIcon = guardIcon;
        ui.guardText = guardValueText;
        ui.statusRowRoot = statusRow;
        ui.statusSlots = slots;
    }

    private static RectTransform FindOrCreateRect(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
            return existing as RectTransform;

        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        return rect;
    }

    private static void SetupText(TextMeshProUGUI text, float fontSize, TextAlignmentOptions alignment)
    {
        text.text = string.Empty;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
    }
}
