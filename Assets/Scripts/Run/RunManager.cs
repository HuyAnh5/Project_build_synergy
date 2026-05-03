using System;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public sealed class RunManager : MonoBehaviour
{
    [Header("Runtime Links")]
    [SerializeField] private RunInventoryManager runInventory;
    [SerializeField] private MapPrototypeController mapController;
    [SerializeField] private CombatActor playerActor;

    [Header("New Run Defaults")]
    [SerializeField] private int startingAct = 1;
    [SerializeField] private int startingMaxHp = 30;
    [SerializeField] private bool startNewRunOnStart;

    [Header("Runtime State")]
    [SerializeField, FormerlySerializedAs("progress")] private RunState state = new RunState();

    public event Action<RunState> StateChanged;

    [Obsolete("Use StateChanged instead.")]
    public event Action<RunState> ProgressChanged;

    public RunState State => state;

    [Obsolete("Use State instead.")]
    public RunState Progress => state;

    public RunInventoryManager Inventory => runInventory;
    public MapPrototypeController MapController => mapController;
    public CombatActor PlayerActor => playerActor;

    private void Awake()
    {
        ResolveLinks();
        EnsureState();
    }

    private void OnEnable()
    {
        ResolveLinks();
        SubscribeMap();
        SubscribeInventory();
    }

    private void OnDisable()
    {
        UnsubscribeMap();
        UnsubscribeInventory();
    }

    private void Start()
    {
        if (startNewRunOnStart)
            StartNewRun();
        else
            SyncFromScene();
    }

    [ContextMenu("Start New Run")]
    public void StartNewRun()
    {
        EnsureState();
        int maxHp = playerActor != null ? playerActor.maxHP : startingMaxHp;
        state.ResetForNewRun(startingAct, maxHp);

        if (playerActor != null)
        {
            playerActor.maxHP = state.PlayerMaxHp;
            playerActor.hp = state.PlayerHp;
        }

        if (mapController != null)
        {
            mapController.ResetAct();
            mapController.WriteStateTo(state);
        }

        SyncInventoryFromManager();
        NotifyStateChanged();
    }

    [ContextMenu("Sync From Scene")]
    public void SyncFromScene()
    {
        EnsureState();
        SyncPlayerFromActor();
        SyncInventoryFromManager();

        if (mapController != null)
            mapController.WriteStateTo(state);

        NotifyStateChanged();
    }

    [ContextMenu("Apply State To Scene")]
    public void ApplyStateToScene()
    {
        EnsureState();

        if (playerActor != null)
        {
            playerActor.maxHP = state.PlayerMaxHp;
            playerActor.hp = state.PlayerHp;
        }

        if (runInventory != null)
            runInventory.ApplyState(state, notifyChanged: false);

        NotifyStateChanged();
    }

    public void BeginEncounter(string nodeId, MapPrototypeNodeType nodeType)
    {
        EnsureState();
        state.BeginEncounter(nodeId, nodeType);
        NotifyStateChanged();
    }

    public void CompleteEncounter(RunCombatResult result)
    {
        EnsureState();
        SyncPlayerFromActor();
        SyncInventoryFromManager();
        state.CompleteEncounter(result);

        if (mapController != null)
            mapController.WriteStateTo(state);

        NotifyStateChanged();
    }

    public void AdvanceAct()
    {
        EnsureState();
        state.AdvanceAct();
        NotifyStateChanged();
    }

    private void HandleMapStateChanged(MapPrototypeController controller)
    {
        EnsureState();
        controller.WriteStateTo(state);
        NotifyStateChanged();
    }

    private void HandleHostileNodeOpened(MapPrototypeNodeData node)
    {
        if (node == null)
            return;

        BeginEncounter(node.id, node.type);
    }

    private void HandleHostileNodeResolved(MapPrototypeNodeData node, RunCombatResult result)
    {
        CompleteEncounter(result);
    }

    private void HandleInventoryChanged()
    {
        EnsureState();
        SyncInventoryFromManager();
        NotifyStateChanged();
    }

    private void SyncPlayerFromActor()
    {
        if (playerActor == null)
            return;

        state.SetPlayerHp(playerActor.hp, playerActor.maxHP);
    }

    private void SyncInventoryFromManager()
    {
        if (runInventory == null)
            return;

        runInventory.WriteStateTo(state);
    }

    private void ResolveLinks()
    {
        if (runInventory == null)
            runInventory = GetComponentInChildren<RunInventoryManager>(true);
        if (runInventory == null)
            runInventory = FindFirstObjectByType<RunInventoryManager>(FindObjectsInactive.Include);

        if (mapController == null)
            mapController = GetComponentInChildren<MapPrototypeController>(true);
        if (mapController == null)
            mapController = FindFirstObjectByType<MapPrototypeController>(FindObjectsInactive.Include);

        if (playerActor == null)
            playerActor = GetComponentInChildren<CombatActor>(true);
        if (playerActor == null)
        {
            CombatActor[] actors = FindObjectsByType<CombatActor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < actors.Length; i++)
            {
                if (actors[i] != null && actors[i].isPlayer)
                {
                    playerActor = actors[i];
                    break;
                }
            }
        }
    }

    private void SubscribeMap()
    {
        if (mapController == null)
            return;

        mapController.MapStateChanged -= HandleMapStateChanged;
        mapController.HostileNodeOpened -= HandleHostileNodeOpened;
        mapController.HostileNodeResolved -= HandleHostileNodeResolved;
        mapController.MapStateChanged += HandleMapStateChanged;
        mapController.HostileNodeOpened += HandleHostileNodeOpened;
        mapController.HostileNodeResolved += HandleHostileNodeResolved;
    }

    private void UnsubscribeMap()
    {
        if (mapController == null)
            return;

        mapController.MapStateChanged -= HandleMapStateChanged;
        mapController.HostileNodeOpened -= HandleHostileNodeOpened;
        mapController.HostileNodeResolved -= HandleHostileNodeResolved;
    }

    private void SubscribeInventory()
    {
        if (runInventory == null)
            return;

        runInventory.InventoryChanged -= HandleInventoryChanged;
        runInventory.InventoryChanged += HandleInventoryChanged;
    }

    private void UnsubscribeInventory()
    {
        if (runInventory == null)
            return;

        runInventory.InventoryChanged -= HandleInventoryChanged;
    }

    private void EnsureState()
    {
        if (state == null)
            state = new RunState();

        state.EnsureNestedState();
    }

    private void NotifyStateChanged()
    {
        StateChanged?.Invoke(state);
        ProgressChanged?.Invoke(state);
    }
}
