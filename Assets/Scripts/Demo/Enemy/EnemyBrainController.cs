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

        // 1) FORCE MOVE (opener/script)
        for (int i = 0; i < definition.moves.Count; i++)
        {
            var m = definition.moves[i];
            if (m.forceOnTurn <= 0 || m.forceOnTurn != turn) continue;

            // vẫn phải hợp lệ
            if (!m.HasAnySkill) continue;
            if (turn < m.minTurnIndex) continue;
            if (hpPct < m.hpPctMin || hpPct > m.hpPctMax) continue;
            if (GetCooldown(i) > 0) continue;
            if (GetConsecutive(i) >= Mathf.Max(1, m.maxConsecutive)) continue;

            // NoRepeatTwice (cho phép ignore)
            if (definition.noRepeatTwice && !m.ignoreNoRepeat && _recentMoveHistory.Count > 0)
            {
                int last = PeekLastHistory();
                if (last == i) continue;
            }

            CurrentIntent = new IntentData
            {
                hasIntent = true,
                moveIndex = i,
                moveId = m.moveId,
                intentTag = m.intent,
                moveTags = m.tags,
                previewText = BuildBasicPreview(m, player),
            };
            return;
        }

        // 2) Build candidates (weighted)
        List<int> candidates = new List<int>(definition.moves.Count);
        for (int i = 0; i < definition.moves.Count; i++)
        {
            var m = definition.moves[i];

            if (!m.HasAnySkill) continue;                 // chưa kéo skill => ignore
            if (m.weight <= 0) continue;
            if (turn < m.minTurnIndex) continue;
            if (hpPct < m.hpPctMin || hpPct > m.hpPctMax) continue;

            int cd = GetCooldown(i);
            if (cd > 0) continue;

            // anti-spam consecutive
            int cons = GetConsecutive(i);
            if (cons >= Mathf.Max(1, m.maxConsecutive)) continue;

            // global no repeat twice (có ngoại lệ)
            if (definition.noRepeatTwice && !m.ignoreNoRepeat && _recentMoveHistory.Count > 0)
            {
                int last = PeekLastHistory();
                if (last == i) continue;
            }

            candidates.Add(i);
        }

        // 3) Fallback: nới lỏng nhẹ để "luôn có move"
        // (vẫn tôn trọng minTurnIndex + HP gate + maxConsecutive)
        if (candidates.Count == 0)
        {
            for (int i = 0; i < definition.moves.Count; i++)
            {
                var m = definition.moves[i];

                if (!m.HasAnySkill) continue;
                if (m.weight <= 0) continue;
                if (turn < m.minTurnIndex) continue;
                if (hpPct < m.hpPctMin || hpPct > m.hpPctMax) continue;

                // fallback bỏ cooldown + noRepeat, nhưng vẫn giữ maxConsecutive
                int cons = GetConsecutive(i);
                if (cons >= Mathf.Max(1, m.maxConsecutive)) continue;

                candidates.Add(i);
            }
        }

        if (candidates.Count == 0)
        {
            CurrentIntent = IntentData.None;
            return;
        }

        int chosenIndex = WeightedPick(candidates);
        var chosenMove = definition.moves[chosenIndex];

        CurrentIntent = new IntentData
        {
            hasIntent = true,
            moveIndex = chosenIndex,
            moveId = chosenMove.moveId,
            intentTag = chosenMove.intent,
            moveTags = chosenMove.tags,
            previewText = BuildBasicPreview(chosenMove, player),
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
        // nếu last move giống idx => +1, else reset
        int last = PeekLastHistory(1); // previous (before this enqueue) is not accessible now, so use a simpler approach:
        // We'll recompute from recent history: count consecutive from tail
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

    private int WeightedPick(List<int> candidateIndices)
    {
        int total = 0;
        for (int i = 0; i < candidateIndices.Count; i++)
        {
            int idx = candidateIndices[i];
            total += Mathf.Max(0, definition.moves[idx].weight);
        }

        if (total <= 0) return candidateIndices[UnityEngine.Random.Range(0, candidateIndices.Count)];

        int r = UnityEngine.Random.Range(0, total);
        int acc = 0;
        for (int i = 0; i < candidateIndices.Count; i++)
        {
            int idx = candidateIndices[i];
            acc += Mathf.Max(0, definition.moves[idx].weight);
            if (r < acc) return idx;
        }

        return candidateIndices[candidateIndices.Count - 1];
    }

    private string BuildBasicPreview(EnemyDefinitionSO.EnemyMoveSlot move, CombatActor player)
    {
        // Skeleton preview: chỉ dựa vào intent tag + tags.
        // Sau này bạn có thể build preview thật từ SkillRuntime/SkillSO fields.
        switch (move.intent)
        {
            case EnemyDefinitionSO.EnemyIntentTag.Attack:
                if ((move.tags & EnemyDefinitionSO.EnemyMoveTag.Heavy) != 0) return $"{move.displayName} (Heavy)";
                return move.displayName;

            case EnemyDefinitionSO.EnemyIntentTag.Defend:
                return $"{move.displayName} (Guard)";

            case EnemyDefinitionSO.EnemyIntentTag.Buff:
                return $"{move.displayName} (Buff)";

            case EnemyDefinitionSO.EnemyIntentTag.Debuff:
                return $"{move.displayName} (Debuff)";

            case EnemyDefinitionSO.EnemyIntentTag.Summon:
                return $"{move.displayName} (Summon)";

            default:
                return move.displayName;
        }
    }

    public CombatActor PickMostInjuredEnemyAlly(BattlePartyManager2D party, bool includeSelf = true)
    {
        if (party == null) return null;

        // Enemy-side allies = Enemies roster
        var allies = party.GetAliveEnemies(frontOnly: false);
        if (allies == null || allies.Count == 0) return null;

        CombatActor best = null;
        float bestHpPct = 999f;
        int bestMissing = -1;

        for (int i = 0; i < allies.Count; i++)
        {
            var a = allies[i];
            if (a == null || a.IsDead) continue;
            if (!includeSelf && a == actor) continue;

            int max = Mathf.Max(1, a.maxHP);
            if (a.hp >= max) continue; // full HP -> không cần heal

            float pct = (float)a.hp / max;
            int missing = max - a.hp;

            // HP% thấp nhất ưu tiên, tie-break: missing HP nhiều nhất
            if (best == null || pct < bestHpPct || (Mathf.Approximately(pct, bestHpPct) && missing > bestMissing))
            {
                best = a;
                bestHpPct = pct;
                bestMissing = missing;
            }
        }

        return best;
    }

    // Helper: check current intent có tag Heal hay không
    public bool CurrentIntentIsHeal()
    {
        if (!CurrentIntent.hasIntent || definition == null) return false;
        var tags = CurrentIntent.moveTags;
        return (tags & EnemyDefinitionSO.EnemyMoveTag.Heal) != 0;
    }
}