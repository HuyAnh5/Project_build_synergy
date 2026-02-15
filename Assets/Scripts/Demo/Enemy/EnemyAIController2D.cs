// EnemyAIController2D.cs
// Attach to Enemy prefab (same GameObject as CombatActor).
// TurnManager should call: yield return enemyAI.TakeTurn(player);
//
// AI:
// - Weighted random + conditions + anti-spam (no repeat) + cooldown per "enemy action"
// - Supports: BasicAttack, Guard, ApplyStatus, Heal(<50%), Setup->Heavy, GuardPunish
// - Executes via SkillExecutor.ExecuteSkill(...) so status rules match Player
// - Special: If THIS enemy hits ICE into a FROZEN player => break Freeze (handled by StatusController)
//   and grant THIS enemy exactly 1 extra action immediately (AI re-picks).

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyAIController2D : MonoBehaviour
{
    public enum MoveType
    {
        BasicAttack,
        HeavyAttack,     // typically executed after Setup
        Guard,
        GuardPunish,
        ApplyStatus,
        Heal,
        Setup            // charge now -> heavy next action
        // Summon later
    }

    [Serializable]
    public class MoveEntry
    {
        [Header("Identity")]
        public string id = "Move";
        public MoveType type = MoveType.BasicAttack;

        [Header("Execution (recommended: reuse SkillSO pipeline)")]
        public SkillSO skill;

        [Header("AI Weight")]
        [Range(0, 100)] public int weight = 30;

        [Header("Anti-spam / Cooldown")]
        [Range(0, 10)] public int cooldownActions = 0;
        public bool allowRepeat = false;

        [Header("Conditions")]
        [Tooltip("Allow only if enemy HP% <= this (1 = always). Ex: Heal: 0.5")]
        [Range(0f, 1f)] public float requireSelfHpPercentLE = 1f;

        [Tooltip("Allow only if player has guard > 0 (useful for GuardPunish).")]
        public bool requirePlayerHasGuard = false;

        [Tooltip("Allow only if NOT currently charging a heavy.")]
        public bool requireNotCharging = false;

        [Header("Heal (if type=Heal and skill is null)")]
        public int healAmount = 5;

        [Header("Setup->Heavy")]
        public SkillSO heavySkill;
    }

    [Header("Refs (auto-find if empty)")]
    public CombatActor self;
    public SkillExecutor executor;
    public BattlePartyManager2D party; // optional

    [Header("Moves")]
    public List<MoveEntry> moves = new List<MoveEntry>();

    [Header("AI Tuning")]
    public bool fallbackToBasicAttack = true;
    public float extraDelayAfterAction = 0.0f;

    [Header("Debug")]
    public bool logDecisions = false;

    // runtime state
    private string _lastMoveId = null;
    private readonly Dictionary<string, int> _cooldownLeft = new Dictionary<string, int>();

    private bool _isChargingHeavy = false;
    private SkillSO _queuedHeavySkill = null;

    private void Awake()
    {
        if (!self) self = GetComponent<CombatActor>();
        if (!executor) executor = FindObjectOfType<SkillExecutor>();
        if (!party) party = FindObjectOfType<BattlePartyManager2D>();
    }

    public IEnumerator TakeTurn(CombatActor player)
    {
        if (self == null || executor == null || player == null) yield break;
        if (self.IsDead || player.IsDead) yield break;

        // Only for special rule: max 2 actions (normal 1 action, +1 extra if Freeze-Ice triggers)
        bool extraActionGranted = false;
        int actionsDone = 0;
        int maxActionsThisTurn = 2;

        while (actionsDone < maxActionsThisTurn)
        {
            actionsDone++;

            TickCooldownsOneStep();

            MoveEntry chosen = null;

            // If charging heavy -> force heavy
            if (_isChargingHeavy && _queuedHeavySkill != null)
            {
                chosen = new MoveEntry
                {
                    id = "ForcedHeavy",
                    type = MoveType.HeavyAttack,
                    skill = _queuedHeavySkill,
                    weight = 0,
                    allowRepeat = true
                };
            }
            else
            {
                chosen = ChooseMove(player);
            }

            if (chosen == null) yield break;

            if (logDecisions)
                Debug.Log($"[EnemyAI] {name} chose: {chosen.id} ({chosen.type})", this);

            // Execute and detect special rule
            bool grantedExtraByFreezeIce = false;
            yield return ExecuteMove(chosen, player, (flag) => grantedExtraByFreezeIce = flag);

            // record last move + apply cooldown
            if (!string.IsNullOrEmpty(chosen.id))
                _lastMoveId = chosen.id;

            if (chosen.cooldownActions > 0 && !string.IsNullOrEmpty(chosen.id))
                _cooldownLeft[chosen.id] = Mathf.Max(_cooldownLeft.TryGetValue(chosen.id, out var v) ? v : 0, chosen.cooldownActions);

            // If we just executed the queued heavy, clear charging state
            if (chosen.type == MoveType.HeavyAttack && _isChargingHeavy)
            {
                _isChargingHeavy = false;
                _queuedHeavySkill = null;
            }

            if (extraDelayAfterAction > 0f)
                yield return new WaitForSeconds(extraDelayAfterAction);

            // Special: grant EXACTLY 1 extra action, only once
            if (grantedExtraByFreezeIce && !extraActionGranted)
            {
                extraActionGranted = true;
                continue; // loop again for extra action (AI will pick again)
            }

            break; // normal end: 1 action
        }
    }

    private void TickCooldownsOneStep()
    {
        if (_cooldownLeft.Count == 0) return;

        var keys = ListPool<string>.Get();
        keys.AddRange(_cooldownLeft.Keys);

        for (int i = 0; i < keys.Count; i++)
        {
            var k = keys[i];
            _cooldownLeft[k] = Mathf.Max(0, _cooldownLeft[k] - 1);
            if (_cooldownLeft[k] <= 0) _cooldownLeft.Remove(k);
        }

        ListPool<string>.Release(keys);
    }

    private MoveEntry ChooseMove(CombatActor player)
    {
        var candidates = ListPool<MoveEntry>.Get();
        int totalWeight = 0;

        float selfHp01 = (self.maxHP <= 0) ? 0f : (float)self.hp / self.maxHP;
        bool playerHasGuard = player.guardPool > 0;

        for (int i = 0; i < moves.Count; i++)
        {
            var m = moves[i];
            if (m == null) continue;

            // Must have skill unless Heal-direct or Setup
            if (m.type != MoveType.Heal && m.type != MoveType.Setup && m.skill == null)
                continue;

            // Conditions
            if (selfHp01 > Mathf.Clamp01(m.requireSelfHpPercentLE)) continue;
            if (m.requirePlayerHasGuard && !playerHasGuard) continue;
            if (m.requireNotCharging && _isChargingHeavy) continue;

            // Anti-repeat
            if (!m.allowRepeat && !string.IsNullOrEmpty(_lastMoveId) && m.id == _lastMoveId)
                continue;

            // Cooldown
            if (!string.IsNullOrEmpty(m.id) && _cooldownLeft.TryGetValue(m.id, out int cd) && cd > 0)
                continue;

            int w = Mathf.Max(0, m.weight);
            if (w <= 0) continue;

            candidates.Add(m);
            totalWeight += w;
        }

        if (candidates.Count == 0)
        {
            ListPool<MoveEntry>.Release(candidates);

            if (!fallbackToBasicAttack) return null;

            for (int i = 0; i < moves.Count; i++)
            {
                var m = moves[i];
                if (m != null && m.type == MoveType.BasicAttack && m.skill != null)
                    return m;
            }

            return null;
        }

        int roll = UnityEngine.Random.Range(0, totalWeight);
        int acc = 0;

        for (int i = 0; i < candidates.Count; i++)
        {
            var m = candidates[i];
            acc += Mathf.Max(0, m.weight);
            if (roll < acc)
            {
                ListPool<MoveEntry>.Release(candidates);
                return m;
            }
        }

        var pick = candidates[candidates.Count - 1];
        ListPool<MoveEntry>.Release(candidates);
        return pick;
    }

    // We avoid out/ref by using a callback to set the flag.
    private IEnumerator ExecuteMove(MoveEntry move, CombatActor player, Action<bool> setGrantedExtra)
    {
        setGrantedExtra?.Invoke(false);

        if (move == null || self == null || player == null) yield break;

        // SETUP: queue heavy next action
        if (move.type == MoveType.Setup)
        {
            _isChargingHeavy = true;
            _queuedHeavySkill = move.heavySkill;
            yield return new WaitForSeconds(executor.delayBetweenActions);
            yield break;
        }

        // HEAL (direct) if no skill
        if (move.type == MoveType.Heal && move.skill == null)
        {
            self.Heal(Mathf.Max(0, move.healAmount));
            yield return new WaitForSeconds(executor.delayBetweenActions);
            yield break;
        }

        if (move.skill == null) yield break;

        // Special rule detection:
        bool playerWasFrozenBefore = (player.status != null && player.status.frozen);

        // Enemies don't roll dice -> dieValue = 0
        int dieValue = 0;

        yield return executor.ExecuteSkill(move.skill, self, player, dieValue, skipCost: true);

        // If player WAS frozen, and this skill is ICE Attack, and now freeze is broken -> grant extra action
        if (playerWasFrozenBefore && player.status != null)
        {
            bool freezeBrokenNow = (player.status.frozen == false);
            bool isIceAttack = (move.skill.kind == SkillKind.Attack && move.skill.element == ElementType.Ice);

            if (freezeBrokenNow && isIceAttack)
            {
                // Your rule: ONLY this enemy gets +1 extra action, exactly once (handled in TakeTurn loop)
                setGrantedExtra?.Invoke(true);
            }
        }
    }

    // small pooled list helper
    private static class ListPool<T>
    {
        private static readonly Stack<List<T>> _pool = new Stack<List<T>>();

        public static List<T> Get()
        {
            if (_pool.Count > 0)
            {
                var l = _pool.Pop();
                l.Clear();
                return l;
            }
            return new List<T>(8);
        }

        public static void Release(List<T> l)
        {
            if (l == null) return;
            l.Clear();
            _pool.Push(l);
        }
    }
}
