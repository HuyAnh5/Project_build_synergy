using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class CombatActor : MonoBehaviour
{
    public const string DefaultWorldUiTag = "Standard";

    public enum TeamSide { Ally, Enemy }
    public enum RowTag { Front, Back }

    [Header("Identity")]
    public TeamSide team = TeamSide.Enemy;
    public RowTag row = RowTag.Front;

    [Tooltip("Player usually uses its own HUD, but can still expose world UI.")]
    public bool isPlayer = false;

    [Header("Stats")]
    public int maxHP = 30;
    public int hp = 30;
    public int maxFocus = 9;
    public int focus = 2;

    [Tooltip("Battle start focus for non-player actors.")]
    public int startingFocus = 2;

    public int guardPool = 0;

    private bool _deathStateApplied;

    [Header("Refs")]
    public Transform firePoint;
    public StatusController status;

    [Header("World UI")]
    [Tooltip("Tag world UI dùng để resolve prefab từ BattlePartyManager2D.")]
    public string worldUiTag = DefaultWorldUiTag;
    [Tooltip("World UI anchor. If empty, one is auto-created at the actor visual center.")]
    public Transform uiAnchor;
    public bool autoSetupUiAnchor = true;
    public string uiAnchorName = "UIAnchor";

    public string GetResolvedWorldUiTag()
    {
        return string.IsNullOrWhiteSpace(worldUiTag)
            ? DefaultWorldUiTag
            : worldUiTag.Trim();
    }

    private void Awake()
    {
        if (!status) status = GetComponent<StatusController>();
        EnsureUiAnchor(alignExistingAnchorToVisualCenter: true);
    }

    private void OnValidate()
    {
        EnsureUiAnchor(alignExistingAnchorToVisualCenter: false);
    }

    [ContextMenu("Align UI Anchor To Visual Center")]
    public void AlignUiAnchorToVisualCenter()
    {
        EnsureUiAnchor(alignExistingAnchorToVisualCenter: true);
    }

    private void EnsureUiAnchor(bool alignExistingAnchorToVisualCenter)
    {
        if (!autoSetupUiAnchor)
        {
            if (!uiAnchor) uiAnchor = transform;
            return;
        }

        if (!CanMutateHierarchyInEditor())
        {
            if (!uiAnchor)
                uiAnchor = transform;
            return;
        }

        bool createdAnchor = false;
        if (uiAnchor == null)
        {
            Transform existing = transform.Find(uiAnchorName);
            if (existing != null)
                uiAnchor = existing;
        }

        if (uiAnchor == null)
        {
            GameObject go = new GameObject(uiAnchorName);
            uiAnchor = go.transform;
            uiAnchor.SetParent(transform, false);
            createdAnchor = true;
        }

        if (createdAnchor || alignExistingAnchorToVisualCenter)
        {
            uiAnchor.localRotation = Quaternion.identity;
            uiAnchor.localScale = Vector3.one;
            uiAnchor.localPosition = GetVisualCenterLocalPosition();
        }
    }

    private Vector3 GetVisualCenterLocalPosition()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Bounds combined = default;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;
            if (!renderer.enabled)
                continue;
            if (renderer.transform == uiAnchor)
                continue;
            if (renderer is ParticleSystemRenderer)
                continue;

            if (!hasBounds)
            {
                combined = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                combined.Encapsulate(renderer.bounds);
            }
        }

        Vector3 worldCenter = hasBounds ? combined.center : transform.position;
        return transform.InverseTransformPoint(worldCenter);
    }

    private bool CanMutateHierarchyInEditor()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && EditorUtility.IsPersistent(this) && !PrefabUtility.IsPartOfPrefabInstance(this))
            return false;
#endif
        return true;
    }

    public bool TrySpendFocus(int amount)
    {
        if (amount <= 0) return true;
        if (focus < amount) return false;
        focus -= amount;
        return true;
    }

    public void GainFocus(int amount)
    {
        focus = Mathf.Clamp(focus + amount, 0, maxFocus);
    }

    public void SetGuard(int value)
    {
        guardPool = Mathf.Max(0, value);
    }

    public void AddGuard(int amount)
    {
        if (amount == 0) return;
        guardPool = Mathf.Max(0, guardPool + amount);
    }

    public struct DamageResult
    {
        public int requested;
        public int blocked;
        public int hpLost;
        public bool guardBroken;
    }

    public DamageResult TakeDamageDetailed(int dmg, bool bypassGuard)
    {
        DamageResult r = new DamageResult { requested = Mathf.Max(0, dmg), blocked = 0, hpLost = 0, guardBroken = false };

        int remaining = r.requested;
        int guardBeforeHit = guardPool;

        if (!bypassGuard && guardPool > 0)
        {
            int blocked = Mathf.Min(guardPool, remaining);
            guardPool -= blocked;
            remaining -= blocked;
            r.blocked = blocked;
            r.guardBroken = guardBeforeHit > 0 && guardPool <= 0 && blocked > 0;
        }

        if (remaining > 0)
        {
            int before = hp;
            hp = Mathf.Max(0, hp - remaining);
            r.hpLost = before - hp;
        }

        HandleLifeStateChanged();

        return r;
    }

    public void TakeDamage(int dmg, bool bypassGuard)
    {
        int remaining = dmg;

        if (!bypassGuard && guardPool > 0)
        {
            int blocked = Mathf.Min(guardPool, remaining);
            guardPool -= blocked;
            remaining -= blocked;
        }

        if (remaining > 0)
            hp = Mathf.Max(0, hp - remaining);

        HandleLifeStateChanged();
    }

    public int Heal(int amount)
    {
        if (amount <= 0) return 0;

        int before = hp;
        hp = Mathf.Clamp(hp + amount, 0, maxHP);
        int healed = hp - before;
        if (healed <= 0) return 0;

        var pop = Object.FindObjectOfType<DamagePopupSystem>();
        if (pop != null)
            pop.SpawnHeal(this, this, healed);

        return healed;
    }

    public void ResetForBattle(bool resetHp)
    {
        _deathStateApplied = false;
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (resetHp)
            hp = maxHP;

        int battleStartFocus = isPlayer ? 2 : startingFocus;
        focus = Mathf.Clamp(battleStartFocus, 0, maxFocus);
        guardPool = 0;

        if (status)
            status.ClearAll();

        PassiveSystem passiveSystem = GetComponent<PassiveSystem>();
        if (passiveSystem != null)
            passiveSystem.OnCombatStarted();

        SkillCombatState skillCombatState = GetComponent<SkillCombatState>();
        if (skillCombatState != null)
            skillCombatState.ResetForBattle();
    }

    public bool IsDead => hp <= 0;

    private void HandleLifeStateChanged()
    {
        bool dead = hp <= 0;
        if (!dead)
        {
            _deathStateApplied = false;
            return;
        }

        if (_deathStateApplied)
            return;

        _deathStateApplied = true;

        if (CombatSimulationContext.SuppressPresentation)
            return;

        BattlePartyManager2D party = Object.FindFirstObjectByType<BattlePartyManager2D>(FindObjectsInactive.Include);
        if (party != null)
            party.LayoutAll();

        gameObject.SetActive(false);
    }
}

internal static class CombatSimulationContext
{
    public static bool SuppressPresentation { get; private set; }

    public readonly struct Scope : System.IDisposable
    {
        private readonly bool _previous;

        public Scope(bool suppressPresentation)
        {
            _previous = SuppressPresentation;
            SuppressPresentation = suppressPresentation;
        }

        public void Dispose()
        {
            SuppressPresentation = _previous;
        }
    }
}
