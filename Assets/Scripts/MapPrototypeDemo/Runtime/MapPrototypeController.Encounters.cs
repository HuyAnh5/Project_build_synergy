using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class MapPrototypeController
{
    private sealed class ModalAction
    {
        public string label;
        public bool danger;
        public bool disabled;
        public Action handler;
    }

    private void MaybeOpenEncounter(MapPrototypeNodeData node)
    {
        LogMap($"MaybeOpenEncounter for node {node.id} ({node.type}). Cleared={node.cleared}, SafeVisited={node.safeVisited}, HasHint={node.hasHint}, HintTaken={node.hintTaken}.");

        if (node.type == MapPrototypeNodeType.Start)
        {
            CloseModal();
            RenderAll();
            NotifyMapStateChanged();
            return;
        }

        if (MapPrototypeNodeCatalog.IsHostile(node.type) && !node.cleared)
        {
            OpenHostileModal(node);
            return;
        }

        if (node.type == MapPrototypeNodeType.Shop)
        {
            node.safeVisited = true;
            ComputeTravelOptions();
            RenderAll();
            NotifyMapStateChanged();
            OpenShopModal(node);
            return;
        }

        if (node.type == MapPrototypeNodeType.Forge)
        {
            node.safeVisited = true;
            ComputeTravelOptions();
            RenderAll();
            NotifyMapStateChanged();
            OpenForgeModal(node);
            return;
        }

        if (!MapPrototypeNodeCatalog.IsHostile(node.type) && !node.safeVisited)
        {
            node.safeVisited = true;
            node.cleared = true;
            CollectHintIfAny(node);
            ComputeTravelOptions();
            RenderAll();
            NotifyMapStateChanged();
            OpenPassiveModal(node);
            return;
        }

        CloseModal();
        RenderAll();
        NotifyMapStateChanged();
    }

    private void OpenHostileModal(MapPrototypeNodeData node)
    {
        NotifyHostileNodeOpened(node);

        string title = node.type == MapPrototypeNodeType.Boss
            ? (_bossRevealed && node.bossData != null ? node.bossData.bossName : "Unknown Boss")
            : MapPrototypeNodeCatalog.GetLabel(node.type);

        string body = node.type == MapPrototypeNodeType.Boss && _bossRevealed && node.bossData != null
            ? node.bossData.description
            : MapPrototypeNodeCatalog.GetDescription(node.type);

        if (node.hasHint && !node.hintTaken)
            body += " This node is holding one boss hint if you fight and clear it.";
        if (node.ranSkipped)
            body += " You already ran from this node once, so it still blocks safe backtrack.";

        List<ModalAction> actions = new List<ModalAction>();
        if (node.type != MapPrototypeNodeType.Boss)
        {
            actions.Add(new ModalAction
            {
                label = "Run",
                handler = () => ResolveRun(node)
            });
        }

        actions.Add(new ModalAction
        {
            label = node.type == MapPrototypeNodeType.Boss ? "Fight Boss" : "Fight",
            danger = true,
            handler = () => ResolveFight(node)
        });

        ShowModal(
            MapPrototypeNodeCatalog.GetBadge(node.type, _bossRevealed, node.bossData),
            title,
            body,
            actions);
    }

    private void OpenShopModal(MapPrototypeNodeData node)
    {
        string body = MapPrototypeNodeCatalog.GetDescription(node.type);
        if (node.shopHintBought)
            body += " You already bought the hint from this shop.";
        else if (node.hasHint && !node.hintTaken)
            body += " Hint is not auto-collected here. You must press Buy Hint.";

        ShowModal("SH", "Shop", body, new List<ModalAction>
        {
            new ModalAction
            {
                label = node.shopHintBought ? "Hint Bought" : "Buy Hint",
                disabled = node.shopHintBought || !node.hasHint || node.hintTaken,
                handler = () => BuyShopHint(node)
            },
            new ModalAction
            {
                label = "Leave Shop",
                handler = CloseModal
            }
        });
    }

    private void OpenForgeModal(MapPrototypeNodeData node)
    {
        string body = MapPrototypeNodeCatalog.GetDescription(node.type)
            + " Forge does not disappear after the first visit; it becomes a safe landmark.";

        ShowModal("FG", "Forge", body, new List<ModalAction>
        {
            new ModalAction
            {
                label = "Leave Forge",
                handler = CloseModal
            }
        });
    }

    private void OpenPassiveModal(MapPrototypeNodeData node)
    {
        string body = MapPrototypeNodeCatalog.GetDescription(node.type)
            + " After the first visit this node becomes a safe path node.";
        if (node.hasHint && node.hintTaken)
            body += " You collected one boss hint here.";

        ShowModal(
            MapPrototypeNodeCatalog.GetBadge(node.type, false, null),
            MapPrototypeNodeCatalog.GetLabel(node.type),
            body,
            new List<ModalAction>
            {
                new ModalAction
                {
                    label = "Continue",
                    handler = CloseModal
                }
            });
    }

    private void ShowModal(string icon, string title, string body, List<ModalAction> actions)
    {
        _modalLocked = true;
        LogMap($"ShowModal: {title} ({actions.Count} actions).");
        if (modalIconText != null) modalIconText.text = icon;
        if (modalTitleText != null) modalTitleText.text = title;
        if (modalBodyText != null) modalBodyText.text = body;

        ClearChildren(modalActionsRoot);
        foreach (ModalAction action in actions)
        {
            Button button = MapPrototypeUIFactory.CreateButton(
                "ModalButton",
                modalActionsRoot,
                action.label,
                action.danger ? DangerColor : new Color32(109, 78, 50, 235),
                InkColor,
                20);
            MapPrototypeUIFactory.AddLayoutElement(button.gameObject, preferredWidth: 120f, preferredHeight: 40f);
            button.interactable = !action.disabled;
            button.onClick.AddListener(() => action.handler?.Invoke());
        }

        if (modalCanvasGroup != null)
        {
            modalCanvasGroup.alpha = 1f;
            modalCanvasGroup.interactable = true;
            modalCanvasGroup.blocksRaycasts = true;
        }
    }

    private void CloseModal()
    {
        _modalLocked = false;
        if (modalCanvasGroup != null)
        {
            modalCanvasGroup.alpha = 0f;
            modalCanvasGroup.interactable = false;
            modalCanvasGroup.blocksRaycasts = false;
        }

        LogMap("CloseModal.");
    }

    private void ResolveRun(MapPrototypeNodeData node)
    {
        LogMap($"ResolveRun on node {node.id} ({node.type}).");
        node.visited = true;
        node.ranSkipped = true;
        CloseModal();
        ComputeTravelOptions();
        RenderAll();
        NotifyHostileNodeResolved(node, RunCombatResult.PlayerFled);
        NotifyMapStateChanged();
    }

    private void ResolveFight(MapPrototypeNodeData node)
    {
        LogMap($"ResolveFight on node {node.id} ({node.type}).");
        node.visited = true;
        node.safeVisited = true;
        node.cleared = true;
        node.ranSkipped = false;
        CollectHintIfAny(node);
        CloseModal();
        ComputeTravelOptions();
        RenderAll();
        NotifyHostileNodeResolved(node, RunCombatResult.Victory);
        NotifyMapStateChanged();

        if (node.type == MapPrototypeNodeType.Boss)
        {
            ShowModal("CL", "Act Clear", "Prototype ends here. You cleared the boss node for this act.", new List<ModalAction>
            {
                new ModalAction { label = "Close", handler = CloseModal },
                new ModalAction { label = "Start Over", danger = true, handler = ResetAct }
            });
        }
    }

    private void CollectHintIfAny(MapPrototypeNodeData node)
    {
        if (!node.hasHint || node.hintTaken)
            return;

        node.hintTaken = true;
        _hintsCollected = Mathf.Min(config.bossHintsRequired, _hintsCollected + 1);
    }

    private void BuyShopHint(MapPrototypeNodeData node)
    {
        if (node.shopHintBought)
        {
            LogMap($"BuyShopHint ignored; already bought on node {node.id}.");
            return;
        }

        node.shopHintBought = true;
        CollectHintIfAny(node);
        ComputeTravelOptions();
        RenderAll();
        NotifyMapStateChanged();
        OpenShopModal(node);
        LogMap($"Bought shop hint on node {node.id}. Total hints={_hintsCollected}.");
    }
}
