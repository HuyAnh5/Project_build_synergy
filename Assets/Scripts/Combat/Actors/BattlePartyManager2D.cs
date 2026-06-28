using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class BattlePartyManager2D : MonoBehaviour
{
    [Serializable]
    public class SpawnSlot
    {
        [HideInInspector] public string label;
        public CombatActor prefab;
        public CombatActor.RowTag row = CombatActor.RowTag.Front;
        [HideInInspector] public int orderInRow = 0;
    }

    [Serializable]
    public class WorldUiPrefabEntry
    {
        public string tag = CombatActor.DefaultWorldUiTag;
        public ActorWorldUI prefab;
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
    public float memberSpacing = 0.9f;

    [Header("Depth Visual (optional)")]
    public bool applyScaleByRow = true;
    public float frontScale = 1.0f;
    public float backScale = 0.92f;

    [Header("Enemy Formation Visual")]
    public float frontRowY = -0.15f;
    public float backRowY = 0.45f;
    public float centerRowY = 0f;
    public float enemyRowSpacing = 0.9f;
    public float rowInterleaveOffset = 0.35f;
    public float enemyBackRowScale = 0.8f;
    public float enemySingleRowScale = 1f;
    public float formationTweenDuration = 0.3f;
    public Ease formationEase = Ease.OutQuad;

    [Header("World UI")]
    [Tooltip("Data-driven mapping from actor worldUiTag to the prefab that should be spawned.")]
    public List<WorldUiPrefabEntry> worldUiPrefabs = new();
    public bool autoSpawnWorldUI = true;

    [Header("Battle Reset Rule (prototype)")]
    public bool resetHPOnBattleStart = true;

    [Header("Wiring")]
    public TurnManager turnManager; // optional; auto-find if null

    private readonly List<CombatActor> _allies = new(3);   // non-player allies
    private readonly List<CombatActor> _enemies = new(3);
    private readonly Dictionary<CombatActor, ActorWorldUI> _uiMap = new();
    private readonly Dictionary<CombatActor, int> _spawnOrder = new();
    private readonly Dictionary<CombatActor, Tween> _formationMoveTweens = new();
    private readonly Dictionary<CombatActor, Tween> _formationScaleTweens = new();
    private int _spawnSerial = 0;
    private bool _spawnedOnce = false;

    public IReadOnlyList<CombatActor> AlliesNonPlayer => _allies;
    public IReadOnlyList<CombatActor> Enemies => _enemies;

    public event Action onRosterChanged;

    void Awake()
    {
        BattlePartyManagerRegistry.Register(this);

        if (turnManager == null)
            turnManager = GetComponent<TurnManager>() ?? TurnManagerRegistry.Get();

        if (spawnOnStart)
            EnsureSpawned();
    }

    private void OnDestroy()
    {
        BattlePartyManagerRegistry.Unregister(this);
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

        for (int i = 0; i < slots.Length; i++)
        {
            SpawnSlot slot = slots[i];
            if (slot == null || slot.prefab == null)
                continue;

            SpawnActor(slot.prefab, side, slot.row, isPlayer: false, addToRoster: true);
        }
    }

    private static int CountValidSpawnSlots(SpawnSlot[] slots)
    {
        if (slots == null)
            return 0;

        int count = 0;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null && slots[i].prefab != null)
                count++;
        }

        return count;
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

        var go = anchor != null
            ? Instantiate(prefab.gameObject, spawnPos, Quaternion.identity, anchor)
            : Instantiate(prefab.gameObject, spawnPos, Quaternion.identity);
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
            turnManager = GetComponent<TurnManager>() ?? TurnManagerRegistry.Get();

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
        if (Player != null) EnsureWorldUIFor(Player);
        foreach (var a in _allies) EnsureWorldUIFor(a);
        foreach (var e in _enemies) EnsureWorldUIFor(e);
    }

    public void EnsureWorldUIFor(CombatActor actor)
    {
        if (!actor) return;

        if (_uiMap.TryGetValue(actor, out var existing) && existing) return;

        ActorWorldUI embeddedUi = FindEmbeddedWorldUI(actor);
        if (embeddedUi != null)
        {
            embeddedUi.Bind(actor);
            _uiMap[actor] = embeddedUi;
            return;
        }

        ActorWorldUI prefab = ResolveWorldUiPrefab(actor);
        if (!prefab) return;

        var ui = Instantiate(prefab);
        ui.Bind(actor);
        _uiMap[actor] = ui;
    }

    private ActorWorldUI ResolveWorldUiPrefab(CombatActor actor)
    {
        if (actor == null)
            return null;

        string resolvedTag = actor.GetResolvedWorldUiTag();
        ActorWorldUI mappedPrefab = FindWorldUiPrefabByTag(resolvedTag);
        if (mappedPrefab != null)
            return mappedPrefab;

        return FindWorldUiPrefabByTag(CombatActor.DefaultWorldUiTag);
    }

    private ActorWorldUI FindWorldUiPrefabByTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag) || worldUiPrefabs == null)
            return null;

        for (int i = 0; i < worldUiPrefabs.Count; i++)
        {
            WorldUiPrefabEntry entry = worldUiPrefabs[i];
            if (entry == null || entry.prefab == null || string.IsNullOrWhiteSpace(entry.tag))
                continue;

            if (string.Equals(entry.tag.Trim(), tag.Trim(), StringComparison.OrdinalIgnoreCase))
                return entry.prefab;
        }

        return null;
    }

    private void OnValidate()
    {
        if (worldUiPrefabs == null)
            worldUiPrefabs = new List<WorldUiPrefabEntry>();

        for (int i = worldUiPrefabs.Count - 1; i >= 0; i--)
        {
            WorldUiPrefabEntry entry = worldUiPrefabs[i];
            if (entry == null)
            {
                worldUiPrefabs.RemoveAt(i);
                continue;
            }

            entry.tag = string.IsNullOrWhiteSpace(entry.tag)
                ? CombatActor.DefaultWorldUiTag
                : entry.tag.Trim();
        }
    }

    private static ActorWorldUI FindEmbeddedWorldUI(CombatActor actor)
    {
        if (actor == null)
            return null;

        ActorWorldUI[] worldUis = actor.GetComponentsInChildren<ActorWorldUI>(true);
        for (int i = 0; i < worldUis.Length; i++)
        {
            ActorWorldUI ui = worldUis[i];
            if (ui == null)
                continue;

            if (ui.transform == actor.transform)
                continue;

            return ui;
        }

        return null;
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

        if (side == CombatActor.TeamSide.Enemy)
        {
            LayoutEnemyFormation(anchor);
            return;
        }

        List<CombatActor> members = (side == CombatActor.TeamSide.Ally)
            ? GetAliveAllies(includePlayer: true)
            : GetAliveEnemies(frontOnly: false);

        members.Sort(CompareBySpawnOrder);
        PlaceSingleLine(members, anchor.position, side);
    }

    public void SpawnPrototypeEncounter(SpawnSlot[] slots, bool resetPlayerForBattle)
    {
        enemySlots = slots ?? new SpawnSlot[maxEnemies];
        maxEnemies = Mathf.Max(maxEnemies, CountValidSpawnSlots(enemySlots));
        ClearAll(destroyObjects: false);
        EnsurePlayerExists();
        if (resetPlayerForBattle && Player != null)
            Player.ResetForBattle(resetHPOnBattleStart);

        SpawnFromSlots(enemySlots, CombatActor.TeamSide.Enemy);

        if (autoSpawnWorldUI)
            EnsureWorldUIForAll();

        LayoutAll();
        onRosterChanged?.Invoke();
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

        float sign = (side == CombatActor.TeamSide.Ally) ? 1f : -1f;

        for (int i = 0; i < n; i++)
        {
            var m = members[i];
            if (!m || m.IsDead) continue;

            float spreadX = GetCenteredX(i, n, memberSpacing);
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

    private void LayoutEnemyFormation(Transform anchor)
    {
        List<CombatActor> aliveEnemies = GetAliveEnemies(frontOnly: false);
        aliveEnemies.Sort(CompareBySpawnOrder);
        if (aliveEnemies.Count <= 0)
            return;

        PromoteBackRowIfFrontIsEmpty(aliveEnemies);

        var frontEnemies = new List<CombatActor>(aliveEnemies.Count);
        var backEnemies = new List<CombatActor>(aliveEnemies.Count);
        for (int i = 0; i < aliveEnemies.Count; i++)
        {
            CombatActor enemy = aliveEnemies[i];
            if (enemy.row == CombatActor.RowTag.Back)
                backEnemies.Add(enemy);
            else
                frontEnemies.Add(enemy);
        }

        bool hasFront = frontEnemies.Count > 0;
        bool hasBack = backEnemies.Count > 0;
        if (!hasFront || !hasBack)
        {
            LayoutEnemyRow(aliveEnemies, anchor, centerRowY, enemySingleRowScale, BuildFormationRowPositions(aliveEnemies.Count, 0, frontIsReference: true));
            return;
        }

        float[] frontPositions = BuildFormationRowPositions(frontEnemies.Count, backEnemies.Count, frontIsReference: true);
        float[] backPositions = BuildFormationRowPositions(backEnemies.Count, frontEnemies.Count, frontIsReference: false);
        LayoutEnemyRow(frontEnemies, anchor, frontRowY, frontScale, frontPositions);
        LayoutEnemyRow(backEnemies, anchor, backRowY, enemyBackRowScale, backPositions);
    }

    private void PromoteBackRowIfFrontIsEmpty(List<CombatActor> aliveEnemies)
    {
        bool hasFront = false;
        bool hasBack = false;
        for (int i = 0; i < aliveEnemies.Count; i++)
        {
            CombatActor enemy = aliveEnemies[i];
            if (enemy.row == CombatActor.RowTag.Front)
                hasFront = true;
            else if (enemy.row == CombatActor.RowTag.Back)
                hasBack = true;
        }

        if (hasFront || !hasBack)
            return;

        for (int i = 0; i < aliveEnemies.Count; i++)
        {
            CombatActor enemy = aliveEnemies[i];
            if (enemy.row == CombatActor.RowTag.Back)
                enemy.row = CombatActor.RowTag.Front;
        }
    }

    private void LayoutEnemyRow(List<CombatActor> rowMembers, Transform anchor, float localY, float targetScale, float[] xPositions)
    {
        int count = rowMembers.Count;
        for (int i = 0; i < count; i++)
        {
            CombatActor enemy = rowMembers[i];
            if (!enemy || enemy.IsDead)
                continue;

            float x = xPositions != null && i < xPositions.Length
                ? xPositions[i]
                : GetCenteredX(i, count, enemyRowSpacing);
            Vector3 worldTarget = anchor.TransformPoint(new Vector3(x, localY, 0f));
            AnimateEnemyFormation(enemy, worldTarget, targetScale);
        }
    }

    private float[] BuildFormationRowPositions(int rowCount, int otherRowCount, bool frontIsReference)
    {
        if (rowCount <= 0)
            return Array.Empty<float>();

        float[] positions = new float[rowCount];
        if (rowCount == 1)
        {
            positions[0] = ResolveSingleRowOffset(otherRowCount, frontIsReference);
            return positions;
        }

        for (int i = 0; i < rowCount; i++)
            positions[i] = GetCenteredX(i, rowCount, enemyRowSpacing);

        float shift = ResolveInterleaveShift(rowCount, otherRowCount, frontIsReference);
        if (!Mathf.Approximately(shift, 0f))
        {
            for (int i = 0; i < positions.Length; i++)
                positions[i] += shift;
        }

        return positions;
    }

    private float ResolveSingleRowOffset(int otherRowCount, bool frontIsReference)
    {
        if (otherRowCount <= 1)
            return 0f;

        float halfSpacing = enemyRowSpacing * 0.5f;
        float offset = Mathf.Min(rowInterleaveOffset, halfSpacing);
        if (offset <= 0f)
            return 0f;

        if ((otherRowCount & 1) == 0)
            return 0f;

        return frontIsReference ? -offset : offset;
    }

    private float ResolveInterleaveShift(int rowCount, int otherRowCount, bool frontIsReference)
    {
        if (rowCount <= 0 || otherRowCount <= 0)
            return 0f;

        float halfSpacing = enemyRowSpacing * 0.5f;
        float offset = Mathf.Min(rowInterleaveOffset, halfSpacing);
        if (offset <= 0f)
            return 0f;

        bool sameParity = (rowCount & 1) == (otherRowCount & 1);
        if (!sameParity)
            return 0f;

        return frontIsReference ? -offset : offset;
    }

    private void AnimateEnemyFormation(CombatActor enemy, Vector3 targetPosition, float targetScale)
    {
        if (enemy == null)
            return;

        if (_formationMoveTweens.TryGetValue(enemy, out Tween moveTween) && moveTween != null && moveTween.IsActive())
            moveTween.Kill(false);

        if (_formationScaleTweens.TryGetValue(enemy, out Tween scaleTween) && scaleTween != null && scaleTween.IsActive())
            scaleTween.Kill(false);

        float duration = Mathf.Max(0.01f, formationTweenDuration);
        Transform target = enemy.transform;

        _formationMoveTweens[enemy] = target
            .DOMove(targetPosition, duration)
            .SetEase(formationEase);
        _formationScaleTweens[enemy] = target
            .DOScale(new Vector3(targetScale, targetScale, 1f), duration)
            .SetEase(formationEase);
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
        {
            if (!kv.Value)
                continue;

            // Detach first so the same-frame prototype respawn path does not
            // rediscover a UI that is already queued for destruction.
            kv.Value.actor = null;
            kv.Value.transform.SetParent(null, false);
            Destroy(kv.Value.gameObject);
        }
        _uiMap.Clear();

        // optionally destroy player too (here we keep it if already spawned)
        if (destroyObjects && Player != null)
        {
            Destroy(Player.gameObject);
            Player = null;
        }

        ClearFormationTweens();
        _spawnOrder.Clear();
        _spawnSerial = 0;
    }

    private void ClearFormationTweens()
    {
        foreach (Tween tween in _formationMoveTweens.Values)
        {
            if (tween != null && tween.IsActive())
                tween.Kill(false);
        }

        foreach (Tween tween in _formationScaleTweens.Values)
        {
            if (tween != null && tween.IsActive())
                tween.Kill(false);
        }

        _formationMoveTweens.Clear();
        _formationScaleTweens.Clear();
    }
}

internal static class BattlePartyManagerRegistry
{
    private static BattlePartyManager2D _instance;
    private static bool _initializedFromScene;

    public static void Register(BattlePartyManager2D party)
    {
        if (party == null)
            return;

        _instance = party;
    }

    public static void Unregister(BattlePartyManager2D party)
    {
        if (party == null || _instance != party)
            return;

        _instance = null;
        _initializedFromScene = false;
    }

    public static BattlePartyManager2D Get()
    {
        if (_instance != null)
            return _instance;

        EnsureInitializedFromScene();
        return _instance;
    }

    private static void EnsureInitializedFromScene()
    {
        if (_initializedFromScene)
            return;

        _initializedFromScene = true;
#if UNITY_2023_1_OR_NEWER
        _instance = UnityEngine.Object.FindFirstObjectByType<BattlePartyManager2D>(FindObjectsInactive.Include);
#else
        _instance = UnityEngine.Object.FindObjectOfType<BattlePartyManager2D>(true);
#endif
    }
}
