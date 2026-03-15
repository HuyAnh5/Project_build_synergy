// EnemyBrainController.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// Gắn lên prefab Enemy để:
/// - Nhận EnemyDefinitionSO (id + tags + moves)
/// - Apply stats vào CombatActor
/// - Giữ runtime state: cooldown, history, current intent (telegraph)
/// 
/// Chưa triển khai gameplay cụ thể. Đây là "brain + intent holder".
/// </summary>
[DisallowMultipleComponent]
public class EnemyBrainController : MonoBehaviour
{
    [BoxGroup("Refs")]
    [Required]
    public EnemyDefinitionSO definition;

    [BoxGroup("Refs")]
    [Required]
    public CombatActor actor;

    [BoxGroup("Runtime (ReadOnly)"), ShowInInspector, ReadOnly]
    public int TurnIndex { get; private set; }

    [BoxGroup("Runtime (ReadOnly)"), ShowInInspector, ReadOnly]
    public IntentData CurrentIntent { get; private set; }

    [Serializable]
    public struct IntentData
    {
        public bool hasIntent;
        public int moveIndex;
        public string moveId;
        public EnemyDefinitionSO.EnemyIntentTag intentTag;
        public EnemyDefinitionSO.EnemyMoveTag moveTags;

        // Preview text cho UI (STS-style). Bạn có thể thay UI lấy từ skill sau này.
        public string previewText;

        public static IntentData None => new IntentData { hasIntent = false, moveIndex = -1, moveId = "", previewText = "" };
    }

    private readonly Dictionary<int, int> _cooldownRemainingByMoveIndex = new Dictionary<int, int>();
    private readonly Dictionary<int, int> _consecutiveByMoveIndex = new Dictionary<int, int>();
    private readonly Queue<int> _recentMoveHistory = new Queue<int>();

    private void Awake()
    {
        if (!actor) actor = GetComponent<CombatActor>();

        // Enemy không dùng focus: khóa về 0 để tránh hệ khác vô tình gain/spend.
        if (actor)
        {
            actor.maxFocus = 0;
            actor.startingFocus = 0;
            actor.focus = 0;
            actor.isPlayer = false;
        }

        if (definition && actor)
            ApplyDefinitionStats();
    }

    private void LateUpdate()
    {
        // Hard lock focus = 0 (phòng trường hợp executor nào đó gọi GainFocus nhầm).
        if (actor && actor.focus != 0) actor.focus = 0;
    }

    [BoxGroup("Setup")]
    [Button(ButtonSizes.Medium)]
    public void ApplyDefinitionStats()
    {
        if (!definition || !actor) return;

        actor.maxHP = Mathf.Max(1, definition.maxHP);
        actor.hp = actor.maxHP;
        actor.guardPool = Mathf.Max(0, definition.startingGuard);

        // Focus disabled already in Awake.
    }

    /// <summary>
    /// Call khi bắt đầu enemy battle hoặc khi reset state.
    /// </summary>
    public void ResetAIState()
    {
        TurnIndex = 0;
        CurrentIntent = IntentData.None;

        _cooldownRemainingByMoveIndex.Clear();
        _consecutiveByMoveIndex.Clear();
        _recentMoveHistory.Clear();
    }

    /// <summary>
    /// Call ở BeginEnemyTurn hoặc "cuối lượt player" để quyết định intent và telegraph.
    /// (Selection logic ở đây chỉ là skeleton: weight + cooldown + anti-repeat + hp gate.)
    /// </summary>
    public void DecideNextIntent(CombatActor player)
    {
        if (!definition || definition.moves == null || definition.moves.Count == 0)
        {
            CurrentIntent = IntentData.None;
            return;
        }

        int turn = TurnIndex + 1; // turn bắt đầu từ 1
        float hpPct = actor && actor.maxHP > 0 ? (float)actor.hp / actor.maxHP : 1f;

        int chosenIndex = EnemyIntentSelectionUtility.DecideNextMoveIndex(
            definition,
            turn,
            hpPct,
            _recentMoveHistory.Count,
            GetCooldown,
            GetConsecutive,
            () => PeekLastHistory());
        if (chosenIndex < 0)
        {
            CurrentIntent = IntentData.None;
            return;
        }

        var chosenMove = definition.moves[chosenIndex];

        CurrentIntent = new IntentData
        {
            hasIntent = true,
            moveIndex = chosenIndex,
            moveId = chosenMove.moveId,
            intentTag = chosenMove.intent,
            moveTags = chosenMove.tags,
            previewText = EnemyIntentPreviewUtility.BuildBasicPreview(chosenMove),
        };
    }

    /// <summary>
    /// Call khi intent đã được thực thi xong trong EnemyTurn.
    /// Update history/cooldown/consecutive và clear intent.
    /// </summary>
    public void ConsumeCurrentIntent()
    {
        if (!CurrentIntent.hasIntent) return;

        int idx = CurrentIntent.moveIndex;

        // history window
        EnqueueHistory(idx);

        // cooldown
        int cd = Mathf.Max(0, definition.moves[idx].cooldownTurns);
        if (cd > 0) _cooldownRemainingByMoveIndex[idx] = cd;

        // consecutive tracking
        // Recompute từ tail history sau khi enqueue để giữ logic đơn giản.
        _consecutiveByMoveIndex[idx] = CountTailConsecutive(idx);

        // clear
        CurrentIntent = IntentData.None;
    }

    /// <summary>
    /// Call mỗi khi bắt đầu turn mới của enemy (hoặc mỗi EnemyTurn tick) để giảm cooldown.
    /// </summary>
    public void AdvanceTurnTick()
    {
        TurnIndex++;

        // tick cooldowns
        var keys = new List<int>(_cooldownRemainingByMoveIndex.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            int k = keys[i];
            _cooldownRemainingByMoveIndex[k] = Mathf.Max(0, _cooldownRemainingByMoveIndex[k] - 1);
            if (_cooldownRemainingByMoveIndex[k] == 0) _cooldownRemainingByMoveIndex.Remove(k);
        }
    }

    // -------------------- Helpers --------------------

    private int GetCooldown(int moveIndex)
    {
        return _cooldownRemainingByMoveIndex.TryGetValue(moveIndex, out int v) ? v : 0;
    }

    private int GetConsecutive(int moveIndex)
    {
        // compute consecutive from history tail
        return CountTailConsecutive(moveIndex);
    }

    private int CountTailConsecutive(int moveIndex)
    {
        if (_recentMoveHistory.Count == 0) return 0;

        // Convert to array to read tail
        int[] arr = _recentMoveHistory.ToArray();
        int count = 0;
        for (int i = arr.Length - 1; i >= 0; i--)
        {
            if (arr[i] == moveIndex) count++;
            else break;
        }
        return count;
    }

    private void EnqueueHistory(int moveIndex)
    {
        _recentMoveHistory.Enqueue(moveIndex);
        while (_recentMoveHistory.Count > Mathf.Max(1, definition.historyWindow))
            _recentMoveHistory.Dequeue();
    }

    private int PeekLastHistory(int offsetFromEnd = 0)
    {
        if (_recentMoveHistory.Count == 0) return -1;
        int[] arr = _recentMoveHistory.ToArray();
        int idx = Mathf.Clamp(arr.Length - 1 - offsetFromEnd, 0, arr.Length - 1);
        return arr[idx];
    }

    public CombatActor PickMostInjuredEnemyAlly(BattlePartyManager2D party, bool includeSelf = true)
        => EnemyIntentPreviewUtility.PickMostInjuredEnemyAlly(actor, party, includeSelf);

    // Helper: check current intent có tag Heal hay không
    public bool CurrentIntentIsHeal()
    {
        if (definition == null)
            return false;

        return EnemyIntentPreviewUtility.IsHealIntent(CurrentIntent);
    }
}
