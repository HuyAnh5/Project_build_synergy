using UnityEngine;

[DefaultExecutionOrder(-450)]
[DisallowMultipleComponent]
public sealed class PrototypeLinearMapFlowController : MonoBehaviour
{
    [SerializeField] private CombatLabPrototypeController combatController;
    [SerializeField] private MapPrototypeController mapController;
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private bool grantOpeningReward;
    [SerializeField] private bool grantRewardAfterBoss;
    [SerializeField] private bool grantPreCombatOneReward = true;

    private MapPrototypeNodeData _activeNode;
    private bool _subscribed;
    private bool _pendingPreCombatReward;

    private void Awake()
    {
        AutoResolveReferences();
    }

    private void OnEnable()
    {
        AutoResolveReferences();
        Subscribe();
    }

    private void Start()
    {
        AutoResolveReferences();

        if (combatController != null)
            combatController.PreparePrototypeRun();

        if (mapController != null)
            mapController.ResetAct();

        if (grantOpeningReward && combatController != null)
        {
            combatController.ShowConsumableReward(ShowMapScreen);
        }
        else
        {
            ShowMapScreen();
        }
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void AutoResolveReferences()
    {
        if (combatController == null)
            combatController = FindFirstObjectByType<CombatLabPrototypeController>(FindObjectsInactive.Include);
        if (mapController == null)
            mapController = FindFirstObjectByType<MapPrototypeController>(FindObjectsInactive.Include);
        if (turnManager == null)
            turnManager = FindFirstObjectByType<TurnManager>(FindObjectsInactive.Include);
    }

    private void Subscribe()
    {
        if (_subscribed)
            return;

        if (mapController != null)
            mapController.HostileNodeOpened += HandleHostileNodeOpened;
        if (turnManager != null)
            turnManager.CombatVictoryResolved += HandleCombatVictoryResolved;
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed)
            return;

        if (mapController != null)
            mapController.HostileNodeOpened -= HandleHostileNodeOpened;
        if (turnManager != null)
            turnManager.CombatVictoryResolved -= HandleCombatVictoryResolved;
        _subscribed = false;
    }

    private void HandleHostileNodeOpened(MapPrototypeNodeData node)
    {
        if (node == null || combatController == null)
            return;

        _activeNode = node;
        HideMapScreen();
        _pendingPreCombatReward = node.encounterIndex == 0 && grantPreCombatOneReward;
        if (_pendingPreCombatReward)
        {
            combatController.ShowConsumableReward(BeginActiveCombat);
            return;
        }

        BeginActiveCombat();
    }

    private void HandleCombatVictoryResolved()
    {
        if (_activeNode == null)
            return;

        if (mapController != null)
            mapController.ResolveExternalHostileNode(_activeNode.id, RunCombatResult.Victory);

        bool isBoss = _activeNode.type == MapPrototypeNodeType.Boss;
        _activeNode = null;

        if (combatController == null)
        {
            ShowMapScreen();
            return;
        }

        if (isBoss && !grantRewardAfterBoss)
        {
            ShowMapScreen();
            return;
        }

        combatController.ShowConsumableReward(ShowMapScreen);
    }

    private void ShowMapScreen()
    {
        if (mapController != null)
            mapController.gameObject.SetActive(true);
    }

    private void BeginActiveCombat()
    {
        if (_activeNode == null || combatController == null)
            return;

        _pendingPreCombatReward = false;
        combatController.StartCombatAtIndex(Mathf.Max(0, _activeNode.encounterIndex));
    }

    private void HideMapScreen()
    {
        if (mapController != null)
            mapController.gameObject.SetActive(false);
    }
}
