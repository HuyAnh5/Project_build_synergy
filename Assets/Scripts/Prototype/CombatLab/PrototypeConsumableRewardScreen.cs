using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class PrototypeConsumableRewardScreen : MonoBehaviour
{
    [SerializeField] private RectTransform root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private ConsumableBarUIManager rewardBar;

    private readonly List<RewardGachaCard> _offerCards = new List<RewardGachaCard>(3);
    private readonly List<ConsumableDataSO> _offerConsumables = new List<ConsumableDataSO>(3);
    private RunInventoryManager _runInventory;
    private System.Action<RewardGachaCard> _onPicked;
    private bool _locked;

    private void Awake()
    {
        if (root == null)
            root = transform as RectTransform;
        if (rewardBar == null)
            rewardBar = GetComponentInChildren<ConsumableBarUIManager>(true);
        if (titleText != null)
            titleText.text = "Choose";
    }

    public void ShowConsumablePrototypeOffer(
        IEnumerable<ConsumableDataSO> rewardPool,
        IEnumerable<ConsumableDataSO> excludedOwned,
        RunInventoryManager inventory,
        System.Action<RewardGachaCard> onPicked)
    {
        _runInventory = inventory;
        _onPicked = onPicked;
        _locked = false;
        _offerCards.Clear();
        _offerConsumables.Clear();

        RewardGachaOffer offer = RewardGachaGenerator.RollConsumableOffer(rewardPool, excludedOwned, choices: 3, picks: 1);
        if (offer != null && offer.cards != null)
        {
            for (int i = 0; i < offer.cards.Count && i < 3; i++)
            {
                RewardGachaCard card = offer.cards[i];
                ConsumableDataSO consumable = card != null ? card.asset as ConsumableDataSO : null;
                if (consumable == null)
                    continue;

                _offerCards.Add(card);
                _offerConsumables.Add(consumable);
            }
        }

        gameObject.SetActive(true);
        if (root != null)
            root.SetAsLastSibling();

        if (_offerCards.Count <= 0)
        {
            Debug.LogWarning("[PrototypeConsumableRewardScreen] No consumables available for reward.", this);
            CloseWithoutPick();
            return;
        }

        if (rewardBar == null)
            rewardBar = GetComponentInChildren<ConsumableBarUIManager>(true);
        if (rewardBar == null)
        {
            Debug.LogWarning("[PrototypeConsumableRewardScreen] Missing reward ConsumableBarUIManager.", this);
            CloseWithoutPick();
            return;
        }

        rewardBar.ShowRewardChoices(_offerConsumables, HandleRewardChoicePicked);
    }

    private void HandleRewardChoicePicked(int index, ConsumableDataSO data)
    {
        PickCard(index);
    }

    private void PickCard(int index)
    {
        if (_locked || index < 0 || index >= _offerCards.Count)
            return;

        _locked = true;
        RewardGachaCard picked = _offerCards[index];
        if (!RewardGachaApplier.TryApply(picked, _runInventory, out string message))
            Debug.LogWarning("[PrototypeConsumableRewardScreen] " + message, this);

        if (rewardBar != null)
            rewardBar.ClearRewardChoices();
        gameObject.SetActive(false);
        _onPicked?.Invoke(picked);
        _onPicked = null;
    }

    private void CloseWithoutPick()
    {
        if (rewardBar != null)
            rewardBar.ClearRewardChoices();
        gameObject.SetActive(false);
        _onPicked?.Invoke(null);
        _onPicked = null;
    }
}

