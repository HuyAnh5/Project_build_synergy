using System;
using System.Collections.Generic;
using UnityEngine;

public enum MapPrototypeNodeType
{
    Start,
    Combat,
    Elite,
    Event,
    Shop,
    Rest,
    Forge,
    Boss
}

[Serializable]
public sealed class MapPrototypeBossData
{
    public string bossName;
    public string description;
    public string badgeText;
}

[Serializable]
public sealed class MapPrototypeNodeData
{
    public string id;
    public string key;
    public float row;
    public float col;
    public MapPrototypeNodeType type;
    public int encounterIndex = -1;
    public float x;
    public float y;
    public bool visited;
    public bool safeVisited;
    public bool cleared;
    public bool hasHint;
    public bool hintTaken;
    public bool shopHintBought;
    public bool ranSkipped;
    public bool specialLeaf;
    public int side;
    public bool lockedCombat;
    public MapPrototypeBossData bossData;
    public MapEncounterDefinitionSO encounterDefinition;
}

[Serializable]
public sealed class MapPrototypeEdgeData
{
    public string from;
    public string to;
}

public sealed class MapPrototypeData
{
    public readonly List<MapPrototypeNodeData> nodes = new List<MapPrototypeNodeData>();
    public readonly List<MapPrototypeEdgeData> edges = new List<MapPrototypeEdgeData>();
    public readonly Dictionary<string, HashSet<string>> adjacency = new Dictionary<string, HashSet<string>>();
    public MapPrototypeBossData boss;
}

[Serializable]
public sealed class MapPrototypeConfig
{
    public bool useLinearPrototypeLayout;
    public int linearCombatNodeCount = 4;
    public int columns = 7;
    public int intermediateRows = 9;
    public int pathCount = 3;
    public int minMainNodeCount = 25;
    public int maxMainNodeCount = 30;

    public float mapWidth = 940f;
    public float mapHeight = 1450f;
    public float padX = 92f;
    public float padY = 82f;
    public float minNodeSpacing = 104f;
    public float maxNodeSpacingInLayer = 210f;
    public float targetLayerWidth = 560f;
    public float layerHeightMin = 112f;
    public float layerHeightMax = 150f;
    public float jitterX = 24f;
    public float jitterY = 10f;
    public float edgeCrossingPenalty = 180f;
    public float edgeLengthPenalty = 0.024f;
    public float nodeOverlapPenalty = 12f;
    public float tooSparsePenalty = 0.11f;

    public int maxAttempts = 32;
    public int maxForcedLinearStreak = 5;
    public int maxFourNodeLinearSegments = 4;
    public int maxLinearPenalty = 40;

    public float leafIdealDistance = 154f;
    public float leafMinDistance = 138f;
    public float leafMaxDistance = 178f;
    public float leafMinRise = 66f;
    public float leafMinRun = 70f;

    public int maxNodeDegree = 4;
    public int minRestNodeGap = 2;
    public int minRestRowGap = 2;
    public int targetRestCount = 2;
    public int restThreeThresholdIntermediateNodes = 25;
    public int restFourThresholdIntermediateNodes = 32;
    public float eventRatioTarget = 0.23f;
    public int eventVarianceMin = -1;
    public int eventVarianceMax = 2;
    public int firstRowMaxEvents = 2;
    public int hintMinNodesBetween = 1;
    public int eliteMinNodeGap = 1;
    public int eliteThreeThresholdIntermediateNodes = 25;
    public int maxHostileStreak = 5;
    public int bossRouteMaxSharedRow = 8;

    public int bossHintsRequired = 3;
    public int extraHintSources = 3;
    public int latestHintRow = 8;

    public List<MapPrototypeBossData> bosses = CreateHtmlBossPool();

    public void ApplyHtmlSourceOfTruthDefaults()
    {
        columns = 7;
        intermediateRows = 9;
        pathCount = 3;
        minMainNodeCount = 25;
        maxMainNodeCount = 30;

        mapWidth = 940f;
        mapHeight = 1450f;
        padX = 92f;
        padY = 82f;
        minNodeSpacing = 104f;
        maxNodeSpacingInLayer = 210f;
        targetLayerWidth = 560f;
        layerHeightMin = 112f;
        layerHeightMax = 150f;
        jitterX = 24f;
        jitterY = 10f;
        edgeCrossingPenalty = 180f;
        edgeLengthPenalty = 0.024f;
        nodeOverlapPenalty = 12f;
        tooSparsePenalty = 0.11f;

        maxAttempts = 32;
        maxForcedLinearStreak = 5;
        maxFourNodeLinearSegments = 4;
        maxLinearPenalty = 40;

        leafIdealDistance = 154f;
        leafMinDistance = 138f;
        leafMaxDistance = 178f;
        leafMinRise = 66f;
        leafMinRun = 70f;

        maxNodeDegree = 4;
        minRestNodeGap = 2;
        minRestRowGap = 2;
        targetRestCount = 2;
        restThreeThresholdIntermediateNodes = 25;
        restFourThresholdIntermediateNodes = 32;
        eventRatioTarget = 0.23f;
        eventVarianceMin = -1;
        eventVarianceMax = 2;
        firstRowMaxEvents = 2;
        hintMinNodesBetween = 1;
        eliteMinNodeGap = 1;
        eliteThreeThresholdIntermediateNodes = 25;
        maxHostileStreak = 5;
        bossRouteMaxSharedRow = 8;

        bossHintsRequired = 3;
        extraHintSources = 3;
        latestHintRow = 8;
        bosses = CreateHtmlBossPool();
    }

    private static List<MapPrototypeBossData> CreateHtmlBossPool()
    {
        return new List<MapPrototypeBossData>
        {
            new MapPrototypeBossData
            {
                bossName = "The Hollow King",
                description = "A ruined king still wearing a crown that has not stopped bleeding.",
                badgeText = "HK"
            },
            new MapPrototypeBossData
            {
                bossName = "Ashen Matriarch",
                description = "A spider matriarch holding the whole act inside ash and old curses.",
                badgeText = "AM"
            },
            new MapPrototypeBossData
            {
                bossName = "The Bell Tyrant",
                description = "Every ring of the bell calls another omen back to life.",
                badgeText = "BT"
            },
            new MapPrototypeBossData
            {
                bossName = "The Gloam Warden",
                description = "A dusk sentinel that watches the road without ever resting.",
                badgeText = "GW"
            }
        };
    }
}

public static class MapPrototypeNodeCatalog
{
    public static bool IsHostile(MapPrototypeNodeType type)
    {
        return type == MapPrototypeNodeType.Combat
            || type == MapPrototypeNodeType.Elite
            || type == MapPrototypeNodeType.Boss;
    }

    public static string GetLabel(MapPrototypeNodeType type)
    {
        switch (type)
        {
            case MapPrototypeNodeType.Start: return "Start";
            case MapPrototypeNodeType.Combat: return "Combat";
            case MapPrototypeNodeType.Elite: return "Elite";
            case MapPrototypeNodeType.Event: return "Event";
            case MapPrototypeNodeType.Shop: return "Shop";
            case MapPrototypeNodeType.Rest: return "Rest";
            case MapPrototypeNodeType.Forge: return "Forge";
            case MapPrototypeNodeType.Boss: return "Boss";
            default: return "Node";
        }
    }

    public static string GetDescription(MapPrototypeNodeType type)
    {
        switch (type)
        {
            case MapPrototypeNodeType.Start:
                return "The starting point of the act.";
            case MapPrototypeNodeType.Combat:
                return "A normal encounter. Running leaves the enemy here.";
            case MapPrototypeNodeType.Elite:
                return "A harder encounter that still behaves like prototype combat.";
            case MapPrototypeNodeType.Event:
                return "A one-time event. After the first visit it becomes a safe path node.";
            case MapPrototypeNodeType.Shop:
                return "A merchant node. Buy Hint is a real action, not an automatic reward.";
            case MapPrototypeNodeType.Rest:
                return "A rest node that represents healing inside the act.";
            case MapPrototypeNodeType.Forge:
                return "A forge landmark. It stays on the map after the first visit.";
            case MapPrototypeNodeType.Boss:
                return "The boss node at the top of the act.";
            default:
                return string.Empty;
        }
    }

    public static string GetBadge(MapPrototypeNodeType type, bool bossRevealed, MapPrototypeBossData bossData)
    {
        switch (type)
        {
            case MapPrototypeNodeType.Start: return "ST";
            case MapPrototypeNodeType.Combat: return "C";
            case MapPrototypeNodeType.Elite: return "E";
            case MapPrototypeNodeType.Event: return "EV";
            case MapPrototypeNodeType.Shop: return "SH";
            case MapPrototypeNodeType.Rest: return "R";
            case MapPrototypeNodeType.Forge: return "FG";
            case MapPrototypeNodeType.Boss:
                if (bossRevealed && bossData != null && !string.IsNullOrWhiteSpace(bossData.badgeText))
                    return bossData.badgeText;
                return "?";
            default:
                return string.Empty;
        }
    }

    public static Color GetFillColor(MapPrototypeNodeType type)
    {
        switch (type)
        {
            case MapPrototypeNodeType.Start: return new Color32(98, 72, 31, 255);
            case MapPrototypeNodeType.Combat: return new Color32(104, 61, 36, 255);
            case MapPrototypeNodeType.Elite: return new Color32(118, 44, 46, 255);
            case MapPrototypeNodeType.Event: return new Color32(74, 52, 112, 255);
            case MapPrototypeNodeType.Shop: return new Color32(106, 78, 28, 255);
            case MapPrototypeNodeType.Rest: return new Color32(43, 92, 70, 255);
            case MapPrototypeNodeType.Forge: return new Color32(58, 67, 84, 255);
            case MapPrototypeNodeType.Boss: return new Color32(92, 31, 41, 255);
            default: return Color.gray;
        }
    }
}
