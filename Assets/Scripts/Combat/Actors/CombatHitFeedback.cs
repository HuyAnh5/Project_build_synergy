using System.Collections;
using UnityEngine;

/// <summary>
/// Small runtime-only combat feedback helper.
/// Feedback is attached to the resolved effect, not to the skill element:
/// damage -> hit shake/flash, guard -> shield pulse, heal -> heal pulse,
/// focus -> energy pulse, burn consume -> fire burst, mark payoff -> mark burst.
/// </summary>
public class CombatHitFeedback : MonoBehaviour
{
    public enum FeedbackKind
    {
        Hit,
        Guard,
        Heal,
        Focus,
        BurnConsume,
        MarkPayoff
    }

    [Header("Shake")]
    [SerializeField] private float shakeDuration = 0.12f;
    [SerializeField] private float shakeStrength = 0.035f;
    [SerializeField] private int shakeOscillations = 4;

    [Header("Flash")]
    [SerializeField] private float flashDuration = 0.12f;
    [SerializeField] private Color hitFlash = Color.white;
    [SerializeField] private Color guardFlash = new Color(0.35f, 0.75f, 1f, 1f);
    [SerializeField] private Color healFlash = new Color(0.35f, 1f, 0.55f, 1f);
    [SerializeField] private Color focusFlash = new Color(0.75f, 0.55f, 1f, 1f);
    [SerializeField] private Color burnFlash = new Color(1f, 0.35f, 0.08f, 1f);
    [SerializeField] private Color markFlash = new Color(1f, 0.9f, 0.2f, 1f);

    [Header("Burst")]
    [SerializeField] private bool spawnProceduralBursts = true;
    [SerializeField] private float burstLifetime = 0.58f;
    [SerializeField] private Material particleMaterial;

    private SpriteRenderer[] _sprites;
    private Color[] _baseColors;
    private Coroutine _flashRoutine;
    private Coroutine _shakeRoutine;
    private Vector3 _baseLocalPosition;

    private void Awake()
    {
        CacheSprites();
    }

    public static void Play(CombatActor actor, FeedbackKind kind)
    {
        if (actor == null || !actor.gameObject.activeInHierarchy)
            return;

        CombatHitFeedback feedback = actor.GetComponent<CombatHitFeedback>();
        if (feedback == null)
            feedback = actor.gameObject.AddComponent<CombatHitFeedback>();

        feedback.Play(kind);
    }

    public void Play(FeedbackKind kind)
    {
        CacheSprites();

        if (kind == FeedbackKind.Hit)
            StartShake();

        StartFlash(GetFlashColor(kind));

        if (spawnProceduralBursts && ShouldSpawnProceduralBurst(kind))
            SpawnBurst(kind);
    }

    private static bool ShouldSpawnProceduralBurst(FeedbackKind kind)
    {
        return kind != FeedbackKind.Hit &&
               kind != FeedbackKind.Guard;
    }

    private void CacheSprites()
    {
        _sprites = GetComponentsInChildren<SpriteRenderer>(true);
        _baseColors = new Color[_sprites.Length];
        for (int i = 0; i < _sprites.Length; i++)
            _baseColors[i] = _sprites[i] != null ? _sprites[i].color : Color.white;
    }

    private void StartShake()
    {
        if (_shakeRoutine != null)
        {
            StopCoroutine(_shakeRoutine);
            transform.localPosition = _baseLocalPosition;
        }

        _baseLocalPosition = transform.localPosition;
        _shakeRoutine = StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        float t = 0f;
        while (t < shakeDuration)
        {
            t += Time.deltaTime;
            float normalized = Mathf.Clamp01(t / shakeDuration);
            float fade = 1f - normalized;
            float x = Mathf.Sin(normalized * Mathf.PI * shakeOscillations) * shakeStrength * fade;
            transform.localPosition = _baseLocalPosition + new Vector3(x, 0f, 0f);
            yield return null;
        }

        transform.localPosition = _baseLocalPosition;
        _shakeRoutine = null;
    }

    private void StartFlash(Color color)
    {
        if (_flashRoutine != null)
            StopCoroutine(_flashRoutine);
        _flashRoutine = StartCoroutine(FlashRoutine(color));
    }

    private IEnumerator FlashRoutine(Color color)
    {
        float half = Mathf.Max(0.01f, flashDuration * 0.5f);
        float t = 0f;

        while (t < half)
        {
            t += Time.deltaTime;
            ApplyTint(Color.Lerp(Color.white, color, Mathf.Clamp01(t / half)));
            yield return null;
        }

        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            ApplyTint(Color.Lerp(color, Color.white, Mathf.Clamp01(t / half)));
            yield return null;
        }

        RestoreColors();
        _flashRoutine = null;
    }

    private void ApplyTint(Color tint)
    {
        if (_sprites == null)
            return;

        for (int i = 0; i < _sprites.Length; i++)
        {
            if (_sprites[i] == null)
                continue;
            Color baseColor = i < _baseColors.Length ? _baseColors[i] : Color.white;
            _sprites[i].color = baseColor * tint;
        }
    }

    private void RestoreColors()
    {
        if (_sprites == null || _baseColors == null)
            return;

        for (int i = 0; i < _sprites.Length && i < _baseColors.Length; i++)
        {
            if (_sprites[i] != null)
                _sprites[i].color = _baseColors[i];
        }
    }

    private Color GetFlashColor(FeedbackKind kind)
    {
        switch (kind)
        {
            case FeedbackKind.Guard: return guardFlash;
            case FeedbackKind.Heal: return healFlash;
            case FeedbackKind.Focus: return focusFlash;
            case FeedbackKind.BurnConsume: return burnFlash;
            case FeedbackKind.MarkPayoff: return markFlash;
            default: return hitFlash;
        }
    }

    private void SpawnBurst(FeedbackKind kind)
    {
        GameObject burst = new GameObject($"{kind}FeedbackBurst");
        burst.transform.SetParent(transform, false);
        burst.transform.localPosition = Vector3.zero;

        ParticleSystem ps = burst.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 0.12f;
        main.startLifetime = burstLifetime;
        main.startSpeed = new ParticleSystem.MinMaxCurve(GetBurstSpeed(kind) * 0.55f, GetBurstSpeed(kind));
        main.startSize = new ParticleSystem.MinMaxCurve(GetBurstSize(kind) * 0.55f, GetBurstSize(kind));
        main.startColor = GetFlashColor(kind);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, GetBurstCount(kind)) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.11f;
        shape.radiusThickness = 1f;
        shape.randomDirectionAmount = 0.65f;

        ParticleSystem.NoiseModule noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.12f;
        noise.frequency = 1.8f;
        noise.scrollSpeed = 0.6f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        Color color = GetFlashColor(kind);
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(color, 0f),
                new GradientColorKey(color, 0.65f),
                new GradientColorKey(color, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.95f, 0f),
                new GradientAlphaKey(0.8f, 0.45f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient;

        ParticleSystemRenderer renderer = burst.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = GetTopSortingOrder() + 5;
        string sortingLayer = GetSortingLayerName();
        if (!string.IsNullOrEmpty(sortingLayer))
            renderer.sortingLayerName = sortingLayer;
        renderer.material = particleMaterial != null ? particleMaterial : GetDefaultParticleMaterial();

        ps.Play(true);
        Destroy(burst, burstLifetime + 0.25f);
    }

    private static float GetBurstSpeed(FeedbackKind kind)
    {
        switch (kind)
        {
            case FeedbackKind.BurnConsume: return 0.55f;
            case FeedbackKind.MarkPayoff: return 0.65f;
            default: return 0.35f;
        }
    }

    private static float GetBurstSize(FeedbackKind kind)
    {
        switch (kind)
        {
            case FeedbackKind.BurnConsume: return 0.06f;
            case FeedbackKind.MarkPayoff: return 0.052f;
            default: return 0.055f;
        }
    }

    private static short GetBurstCount(FeedbackKind kind)
    {
        switch (kind)
        {
            case FeedbackKind.BurnConsume: return 16;
            case FeedbackKind.MarkPayoff: return 14;
            default: return 12;
        }
    }

    private int GetTopSortingOrder()
    {
        int topOrder = 0;
        if (_sprites == null || _sprites.Length == 0)
            CacheSprites();

        for (int i = 0; i < _sprites.Length; i++)
        {
            if (_sprites[i] != null && _sprites[i].sortingOrder > topOrder)
                topOrder = _sprites[i].sortingOrder;
        }

        return topOrder;
    }

    private string GetSortingLayerName()
    {
        if (_sprites == null || _sprites.Length == 0)
            CacheSprites();

        for (int i = 0; i < _sprites.Length; i++)
        {
            if (_sprites[i] != null)
                return _sprites[i].sortingLayerName;
        }

        return string.Empty;
    }

    private static Material _defaultParticleMaterial;

    private static Material GetDefaultParticleMaterial()
    {
        if (_defaultParticleMaterial != null)
            return _defaultParticleMaterial;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");

        _defaultParticleMaterial = shader != null
            ? new Material(shader)
            : null;

        return _defaultParticleMaterial;
    }
}
