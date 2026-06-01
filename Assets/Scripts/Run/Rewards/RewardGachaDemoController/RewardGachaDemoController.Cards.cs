using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Renders reward cards and handles card selection/lock behavior.
public sealed partial class RewardGachaDemoController
{
    // Adds one mode button and wires it to the current demo mode.
    private void AddModeButton(Transform parent, string label, RewardGachaEncounterMode mode)
    {
        Button button = CreateSmallButton(parent, label);
        Image image = button.GetComponent<Image>();
        _modeButtons.Add(button);
        _modeButtonImages.Add(image);
        button.onClick.AddListener(() => SetMode(mode));
    }

    // Rebuilds card buttons for the current generated offer.
    private void RenderCards()
    {
        if (_cardsRoot == null)
            return;

        ClearChildren(_cardsRoot);
        if (_offer == null || _offer.cards == null)
            return;

        for (int i = 0; i < _offer.cards.Count; i++)
        {
            int index = i;
            RewardGachaCard card = _offer.cards[i];
            Button button = CreateCard(_cardsRoot, card, index);
            button.onClick.AddListener(() => ToggleCard(index));
        }
    }

    // Creates one visual reward card button.
    private Button CreateCard(Transform parent, RewardGachaCard card, int index)
    {
        Image image = MapPrototypeUIFactory.CreateImage("Card" + index, parent, PanelInnerColor, true);
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        MapPrototypeUIFactory.AddLayoutElement(image.gameObject, preferredHeight: 224f, flexibleWidth: 1f);

        VerticalLayoutGroup layout = GetOrAdd<VerticalLayoutGroup>(image.gameObject);
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.spacing = 7f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        Image stripe = MapPrototypeUIFactory.CreateImage("RarityStripe", image.transform, RewardGachaGenerator.GetRarityColor(card.rarity), false);
        MapPrototypeUIFactory.AddLayoutElement(stripe.gameObject, preferredHeight: 7f);

        TextMeshProUGUI rarity = MapPrototypeUIFactory.CreateText("Rarity", image.transform, RewardGachaGenerator.GetRarityLabel(card.rarity), 13, FontStyles.Bold, RewardGachaGenerator.GetRarityColor(card.rarity), TextAlignmentOptions.Left);
        MapPrototypeUIFactory.AddLayoutElement(rarity.gameObject, preferredHeight: 22f);

        TextMeshProUGUI title = MapPrototypeUIFactory.CreateText("Title", image.transform, card.displayName, 20, FontStyles.Bold, TextColor, TextAlignmentOptions.Left);
        MapPrototypeUIFactory.AddLayoutElement(title.gameObject, preferredHeight: 54f);

        TextMeshProUGUI purpose = MapPrototypeUIFactory.CreateText(
            "Purpose",
            image.transform,
            RewardGachaGenerator.GetPurposeLabel(card.purpose) + " / " + card.itemKind,
            14,
            FontStyles.Normal,
            MutedTextColor,
            TextAlignmentOptions.Left);
        MapPrototypeUIFactory.AddLayoutElement(purpose.gameObject, preferredHeight: 32f);

        string description = string.IsNullOrWhiteSpace(card.description) ? "No description yet." : card.description;
        TextMeshProUGUI body = MapPrototypeUIFactory.CreateText("Description", image.transform, description, 13, FontStyles.Normal, MutedTextColor, TextAlignmentOptions.TopLeft);
        MapPrototypeUIFactory.AddLayoutElement(body.gameObject, flexibleHeight: 1f);

        RefreshCardVisual(image, index);
        return button;
    }

    // Toggles a card and locks the roll once enough picks are selected.
    private void ToggleCard(int index)
    {
        if (_locked || _offer == null || index < 0 || index >= _offer.cards.Count)
            return;

        if (_selectedIndexes.Contains(index))
            _selectedIndexes.Remove(index);
        else
        {
            if (_selectedIndexes.Count >= _offer.picksAllowed)
                return;
            _selectedIndexes.Add(index);
        }

        RefreshHeader();
        RefreshCardsOnly();

        if (_selectedIndexes.Count >= _offer.picksAllowed)
            LockSelection();
    }

    // Applies selected cards or records demo-only selection messages.
    private void LockSelection()
    {
        _locked = true;
        List<string> messages = new List<string>();
        foreach (int index in _selectedIndexes)
        {
            RewardGachaCard card = _offer.cards[index];
            if (applySelectionToInventory)
            {
                RewardGachaApplier.TryApply(card, runInventory, out string message);
                messages.Add(message);
            }
            else
            {
                messages.Add("Selected " + card.displayName + ".");
            }
        }

        SetStatus(string.Join(" ", messages));
        if (autoRerollAfterPick)
            _rerollRoutine = StartCoroutine(AutoRerollRoutine());
    }

    // Delays the next roll after an automatic pick.
    private IEnumerator AutoRerollRoutine()
    {
        yield return new WaitForSeconds(Mathf.Max(0.1f, autoRerollDelay));
        _rerollRoutine = null;
        RollRewards();
    }

    // Refreshes selection visuals without rebuilding card GameObjects.
    private void RefreshCardsOnly()
    {
        if (_cardsRoot == null)
            return;

        for (int i = 0; i < _cardsRoot.childCount; i++)
        {
            Image image = _cardsRoot.GetChild(i).GetComponent<Image>();
            if (image != null)
                RefreshCardVisual(image, i);
        }
    }

    // Applies selected/unselected color to one card.
    private void RefreshCardVisual(Image image, int index)
    {
        bool selected = _selectedIndexes.Contains(index);
        image.color = selected ? SelectedCardColor : PanelInnerColor;
    }
}
