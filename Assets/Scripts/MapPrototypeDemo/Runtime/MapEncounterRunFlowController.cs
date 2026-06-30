using UnityEngine;

[DefaultExecutionOrder(-440)]
[DisallowMultipleComponent]
public sealed class MapEncounterRunFlowController : MonoBehaviour
{
    [SerializeField] private CombatLabPrototypeController combatController;
    [SerializeField] private MapPrototypeController mapController;
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private bool prepareCombatLoadoutOnStart = true;
    [SerializeField] private bool resetMapOnStart = true;
    [SerializeField] private bool grantRewardAfterCombat = true;
    [SerializeField] private bool grantRewardAfterBoss;

    private MapPrototypeNodeData _activeNode;
    private bool _subscribed;

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

        if (prepareCombatLoadoutOnStart && combatController != null)
            combatController.PreparePrototypeRun();

        if (resetMapOnStart && mapController != null)
            mapController.ResetAct();

        ShowMapScreen();
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

        if (node.encounterDefinition != null)
        {
            combatController.StartCombat(node.encounterDefinition);
            return;
        }

        Debug.LogWarning($"[MapEncounterRunFlowController] Node {node.id} has no encounter definition. Falling back to prototype index.", this);
        combatController.StartCombatAtIndex(Mathf.Max(0, node.encounterIndex));
    }

    private void HandleCombatVictoryResolved()
    {
        if (_activeNode == null)
            return;

        bool isBoss = _activeNode.type == MapPrototypeNodeType.Boss;
        if (mapController != null)
            mapController.ResolveExternalHostileNode(_activeNode.id, RunCombatResult.Victory);

        _activeNode = null;

        if (combatController != null && grantRewardAfterCombat && (!isBoss || grantRewardAfterBoss))
        {
            combatController.ShowConsumableReward(ShowMapScreen);
            return;
        }

        ShowMapScreen();
    }

    private void ShowMapScreen()
    {
        if (mapController != null)
            mapController.gameObject.SetActive(true);
    }

    private void HideMapScreen()
    {
        if (mapController != null)
            mapController.gameObject.SetActive(false);
    }
}
