using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

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
    [SerializeField] private float frontRowY = -0.3f;
    [SerializeField] private float backRowY = 0.55f;
    [SerializeField] private float centerRowY = 0f;
    [SerializeField] private float singleRowSpacing = 2.5f;
    [SerializeField] private float dualRowSpacing = 2.5f;
    [SerializeField] private float equalRowStagger = 0.7f;
    [SerializeField] private float minCrossRowX = 1.1f;
    [SerializeField] private int crossRowResolveIterations = 2;
    [SerializeField] private float outerGapFactor = 0.75f;
    [SerializeField] private int gapSideBias = 1;
    [FormerlySerializedAs("enemySingleRowScale")]
    [SerializeField] private float enemyFrontRowScale = 1f;
    [SerializeField] private float enemyBackRowScale = 0.8f;
    [SerializeField] private float formationTweenDuration = 0.3f;
    [SerializeField] private Ease formationEase = Ease.OutQuad;
    [SerializeField] private bool applyEnemyRowSorting = true;
    [SerializeField] private int enemyBackRowSortingBase = 0;
    [SerializeField] private int enemyFrontRowSortingBase = 100;

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
    private EnemyFormationSettingsSnapshot _lastFormationSettings;
    private string _lastEnemySlotsSignature = string.Empty;

    private struct EnemyFormationSlot
    {
        public CombatActor actor;
        public float localX;
        public float localY;
        public float targetScale;
        public bool isFrontRow;
    }

    private struct EnemyFormationSettingsSnapshot
    {
        public float frontRowY;
        public float backRowY;
        public float centerRowY;
        public float singleRowSpacing;
        public float dualRowSpacing;
        public float equalRowStagger;
        public float minCrossRowX;
        public int crossRowResolveIterations;
        public float outerGapFactor;
        public int gapSideBias;
        public float enemyFrontRowScale;
        public float enemyBackRowScale;
        public bool applyEnemyRowSorting;
        public int enemyBackRowSortingBase;
        public int enemyFrontRowSortingBase;
        public float formationTweenDuration;
        public Ease formationEase;

        public bool Matches(EnemyFormationSettingsSnapshot other)
        {
            return Mathf.Approximately(frontRowY, other.frontRowY) &&
                   Mathf.Approximately(backRowY, other.backRowY) &&
                   Mathf.Approximately(centerRowY, other.centerRowY) &&
                   Mathf.Approximately(singleRowSpacing, other.singleRowSpacing) &&
                   Mathf.Approximately(dualRowSpacing, other.dualRowSpacing) &&
                   Mathf.Approximately(equalRowStagger, other.equalRowStagger) &&
                   Mathf.Approximately(minCrossRowX, other.minCrossRowX) &&
                   crossRowResolveIterations == other.crossRowResolveIterations &&
                   Mathf.Approximately(outerGapFactor, other.outerGapFactor) &&
                   gapSideBias == other.gapSideBias &&
                   Mathf.Approximately(enemyFrontRowScale, other.enemyFrontRowScale) &&
                   Mathf.Approximately(enemyBackRowScale, other.enemyBackRowScale) &&
                   applyEnemyRowSorting == other.applyEnemyRowSorting &&
                   enemyBackRowSortingBase == other.enemyBackRowSortingBase &&
                   enemyFrontRowSortingBase == other.enemyFrontRowSortingBase &&
                   Mathf.Approximately(formationTweenDuration, other.formationTweenDuration) &&
                   formationEase == other.formationEase;
        }
    }

    public IReadOnlyList<CombatActor> AlliesNonPlayer => _allies;
    public IReadOnlyList<CombatActor> Enemies => _enemies;

    public event Action onRosterChanged;

    private EnemyFormationSettingsSnapshot CaptureFormationSettings()
    {
        return new EnemyFormationSettingsSnapshot
        {
            frontRowY = frontRowY,
            backRowY = backRowY,
            centerRowY = centerRowY,
            singleRowSpacing = singleRowSpacing,
            dualRowSpacing = dualRowSpacing,
            equalRowStagger = equalRowStagger,
            minCrossRowX = minCrossRowX,
            crossRowResolveIterations = crossRowResolveIterations,
            outerGapFactor = outerGapFactor,
            gapSideBias = gapSideBias,
            enemyFrontRowScale = enemyFrontRowScale,
            enemyBackRowScale = enemyBackRowScale,
            applyEnemyRowSorting = applyEnemyRowSorting,
            enemyBackRowSortingBase = enemyBackRowSortingBase,
            enemyFrontRowSortingBase = enemyFrontRowSortingBase,
            formationTweenDuration = formationTweenDuration,
            formationEase = formationEase,
        };
    }

    private void CacheFormationSettings()
    {
        _lastFormationSettings = CaptureFormationSettings();
    }

    private string BuildEnemySlotsSignature()
    {
        if (enemySlots == null || enemySlots.Length == 0)
            return string.Empty;

        var parts = new string[enemySlots.Length];
        for (int i = 0; i < enemySlots.Length; i++)
        {
            SpawnSlot slot = enemySlots[i];
            if (slot == null || slot.prefab == null)
            {
                parts[i] = "null";
                continue;
            }

            parts[i] = $"{slot.prefab.GetInstanceID()}:{slot.row}";
        }

        return string.Join("|", parts);
    }

    private void CacheEnemySlotsSignature()
    {
        _lastEnemySlotsSignature = BuildEnemySlotsSignature();
    }

    void Awake()
    {
        BattlePartyManagerRegistry.Register(this);

        if (turnManager == null)
            turnManager = GetComponent<TurnManager>() ?? TurnManagerRegistry.Get();

        CacheFormationSettings();
        CacheEnemySlotsSignature();

        if (spawnOnStart)
            EnsureSpawned();
    }

    private void OnDestroy()
    {
        BattlePartyManagerRegistry.Unregister(this);
    }

    private void Update()
    {
        if (!Application.isPlaying)
            return;

        EnemyFormationSettingsSnapshot current = CaptureFormationSettings();
        string currentEnemySlotsSignature = BuildEnemySlotsSignature();
        bool formationChanged = !current.Matches(_lastFormationSettings);
        bool enemySlotsChanged = !string.Equals(currentEnemySlotsSignature, _lastEnemySlotsSignature, StringComparison.Ordinal);

        if (!formationChanged && !enemySlotsChanged)
            return;

        _lastFormationSettings = current;
        _lastEnemySlotsSignature = currentEnemySlotsSignature;

        if (enemySlotsChanged)
        {
            SpawnPrototypeEncounter(enemySlots, resetPlayerForBattle: false);
            return;
        }

        LayoutSide(CombatActor.TeamSide.Enemy);
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

        CacheFormationSettings();
        CacheEnemySlotsSignature();

        if (Application.isPlaying)
            SpawnPrototypeEncounter(enemySlots, resetPlayerForBattle: false);
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

        List<EnemyFormationSlot> slots = CalculateEnemyFormationSlots(frontEnemies, backEnemies);
        for (int i = 0; i < slots.Count; i++)
        {
            EnemyFormationSlot slot = slots[i];
            LayoutEnemyActor(slot.actor, anchor, slot.localX, slot.localY, slot.targetScale);
        }
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

    private void LayoutEnemyActor(CombatActor enemy, Transform anchor, float localX, float localY, float targetScale)
    {
        if (enemy == null || anchor == null)
            return;

        ApplyEnemyRowSorting(enemy);
        Vector3 worldTarget = anchor.TransformPoint(new Vector3(localX, localY, 0f));
        AnimateEnemyFormation(enemy, worldTarget, targetScale);
    }

    private void ApplyEnemyRowSorting(CombatActor enemy)
    {
        if (!applyEnemyRowSorting || enemy == null)
            return;

        int baseOrder = enemy.row == CombatActor.RowTag.Front
            ? enemyFrontRowSortingBase
            : enemyBackRowSortingBase;
        Renderer[] renderers = enemy.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
            return;

        int minOrder = int.MaxValue;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || renderer is ParticleSystemRenderer)
                continue;

            minOrder = Mathf.Min(minOrder, renderer.sortingOrder);
        }

        if (minOrder == int.MaxValue)
            minOrder = 0;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || renderer is ParticleSystemRenderer)
                continue;

            renderer.sortingOrder = baseOrder + (renderer.sortingOrder - minOrder);
        }
    }

    private List<EnemyFormationSlot> CalculateEnemyFormationSlots(List<CombatActor> frontEnemies, List<CombatActor> backEnemies)
    {
        var slots = new List<EnemyFormationSlot>((frontEnemies?.Count ?? 0) + (backEnemies?.Count ?? 0));
        int frontCount = frontEnemies != null ? frontEnemies.Count : 0;
        int backCount = backEnemies != null ? backEnemies.Count : 0;

        if (frontCount <= 0 && backCount <= 0)
            return slots;

        if (frontCount == 0 || backCount == 0)
        {
            List<CombatActor> singleRowActors = frontCount > 0 ? frontEnemies : backEnemies;
            List<float> xs = CenteredOffsets(singleRowActors.Count, singleRowSpacing);
            for (int i = 0; i < singleRowActors.Count; i++)
            {
                slots.Add(new EnemyFormationSlot
                {
                    actor = singleRowActors[i],
                    localX = xs[i],
                    localY = centerRowY,
                    targetScale = enemyFrontRowScale,
                    isFrontRow = true,
                });
            }

            NormalizeSlotsToAnchor(slots);
            return slots;
        }

        List<float> frontXs;
        List<float> backXs;

        if (frontCount == backCount)
        {
            List<float> baseXs = CenteredOffsets(frontCount, dualRowSpacing);
            frontXs = new List<float>(baseXs.Count);
            backXs = new List<float>(baseXs.Count);
            for (int i = 0; i < baseXs.Count; i++)
            {
                frontXs.Add(baseXs[i] - equalRowStagger);
                backXs.Add(baseXs[i] + equalRowStagger);
            }
        }
        else if (frontCount > backCount)
        {
            frontXs = CenteredOffsets(frontCount, dualRowSpacing);
            backXs = GapOffsets(frontXs, backCount, dualRowSpacing, gapSideBias);
        }
        else
        {
            backXs = CenteredOffsets(backCount, dualRowSpacing);
            frontXs = GapOffsets(backXs, frontCount, dualRowSpacing, gapSideBias);
        }

        for (int i = 0; i < frontCount; i++)
        {
            slots.Add(new EnemyFormationSlot
            {
                actor = frontEnemies[i],
                localX = frontXs[i],
                localY = frontRowY,
                targetScale = enemyFrontRowScale,
                isFrontRow = true,
            });
        }

        for (int i = 0; i < backCount; i++)
        {
            slots.Add(new EnemyFormationSlot
            {
                actor = backEnemies[i],
                localX = backXs[i],
                localY = backRowY,
                targetScale = enemyBackRowScale,
                isFrontRow = false,
            });
        }

        NormalizeSlotsToAnchor(slots);
        ResolveCrossRowOverlap(slots, minCrossRowX, crossRowResolveIterations);
        NormalizeSlotsToAnchor(slots);
        return slots;
    }

    private List<float> CenteredOffsets(int count, float spacing)
    {
        var xs = new List<float>(count);
        for (int i = 0; i < count; i++)
            xs.Add((i - (count - 1) * 0.5f) * spacing);
        return xs;
    }

    private List<float> GapOffsets(List<float> mainXs, int neededCount, float spacing, int sideBias)
    {
        var gaps = new List<float>();
        if (mainXs == null || mainXs.Count == 0 || neededCount <= 0)
            return gaps;

        for (int i = 0; i < mainXs.Count - 1; i++)
            gaps.Add((mainXs[i] + mainXs[i + 1]) * 0.5f);

        gaps.Add(mainXs[0] - spacing * outerGapFactor);
        gaps.Add(mainXs[mainXs.Count - 1] + spacing * outerGapFactor);

        gaps.Sort((a, b) =>
        {
            int magnitude = Mathf.Abs(a).CompareTo(Mathf.Abs(b));
            if (magnitude != 0)
                return magnitude;

            return (sideBias * a).CompareTo(sideBias * b);
        });

        int take = Mathf.Min(neededCount, gaps.Count);
        List<float> result = gaps.GetRange(0, take);
        result.Sort();
        return result;
    }

    private void NormalizeSlotsToAnchor(List<EnemyFormationSlot> slots)
    {
        if (slots == null || slots.Count == 0)
            return;

        float sum = 0f;
        for (int i = 0; i < slots.Count; i++)
            sum += slots[i].localX;

        float average = sum / slots.Count;
        for (int i = 0; i < slots.Count; i++)
        {
            EnemyFormationSlot slot = slots[i];
            slot.localX -= average;
            slots[i] = slot;
        }
    }

    private void ResolveCrossRowOverlap(List<EnemyFormationSlot> slots, float requiredSeparation, int iterations)
    {
        if (slots == null || slots.Count <= 1 || requiredSeparation <= 0f || iterations <= 0)
            return;

        for (int iteration = 0; iteration < iterations; iteration++)
        {
            bool changed = false;
            for (int i = 0; i < slots.Count; i++)
            {
                for (int j = i + 1; j < slots.Count; j++)
                {
                    EnemyFormationSlot a = slots[i];
                    EnemyFormationSlot b = slots[j];
                    if (a.isFrontRow == b.isFrontRow)
                        continue;

                    float deltaX = b.localX - a.localX;
                    float distance = Mathf.Abs(deltaX);
                    if (distance >= requiredSeparation)
                        continue;

                    float push = (requiredSeparation - distance) * 0.5f;
                    float sign = deltaX >= 0f ? 1f : -1f;

                    if (a.isFrontRow)
                    {
                        a.localX -= push * sign;
                        b.localX += push * sign;
                    }
                    else
                    {
                        a.localX += push * sign;
                        b.localX -= push * sign;
                    }

                    slots[i] = a;
                    slots[j] = b;
                    changed = true;
                }
            }

            if (!changed)
                break;
        }
    }

    private void AnimateEnemyFormation(CombatActor enemy, Vector3 targetPosition, float targetScale)
    {
        if (enemy == null)
            return;

        if (CombatActorMotionLock.IsLocked(enemy))
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
