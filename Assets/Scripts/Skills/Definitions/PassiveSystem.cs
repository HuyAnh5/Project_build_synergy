using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PassiveSystem : MonoBehaviour
{
    [Tooltip("Equip-only passives currently active on this actor.")]
    public List<SkillPassiveSO> equipped = new List<SkillPassiveSO>();

    [Header("Auto Sync (Prefab-safe)")]
    [Tooltip("If true, this component will automatically find RunInventoryManager in the scene and sync passives from it.")]
    [SerializeField] private bool autoSyncFromRunInventory = true;

    [Tooltip("Optional explicit reference. Leave empty on prefabs; it will be auto-found at runtime.")]
    [SerializeField] private RunInventoryManager runInventory;

    private readonly List<SkillPassiveSO> _syncBuffer = new List<SkillPassiveSO>(16);

    // Aggregates (minimal set for Batch 4)
    private int _focusBonusTurnStart;
    private float _guardGainPct = 0f;
    private int _guardFlatAtTurnEnd = 0;

    private float _damageAllPct;
    private readonly Dictionary<ElementType, float> _damageByElementPct = new Dictionary<ElementType, float>();
    private readonly List<PassiveEffectEntry> _conditionalDamage = new List<PassiveEffectEntry>();

    private float _burnConsumeMultiplier = 1f;
    private float _lightningVsMarkMultiplierAdd = 0f;
    private int _freezeBreakFocusBonusAdd = 0;

    private void Awake()
    {
        TryBindInventory();
        SyncFromInventoryIfPossible();
        Rebuild();
    }

    private void Start()
    {
        // In some setups the player prefab spawns before RunInventoryManager.
        // Retry a few times so passives come online automatically.
        if (autoSyncFromRunInventory && runInventory == null)
            StartCoroutine(RetryBindInventoryRoutine());
    }

    private void OnEnable()
    {
        TryBindInventory();
        SyncFromInventoryIfPossible();
        Rebuild();
    }

    private void OnDisable()
    {
        UnbindInventory();
    }

    private System.Collections.IEnumerator RetryBindInventoryRoutine()
    {
        const int attempts = 25;
        const float wait = 0.2f;

        for (int i = 0; i < attempts && runInventory == null; i++)
        {
            TryBindInventory();
            if (runInventory != null) break;
            yield return new WaitForSeconds(wait);
        }

        if (runInventory != null)
        {
            SyncFromInventoryIfPossible();
            Rebuild();
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying) Rebuild();
    }
#endif

    public void Rebuild()
    {
        _focusBonusTurnStart = 0;

        _damageAllPct = 0f;
        _damageByElementPct.Clear();
        _conditionalDamage.Clear();
        _guardGainPct = 0f;
        _guardFlatAtTurnEnd = 0;

        _burnConsumeMultiplier = 1f;
        _lightningVsMarkMultiplierAdd = 0f;
        _freezeBreakFocusBonusAdd = 0;

        if (equipped == null) equipped = new List<SkillPassiveSO>();

        for (int i = 0; i < equipped.Count; i++)
        {
            var p = equipped[i];
            if (p == null || p.effects == null) continue;

            for (int k = 0; k < p.effects.Count; k++)
            {
                var e = p.effects[k];
                if (e == null) continue;
                Accumulate(e);
            }
        }
    }

    // ==================== AUTO SYNC ====================
    private void TryBindInventory()
    {
        if (!autoSyncFromRunInventory) return;

        if (runInventory == null)
        {
            // Prefab-safe: find at runtime.
            // 1) same GO or parents (common if inventory is on player root)
            runInventory = GetComponentInParent<RunInventoryManager>(true);
            // 2) global singleton-style in scene
            if (runInventory == null)
            {
#if UNITY_2023_1_OR_NEWER
                runInventory = Object.FindFirstObjectByType<RunInventoryManager>(FindObjectsInactive.Include);
#else
                runInventory = Object.FindObjectOfType<RunInventoryManager>(true);
#endif
            }
        }

        if (runInventory != null)
        {
            // prevent double-subscribe
            runInventory.InventoryChanged -= OnInventoryChanged;
            runInventory.InventoryChanged += OnInventoryChanged;
        }
    }

    private void UnbindInventory()
    {
        if (runInventory != null)
            runInventory.InventoryChanged -= OnInventoryChanged;
    }

    private void OnInventoryChanged()
    {
        SyncFromInventoryIfPossible();
        Rebuild();
    }

    private void SyncFromInventoryIfPossible()
    {
        if (!autoSyncFromRunInventory) return;
        if (runInventory == null) return;

        runInventory.FillPassives(_syncBuffer);

        if (equipped == null) equipped = new List<SkillPassiveSO>();
        equipped.Clear();
        for (int i = 0; i < _syncBuffer.Count; i++)
            equipped.Add(_syncBuffer[i]);
    }

    public bool Equip(SkillPassiveSO p)
    {
        if (p == null) return false;
        if (equipped.Contains(p)) return false;
        equipped.Add(p);
        Rebuild();
        return true;
    }

    public bool IsEquipped(SkillPassiveSO p)
    {
        return p != null && equipped.Contains(p);
    }

    public bool Unequip(SkillPassiveSO p)
    {
        if (p == null) return false;
        if (!equipped.Remove(p)) return false;
        Rebuild();
        return true;
    }

    private void Accumulate(PassiveEffectEntry e)
    {
        switch (e.id)
        {
            case PassiveEffectId.FocusBonusOnTurnStart:
                _focusBonusTurnStart += e.valueI;
                break;

            case PassiveEffectId.DamagePercentAll:
                _damageAllPct += e.valueF;
                break;

            case PassiveEffectId.DamagePercentByElement:
                _damageByElementPct.TryGetValue(e.element, out var cur);
                _damageByElementPct[e.element] = cur + e.valueF;
                break;

            case PassiveEffectId.ConditionalDamagePercent:
                _conditionalDamage.Add(e);
                break;

            case PassiveEffectId.BurnConsumeDamageMultiplier:
                _burnConsumeMultiplier *= Mathf.Max(0f, e.valueF);
                break;

            case PassiveEffectId.LightningVsMarkMultiplierAdd:
                _lightningVsMarkMultiplierAdd += e.valueF;
                break;

            case PassiveEffectId.FreezeBreakFocusBonusAdd:
                _freezeBreakFocusBonusAdd += e.valueI;
                break;
            case PassiveEffectId.GuardGainPercent:
                _guardGainPct += e.valueF;
                break;

            case PassiveEffectId.GuardFlatAtTurnEnd:
                _guardFlatAtTurnEnd += e.valueI;
                break;
        }
    }

    public int GetFocusBonusOnTurnStart() => _focusBonusTurnStart;
    public float GetBurnConsumeMultiplier() => _burnConsumeMultiplier;
    public float GetLightningVsMarkMultiplierAdd() => _lightningVsMarkMultiplierAdd;
    public int GetFreezeBreakFocusBonusAdd() => _freezeBreakFocusBonusAdd;
    public float GetGuardGainPercent() => _guardGainPct;
    public int GetGuardFlatAtTurnEnd() => _guardFlatAtTurnEnd;

    public float GetOutgoingDamageMultiplier(SkillRuntime rt, CombatActor target)
    {
        if (rt == null) return 1f;

        float pct = _damageAllPct;

        if (_damageByElementPct.TryGetValue(rt.element, out var ePct))
            pct += ePct;

        for (int i = 0; i < _conditionalDamage.Count; i++)
        {
            var c = _conditionalDamage[i];
            if (c == null) continue;
            if (MatchesConditional(c, rt, target))
                pct += c.valueF;
        }

        return 1f + pct;
    }

    private bool MatchesConditional(PassiveEffectEntry e, SkillRuntime rt, CombatActor target)
    {
        // attack type
        if (e.attackType == PassiveAttackType.Melee && rt.range != RangeType.Melee) return false;
        if (e.attackType == PassiveAttackType.Ranged && rt.range != RangeType.Ranged) return false;

        // target row
        if (target != null)
        {
            if (e.targetRow == PassiveTargetRow.Front && target.row != CombatActor.RowTag.Front) return false;
            if (e.targetRow == PassiveTargetRow.Back && target.row != CombatActor.RowTag.Back) return false;
        }

        return true;
    }
}
