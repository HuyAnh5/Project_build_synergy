using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class ConsumableBarUIManager
{
    private void EnsureSlotViews()
    {
        if (layoutContainer == null)
        {
            ConsumableSlotView existing = FindFirstUsableSlotView();
            if (existing != null && existing.root != null)
                layoutContainer = existing.root.parent as RectTransform;
        }

        if (layoutContainer == null)
            layoutContainer = transform as RectTransform;

        DisableAutoLayout(layoutContainer);

        int capacity = runInventory != null ? runInventory.ConsumableCapacity : RunInventoryManager.DEFAULT_CONSUMABLE_CAPACITY;
        capacity = Mathf.Clamp(capacity, 1, RunInventoryManager.MAX_CONSUMABLE_CAPACITY);
        if (slots == null)
            slots = new ConsumableSlotView[capacity];
        else if (slots.Length < capacity)
            Array.Resize(ref slots, capacity);

        ConsumableSlotView template = slotTemplatePrefab != null
            ? BuildSlotViewFromRoot(slotTemplatePrefab)
            : FindFirstUsableSlotView();
        for (int i = 0; i < capacity; i++)
        {
            if (slots[i] == null || slots[i].root == null)
            {
                if (!autoCreateMissingCards || template == null || template.root == null || layoutContainer == null)
                    continue;

                RectTransform cloneRoot = Instantiate(template.root, layoutContainer);
                cloneRoot.name = $"ConsumableCard_{i + 1}";
                slots[i] = BuildSlotViewFromRoot(cloneRoot);
            }

            ConfigureSlotView(slots[i], i);
        }
    }

    private ConsumableSlotView FindFirstUsableSlotView()
    {
        if (slots == null)
            return null;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null && slots[i].root != null)
                return slots[i];
        }

        return null;
    }

    private ConsumableSlotView BuildSlotViewFromRoot(RectTransform root)
    {
        if (root == null)
            return null;

        return new ConsumableSlotView
        {
            root = root,
            button = root.GetComponent<Button>(),
            background = root.GetComponent<Image>(),
            icon = FindChildComponent<Image>(root, "Icon"),
            titleText = FindChildComponent<TMP_Text>(root, "Title"),
            chargesText = FindChildComponent<TMP_Text>(root, "Charges"),
            interactionProxy = root.GetComponent<ConsumableSlotInteractionProxy>(),
            actionAnchor = FindChildComponent<RectTransform>(root, "ActionAnchor"),
            tooltipAnchor = FindChildComponent<RectTransform>(root, "TooltipAnchor"),
            localActionPanelRoot = FindChildComponent<RectTransform>(root, "LocalActionPanel"),
            localUseButton = FindChildComponent<Button>(root, "UseButton"),
            localUseButtonText = FindChildComponent<TMP_Text>(root, "UseLabel"),
            localSellButton = FindChildComponent<Button>(root, "SellButton"),
            localSellButtonText = FindChildComponent<TMP_Text>(root, "SellLabel"),
            localTooltipRoot = FindChildComponent<RectTransform>(root, "LocalTooltip"),
            localTooltipTitleText = FindChildComponent<TMP_Text>(root, "TooltipTitle"),
            localTooltipBodyText = FindChildComponent<TMP_Text>(root, "TooltipBody")
        };
    }

    private static T FindChildComponent<T>(Transform root, string childName) where T : Component
    {
        if (root == null)
            return null;

        Transform match = FindDescendantByName(root, childName);
        return match != null && match.TryGetComponent(out T component) ? component : null;
    }

    private static Transform FindDescendantByName(Transform root, string childName)
    {
        if (root == null || string.IsNullOrEmpty(childName))
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == childName)
                return child;

            Transform nested = FindDescendantByName(child, childName);
            if (nested != null)
                return nested;
        }

        return null;
    }

    private void ConfigureSlotView(ConsumableSlotView slot, int index)
    {
        if (slot == null || slot.root == null)
            return;

        slot.root.anchorMin = slot.root.anchorMax = new Vector2(0.5f, 0.5f);
        slot.root.pivot = new Vector2(0.5f, 0.5f);
        slot.root.sizeDelta = cardSize;

        LayoutElement layout = slot.root.GetComponent<LayoutElement>();
        if (layout == null)
            layout = slot.root.gameObject.AddComponent<LayoutElement>();
        layout.ignoreLayout = true;
        layout.preferredWidth = cardSize.x;
        layout.preferredHeight = cardSize.y;

        if (slot.interactionProxy == null)
            slot.interactionProxy = slot.root.GetComponent<ConsumableSlotInteractionProxy>();
        if (slot.interactionProxy == null)
            slot.interactionProxy = slot.root.gameObject.AddComponent<ConsumableSlotInteractionProxy>();

        slot.interactionProxy.manager = this;
        slot.interactionProxy.slotIndex = index;

        EnsurePresentationAnchors(slot);

        if (slot.button != null && slot.background != null)
            slot.button.targetGraphic = slot.background;

        if (slot.localUseButton != null)
        {
            slot.localUseButton.onClick.RemoveAllListeners();
            slot.localUseButton.onClick.AddListener(UseSelectedConsumable);
        }

        if (slot.localSellButton != null)
        {
            slot.localSellButton.onClick.RemoveAllListeners();
            slot.localSellButton.onClick.AddListener(SellSelectedConsumable);
        }
    }

    private void EnsurePresentationAnchors(ConsumableSlotView slot)
    {
        if (slot == null || slot.root == null)
            return;

        if (slot.actionAnchor == null)
        {
            Transform existing = slot.root.Find("ActionAnchor");
            if (existing != null)
            {
                slot.actionAnchor = existing as RectTransform;
            }
            else
            {
                GameObject go = new GameObject("ActionAnchor", typeof(RectTransform));
                RectTransform rect = go.GetComponent<RectTransform>();
                rect.SetParent(slot.root, false);
                rect.anchorMin = new Vector2(1f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0f, 0.5f);
                rect.anchoredPosition = new Vector2(cardSize.x * 0.5f + 8f, -22f);
                rect.sizeDelta = Vector2.zero;
                slot.actionAnchor = rect;
            }
        }

        if (slot.tooltipAnchor == null)
        {
            Transform existing = slot.root.Find("TooltipAnchor");
            if (existing != null)
            {
                slot.tooltipAnchor = existing as RectTransform;
            }
            else
            {
                GameObject go = new GameObject("TooltipAnchor", typeof(RectTransform));
                RectTransform rect = go.GetComponent<RectTransform>();
                rect.SetParent(slot.root, false);
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, -8f);
                rect.sizeDelta = Vector2.zero;
                slot.tooltipAnchor = rect;
            }
        }
    }

    private static void DisableAutoLayout(RectTransform container)
    {
        if (container == null)
            return;

        HorizontalLayoutGroup horizontal = container.GetComponent<HorizontalLayoutGroup>();
        if (horizontal != null)
            horizontal.enabled = false;

        VerticalLayoutGroup vertical = container.GetComponent<VerticalLayoutGroup>();
        if (vertical != null)
            vertical.enabled = false;

        GridLayoutGroup grid = container.GetComponent<GridLayoutGroup>();
        if (grid != null)
            grid.enabled = false;

        ContentSizeFitter fitter = container.GetComponent<ContentSizeFitter>();
        if (fitter != null)
            fitter.enabled = false;
    }
}


