using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Spawns and recycles combat popup texts for damage, guard, healing, and focus gain.
/// </summary>
public partial class DamagePopupSystem : MonoBehaviour
{
    private const string PopupCanvasName = "CombatPopupCanvas";

    private sealed class TotalDamageState
    {
        public TMP_Text popup;
        public int total;
        public int hitCount;
        public float lastHitTime;
    }

    [Header("Prefab & Parent (UI or World)")]
    [Tooltip("Prefab nên là TextMeshProUGUI (UI) hoặc TMP_Text (world).")]
    [SerializeField] private TMP_Text popupPrefab;

    [Tooltip("Nếu popup là UI TextMeshProUGUI: đặt spawnParent là RectTransform dưới Canvas (PopUpDMG). Nếu để null: sẽ spawn theo transform của DamagePopupSystem.")]
    [SerializeField] private Transform spawnParent;

    [Header("Pool")]
    [SerializeField] private int prewarmCount = 30;
    [SerializeField] private bool allowExpand = true;

    [Header("Scale (ALL types): normal -> bigger -> shrink+fade")]
    [SerializeField] private float popUpScale = 1.25f;
    [SerializeField] private float popUpTime = 0.10f;
    [SerializeField] private float shrinkTo = 0.75f;
    [SerializeField] private float shrinkTime = 0.70f;

    [Header("Timing")]
    [SerializeField] private float hpDuration = 0.90f;
    [SerializeField] private float guardDuration = 0.85f;
    [SerializeField] private float totalDamageDuration = 1.10f;
    [SerializeField] private float totalDamageFadeDuration = 0.22f;

    [Header("HP/Heal Arc (world or UI local units)")]
    [SerializeField] private float arcUp = 0.45f;
    [SerializeField] private float arcDown = 0.95f;
    [SerializeField] private float arcSide = 0.35f;
    [SerializeField] private float arcSideDrift = 0.10f;

    [Header("Total Damage Float")]
    [SerializeField] private float totalDamageUp = 0.45f;
    [SerializeField] private float totalDamageStartScale = 1f;
    [SerializeField] private float totalDamagePopScale = 1.25f;
    [SerializeField] private float totalDamageSettleScale = 1f;

    [Header("Guard S-curve (world or UI local units)")]
    [SerializeField] private float sUp = 1.00f;
    [SerializeField] private float sAmp = 0.20f;
    [SerializeField] private float sSideDrift = 0.12f;

    [Header("Colors")]
    [SerializeField] private Color hpColor = Color.white;
    [SerializeField] private Color guardColor = new Color(0.3f, 0.75f, 1f, 1f);
    [SerializeField] private Color totalDamageColor = new Color(1f, 0.82f, 0.22f, 1f);
    [SerializeField] private Color healColor = new Color(0.25f, 1f, 0.35f, 1f);
    [SerializeField] private Color focusGainColor = new Color(0.22f, 0.74f, 1f, 1f);

    [Header("Spawn Offsets")]
    [Tooltip("Điểm bắt đầu popup HP/Heal tính từ uiAnchor hoặc transform của target.")]
    [SerializeField] private Vector3 hpSpawnOffset = new Vector3(0f, 1.2f, 0f);

    [Tooltip("Điểm bắt đầu popup Guard/Focus tính từ uiAnchor hoặc transform của target.")]
    [SerializeField] private Vector3 guardSpawnOffset = new Vector3(-0.25f, 1f, 0f);

    [Tooltip("Điểm bắt đầu popup vàng tổng damage tính từ uiAnchor hoặc transform của target.")]
    [SerializeField] private Vector3 totalDamageSpawnOffset = new Vector3(0f, 1.65f, 0f);

    [Header("Debug")]
    [SerializeField] private bool logIfSpawnFails = true;

    private readonly Queue<TMP_Text> m_pool = new Queue<TMP_Text>(64);
    private readonly Dictionary<CombatActor, TotalDamageState> m_activeTotalDamagePopups = new Dictionary<CombatActor, TotalDamageState>();

    /// <summary>
    /// Exposes the focus-gain color to other UI components without reopening the serialized field.
    /// </summary>
    public Color FocusGainColor
    {
        get { return focusGainColor; }
    }

    private void Awake()
    {
        EnsureSpawnParent();
        Prewarm();
    }

    /// <summary>
    /// Pre-instantiates popup instances so combat does not allocate on the first few hits.
    /// </summary>
    public void Prewarm()
    {
        if (popupPrefab == null)
        {
            LogMissingPrefabWarning();
            return;
        }

        while (m_pool.Count < prewarmCount)
        {
            TMP_Text popup = InstantiatePopupInstance();
            popup.gameObject.SetActive(false);
            m_pool.Enqueue(popup);
        }
    }

    /// <summary>
    /// Spawns guard and HP popups for a split damage result.
    /// </summary>
    public void SpawnDamageSplit(CombatActor attacker, CombatActor target, int blocked, int hpLost)
    {
        if (target == null)
        {
            return;
        }

        if (blocked > 0)
        {
            SpawnGuardSCurve(attacker, target, blocked.ToString());
        }

        if (hpLost > 0)
        {
            RegisterTotalDamageHit(attacker, target, hpLost);
            SpawnHpArc(attacker, target, hpLost.ToString(), hpColor);
        }
    }

    /// <summary>
    /// Spawns a healing popup using the HP animation path but a dedicated color.
    /// </summary>
    public void SpawnHeal(CombatActor healer, CombatActor target, int amount)
    {
        if (target == null)
        {
            return;
        }

        int safeAmount = Mathf.Max(0, amount);
        if (safeAmount <= 0)
        {
            return;
        }

        SpawnHpArc(healer, target, safeAmount.ToString(), healColor);
    }

    /// <summary>
    /// Spawns the popup used when the player gains focus from gameplay effects.
    /// </summary>
    public void SpawnFocusGain(CombatActor source, CombatActor target, int amount)
    {
        if (target == null)
        {
            return;
        }

        int safeAmount = Mathf.Max(0, amount);
        if (safeAmount <= 0)
        {
            return;
        }

        SpawnGuardSCurve(source, target, safeAmount.ToString(), focusGainColor);
    }

    private TMP_Text Rent()
    {
        if (m_pool.Count > 0)
        {
            return m_pool.Dequeue();
        }

        if (!allowExpand)
        {
            return null;
        }

        if (popupPrefab == null)
        {
            return null;
        }

        TMP_Text popup = InstantiatePopupInstance();
        popup.gameObject.SetActive(false);
        return popup;
    }

    private void Return(TMP_Text popup)
    {
        if (popup == null)
        {
            return;
        }

        popup.DOKill();
        popup.transform.DOKill();

        Color color = popup.color;
        color.a = 1f;
        popup.color = color;
        popup.transform.localScale = Vector3.one;
        popup.raycastTarget = false;
        popup.gameObject.SetActive(false);

        m_pool.Enqueue(popup);
    }

    private TMP_Text InstantiatePopupInstance()
    {
        return Instantiate(popupPrefab, spawnParent != null ? spawnParent : transform);
    }

    private void EnsureSpawnParent()
    {
        if (spawnParent != null || popupPrefab == null)
            return;

        if (!(popupPrefab is TextMeshProUGUI))
            return;

        Canvas existing = FindPopupCanvas();
        if (existing == null)
        {
            GameObject canvasGo = new GameObject(
                PopupCanvasName,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(UnityEngine.UI.GraphicRaycaster));

            existing = canvasGo.GetComponent<Canvas>();
            existing.renderMode = RenderMode.ScreenSpaceOverlay;
            existing.overrideSorting = true;
            existing.sortingOrder = 31000;
            existing.pixelPerfect = false;

            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        spawnParent = existing.transform;
    }

    private static Canvas FindPopupCanvas()
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas != null && canvas.name == PopupCanvasName)
                return canvas;
        }

        return null;
    }

    private void RegisterTotalDamageHit(CombatActor attacker, CombatActor target, int amount)
    {
        if (attacker == null || !attacker.isPlayer || target == null)
        {
            return;
        }

        int safeAmount = Mathf.Max(0, amount);
        if (safeAmount <= 0)
        {
            return;
        }

        float now = Time.unscaledTime;
        float comboWindow = Mathf.Max(0.01f, totalDamageDuration);

        if (!m_activeTotalDamagePopups.TryGetValue(target, out TotalDamageState state) || state == null)
        {
            state = new TotalDamageState();
            m_activeTotalDamagePopups[target] = state;
        }
        else if (now - state.lastHitTime > comboWindow)
        {
            if (state.popup != null)
            {
                Return(state.popup);
            }

            state.popup = null;
            state.total = 0;
            state.hitCount = 0;
        }

        state.total += safeAmount;
        state.hitCount++;
        state.lastHitTime = now;

        if (state.hitCount >= 2)
        {
            SpawnTotalDamage(target, state.total);
        }
    }

    private void LogMissingPrefabWarning()
    {
        if (logIfSpawnFails)
        {
            Debug.LogWarning("[DamagePopupSystem] popupPrefab is NULL.", this);
        }
    }
}
