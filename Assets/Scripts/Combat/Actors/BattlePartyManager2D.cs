using System;
using System.Collections.Generic;
using UnityEngine;

public class BattlePartyManager2D : MonoBehaviour
{
    [Serializable]
    public class SpawnSlot
    {
        public string label;
        public CombatActor prefab;
        public CombatActor.RowTag row = CombatActor.RowTag.Front;
        public int orderInRow = 0;
    }

    [Header("Limits")]
    public int maxAllies = 3;
    public int maxEnemies = 3;

    [Header("Anchors (world)")]
    public Transform alliesAnchor;
    public Transform enemiesAnchor;

    [Header("Player (Prefab ONLY)")]
    public CombatActor playerPrefab;
    public CombatActor.RowTag playerRow = CombatActor.RowTag.Front;

    public CombatActor Player { get; private set; }   // runtime player instance

    [Header("Initial Spawn Slots (optional)")]
    public bool spawnOnStart = true;
    public bool clearExistingOnSpawn = true;
    public SpawnSlot[] allySlots = new SpawnSlot[3];
    public SpawnSlot[] enemySlots = new SpawnSlot[3];

    [Header("Layout (WORLD UNITS)")]
    public float depthOffset = 0.6f;
    public float spacingFor2 = 1.2f;
    public float spacingFor3 = 0.9f;

    [Header("Depth Visual (optional)")]
    public bool applyScaleByRow = true;
    public float frontScale = 1.0f;
    public float backScale = 0.92f;

    [Header("World UI")]
    public ActorWorldUI worldUiPrefab;
    public bool autoSpawnWorldUI = true;

    [Header("Battle Reset Rule (prototype)")]
    public bool resetHPOnBattleStart = true;

    [Header("Wiring")]
    public TurnManager turnManager; // optional; auto-find if null

    private readonly List<CombatActor> _allies = new(3);   // non-player allies
    private readonly List<CombatActor> _enemies = new(3);
    private readonly Dictionary<CombatActor, ActorWorldUI> _uiMap = new();
    private readonly Dictionary<CombatActor, int> _spawnOrder = new();
    private int _spawnSerial = 0;
    private bool _spawnedOnce = false;

    public IReadOnlyList<CombatActor> AlliesNonPlayer => _allies;
    public IReadOnlyList<CombatActor> Enemies => _enemies;

    public event Action onRosterChanged;

    void Awake()
    {
        if (turnManager == null)
            turnManager = GetComponent<TurnManager>() ?? FindObjectOfType<TurnManager>(true);

        if (spawnOnStart)
            EnsureSpawned();
    }

    [ContextMenu("Ensure Spawned")]
    public void EnsureSpawned()
    {
        if (_spawnedOnce) return;
        _spawnedOnce = true;
        SpawnInitialEncounter();
    }

    [ContextMenu("Spawn Initial Encounter")]
    public void SpawnInitialEncounter()
    {
        if (clearExistingOnSpawn)
            ClearAll(destroyObjects: true);

        EnsurePlayerExists();

        SpawnFromSlots(allySlots, CombatActor.TeamSide.Ally);
        SpawnFromSlots(enemySlots, CombatActor.TeamSide.Enemy);

        if (autoSpawnWorldUI)
            EnsureWorldUIForAll();

        LayoutAll();
        onRosterChanged?.Invoke();
    }

    private void EnsurePlayerExists()
    {
        if (Player != null) return;

        if (playerPrefab == null)
        {
            Debug.LogError("[BattlePartyManager2D] playerPrefab is NULL. Assign Player Prefab.", this);
            return;
        }

        Player = SpawnActor(playerPrefab, CombatActor.TeamSide.Ally, playerRow, isPlayer: true, addToRoster: false);
        if (Player != null)
        {
            Player.team = CombatActor.TeamSide.Ally;
            Player.row = playerRow;
            Player.isPlayer = true;
        }
    }

    private void SpawnFromSlots(SpawnSlot[] slots, CombatActor.TeamSide side)
    {
        if (slots == null) return;

        var temp = new List<SpawnSlot>();
        foreach (var s in slots)
            if (s != null && s.prefab != null) temp.Add(s);

        temp.Sort((a, b) => a.orderInRow.CompareTo(b.orderInRow));

        foreach (var s in temp)
            SpawnActor(s.prefab, side, s.row, isPlayer: false, addToRoster: true);
    }

    public CombatActor SpawnActor(
        CombatActor prefab,
        CombatActor.TeamSide side,
        CombatActor.RowTag row,
        bool isPlayer,
        bool addToRoster,
        bool resetForBattleState = true)
    {
        if (prefab == null) return null;

        if (side == CombatActor.TeamSide.Ally && addToRoster && CountAlliesIncludingPlayer() >= maxAllies)
        {
            Debug.LogWarning("[BattlePartyManager2D] Allies full. Spawn ignored.", this);
            return null;
        }
        if (side == CombatActor.TeamSide.Enemy && addToRoster && _enemies.Count >= maxEnemies)
        {
            Debug.LogWarning("[BattlePartyManager2D] Enemies full. Spawn ignored.", this);
            return null;
        }

        Transform anchor = (side == CombatActor.TeamSide.Ally) ? alliesAnchor : enemiesAnchor;
        Vector3 spawnPos = anchor ? anchor.position : Vector3.zero;

        var go = Instantiate(prefab.gameObject, spawnPos, Quaternion.identity);
        var actor = go.GetComponent<CombatActor>();
        if (!actor)
        {
            Debug.LogError("[BattlePartyManager2D] Prefab missing CombatActor.", this);
            Destroy(go);
            return null;
        }

        actor.team = side;
        actor.row = row;
        actor.isPlayer = isPlayer;

        _spawnOrder[actor] = _spawnSerial++;

        if (addToRoster)
        {
            if (side == CombatActor.TeamSide.Ally) _allies.Add(actor);
            else _enemies.Add(actor);
        }

        if (resetForBattleState)
            actor.ResetForBattle(resetHPOnBattleStart);

        WireClickable(actor);

        if (autoSpawnWorldUI)
            EnsureWorldUIFor(actor);

        LayoutAll();
        onRosterChanged?.Invoke();
        return actor;
    }

    private void WireClickable(CombatActor actor)
    {
        if (actor == null) return;

        // Auto-assign TurnManager to all TargetClickable2D under this actor
        if (turnManager == null)
            turnManager = GetComponent<TurnManager>() ?? FindObjectOfType<TurnManager>(true);

        var clickables = actor.GetComponentsInChildren<TargetClickable2D>(true);
        foreach (var c in clickables)
            if (c) c.turn = turnManager;
    }

    public int CountAlliesIncludingPlayer()
    {
        int c = _allies.Count;
        if (Player != null) c += 1;
        return c;
    }

    public List<CombatActor> GetAliveEnemies(bool frontOnly = false)
    {
        var list = new List<CombatActor>(maxEnemies);
        foreach (var e in _enemies)
        {
            if (!e || e.IsDead) continue;
            if (frontOnly && e.row != CombatActor.RowTag.Front) continue;
            list.Add(e);
        }
        return list;
    }

    public List<CombatActor> GetAliveAllies(bool includePlayer = true)
    {
        var list = new List<CombatActor>(maxAllies);
        if (includePlayer && Player && !Player.IsDead) list.Add(Player);
        foreach (var a in _allies)
            if (a && !a.IsDead) list.Add(a);
        return list;
    }

    public void EnsureWorldUIForAll()
    {
        foreach (var a in _allies) EnsureWorldUIFor(a);
        foreach (var e in _enemies) EnsureWorldUIFor(e);
    }

    public void EnsureWorldUIFor(CombatActor actor)
    {
        if (!worldUiPrefab) return;
        if (!actor) return;
        if (actor.isPlayer) return;

        if (_uiMap.TryGetValue(actor, out var existing) && existing) return;

        var ui = Instantiate(worldUiPrefab);
        ui.Bind(actor);
        _uiMap[actor] = ui;
    }

    [ContextMenu("Layout All")]
    public void LayoutAll()
    {
        LayoutSide(CombatActor.TeamSide.Ally);
        LayoutSide(CombatActor.TeamSide.Enemy);
    }

    public void LayoutSide(CombatActor.TeamSide side)
    {
        Transform anchor = (side == CombatActor.TeamSide.Ally) ? alliesAnchor : enemiesAnchor;
        if (!anchor) return;

        List<CombatActor> members = (side == CombatActor.TeamSide.Ally)
            ? GetAliveAllies(includePlayer: true)
            : GetAliveEnemies(frontOnly: false);

        members.Sort(CompareBySpawnOrder);
        PlaceSingleLine(members, anchor.position, side);
    }

    private int CompareBySpawnOrder(CombatActor a, CombatActor b)
    {
        int oa = _spawnOrder.TryGetValue(a, out int va) ? va : 0;
        int ob = _spawnOrder.TryGetValue(b, out int vb) ? vb : 0;
        return oa.CompareTo(ob);
    }

    private void PlaceSingleLine(List<CombatActor> members, Vector3 anchorPos, CombatActor.TeamSide side)
    {
        int n = members.Count;
        if (n <= 0) return;

        float spacing = 0f;
        if (n == 2) spacing = spacingFor2;
        else if (n >= 3) spacing = spacingFor3;

        float sign = (side == CombatActor.TeamSide.Ally) ? 1f : -1f;

        for (int i = 0; i < n; i++)
        {
            var m = members[i];
            if (!m || m.IsDead) continue;

            float spreadX = GetCenteredX(i, n, spacing);
            float depth = (m.row == CombatActor.RowTag.Front) ? depthOffset : -depthOffset;

            var target = new Vector3(anchorPos.x + spreadX + sign * depth, anchorPos.y, anchorPos.z);
            m.transform.position = target;

            if (applyScaleByRow)
            {
                float s = (m.row == CombatActor.RowTag.Front) ? frontScale : backScale;
                m.transform.localScale = new Vector3(s, s, 1f);
            }
        }
    }

    private static float GetCenteredX(int index, int count, float spacing)
    {
        if (count <= 1) return 0f;
        if (count == 2) return (index == 0) ? -spacing * 0.5f : spacing * 0.5f;

        if (count == 3)
        {
            if (index == 0) return -spacing;
            if (index == 1) return 0f;
            return spacing;
        }

        float total = spacing * (count - 1);
        return -total * 0.5f + spacing * index;
    }

    public void ClearAll(bool destroyObjects)
    {
        // destroy enemies
        for (int i = _enemies.Count - 1; i >= 0; i--)
            if (_enemies[i]) Destroy(_enemies[i].gameObject);
        _enemies.Clear();

        // destroy non-player allies
        for (int i = _allies.Count - 1; i >= 0; i--)
            if (_allies[i]) Destroy(_allies[i].gameObject);
        _allies.Clear();

        // destroy world UI
        foreach (var kv in _uiMap)
            if (kv.Value) Destroy(kv.Value.gameObject);
        _uiMap.Clear();

        // optionally destroy player too (here we keep it if already spawned)
        if (destroyObjects && Player != null)
        {
            Destroy(Player.gameObject);
            Player = null;
        }

        _spawnOrder.Clear();
        _spawnSerial = 0;
    }
}
