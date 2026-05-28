using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public partial class DraggableSkillIcon
{
    private void EnsureActiveAuraUi()
    {
        if (!enableActiveAura)
            return;

        RectTransform root = transform as RectTransform;
        if (root == null || GetActiveAuraSourceImage() == null)
            return;

        EnsureActiveAuraWavePool(GetActiveAuraWaveCount());
        EnsureIconAuraLayer(ref _activeAuraRim, "ActiveIconAura_Rim", root);
        LayoutActiveAuraLayers();
        SyncActiveAuraSprites();
    }

    private void EnsureIconAuraLayer(ref Image layer, string layerName, RectTransform root)
    {
        if (layer != null)
            return;

        GameObject go = new GameObject(layerName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rt = go.transform as RectTransform;
        rt.SetParent(root, false);
        rt.SetAsFirstSibling();

        layer = go.GetComponent<Image>();
        layer.raycastTarget = false;
        layer.preserveAspect = true;
        layer.type = Image.Type.Simple;
        layer.gameObject.SetActive(false);
    }

    private void EnsureActiveAuraWavePool(int requiredCount)
    {
        RectTransform root = transform as RectTransform;
        if (root == null)
            return;

        requiredCount = Mathf.Max(1, requiredCount);
        for (int i = _activeAuraWaves.Count; i < requiredCount; i++)
        {
            Image wave = null;
            EnsureIconAuraLayer(ref wave, $"ActiveIconAura_Wave{i + 1}", root);
            _activeAuraWaves.Add(wave);
        }

        for (int i = 0; i < _activeAuraWaves.Count; i++)
            SetAuraLayerVisible(_activeAuraWaves[i], i < requiredCount && _isActiveRuntimeSkill);
    }

    private int GetActiveAuraWaveCount()
    {
        return 2;
    }

    private void LayoutActiveAuraLayers()
    {
        for (int i = 0; i < _activeAuraWaves.Count; i++)
            LayoutAuraLayer(_activeAuraWaves[i], 0f);
        LayoutAuraLayer(_activeAuraRim, activeAuraBrightSize);
    }

    private void LayoutAuraLayer(Image layer, float expand)
    {
        if (layer == null)
            return;

        RectTransform rt = layer.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(-expand, -expand);
        rt.offsetMax = new Vector2(expand, expand);
    }

    private void SyncActiveAuraSprites()
    {
        Image sourceImage = GetActiveAuraSourceImage();
        if (sourceImage == null)
            return;

        for (int i = 0; i < _activeAuraWaves.Count; i++)
            SyncAuraLayerSprite(_activeAuraWaves[i], sourceImage);
        SyncAuraLayerSprite(_activeAuraRim, sourceImage);
    }

    private Image GetActiveAuraSourceImage()
    {
        return activeAuraSourceImage;
    }

    private void SyncAuraLayerSprite(Image layer, Image sourceImage)
    {
        if (layer == null || sourceImage == null)
            return;

        layer.sprite = sourceImage.sprite;
        layer.material = sourceImage.material;
        layer.preserveAspect = sourceImage.preserveAspect;
        layer.type = sourceImage.type;
    }

    private void ApplyActiveAuraVisibility()
    {
        if (!enableActiveAura)
            return;

        EnsureActiveAuraUi();
        LayoutActiveAuraLayers();
        SyncActiveAuraSprites();

        bool visible = _isActiveRuntimeSkill || _transientAffectedAuraRunning;
        for (int i = 0; i < _activeAuraWaves.Count; i++)
            SetAuraLayerVisible(_activeAuraWaves[i], visible);
        SetAuraLayerVisible(_activeAuraRim, visible);

        if (_transientAffectedAuraRunning)
            return;

        if (visible)
            StartActiveAuraTweens();
        else
            StopActiveAuraTweens();
    }

    private static void SetAuraLayerVisible(Image layer, bool visible)
    {
        if (layer != null && layer.gameObject.activeSelf != visible)
            layer.gameObject.SetActive(visible);
    }

    private void TickActiveAura()
    {
        if (!_isActiveRuntimeSkill)
        {
            StopActiveAuraTweens();
            return;
        }

        StartActiveAuraTweens();
    }

    private void StartActiveAuraTweens()
    {
        if (!enableActiveAura)
            return;

        if (_activeAuraTweensRunning && HasActiveAuraSettingsChanged())
            StopActiveAuraTweens();

        if (_activeAuraTweensRunning)
            return;

        int waveCount = GetActiveAuraWaveCount();
        EnsureActiveAuraWavePool(waveCount);
        for (int i = 0; i < _activeAuraWaves.Count; i++)
            SetAuraLayerVisible(_activeAuraWaves[i], i < waveCount);
        SetAuraLayerVisible(_activeAuraRim, true);

        ApplyAuraRim(_activeAuraRim, activeAuraBrightColor, 1f);

        float waveSeconds = Mathf.Max(0.1f, activeAuraWaveSeconds);
        CreateAuraWaveLoops(waveSeconds, waveCount);
        CacheActiveAuraSettings();
        _activeAuraTweensRunning = true;
    }

    private void StopActiveAuraTweens()
    {
        for (int i = 0; i < _activeAuraTweens.Count; i++)
            _activeAuraTweens[i]?.Kill();
        _activeAuraTweens.Clear();
        _activeAuraTweensRunning = false;
        CacheActiveAuraSettings();
    }

    private void ApplyAuraRim(Image layer, Color color, float alphaScale)
    {
        if (layer == null)
            return;

        LayoutAuraLayer(layer, Mathf.Max(0f, activeAuraBrightSize));

        Color c = color;
        c.a = Mathf.Clamp01(color.a * alphaScale);
        layer.color = c;
    }

    private bool HasActiveAuraSettingsChanged()
    {
        return !Mathf.Approximately(_lastAuraWaveSeconds, activeAuraWaveSeconds)
            || !Mathf.Approximately(_lastAuraWaveSize, activeAuraWaveSize)
            || !Mathf.Approximately(_lastAuraBrightSize, activeAuraBrightSize);
    }

    private void CacheActiveAuraSettings()
    {
        _lastAuraWaveSeconds = activeAuraWaveSeconds;
        _lastAuraWaveSize = activeAuraWaveSize;
        _lastAuraBrightSize = activeAuraBrightSize;
    }

    private void CreateAuraWaveLoops(float waveDuration, int waveCount)
    {
        int activeWaveCount = Mathf.Min(waveCount, _activeAuraWaves.Count);
        float halfWaveDelay = waveDuration * 0.5f;

        for (int i = 0; i < activeWaveCount; i++)
        {
            Image waveLayer = _activeAuraWaves[i];
            float delay = i == 0 ? 0f : halfWaveDelay;
            float alphaScale = i == 0 ? 0.78f : 0.55f;
            Tween waveTween = CreateAuraWaveTween(waveLayer, activeAuraWaveColor, alphaScale, waveDuration)
                .SetLoops(-1, LoopType.Restart);

            if (delay <= 0f)
            {
                _activeAuraTweens.Add(waveTween);
                continue;
            }

            Sequence delayedStart = DOTween.Sequence()
                .SetUpdate(true)
                .AppendInterval(delay)
                .Append(waveTween);
            _activeAuraTweens.Add(delayedStart);
        }
    }

    private Tween CreateAuraWaveTween(Image layer, Color color, float alphaScale, float waveDuration)
    {
        if (layer == null)
            return DOTween.Sequence();

        float startExpand = 0f;
        float maxExpand = Mathf.Max(0f, activeAuraWaveSize);
        Color startColor = color;
        startColor.a = Mathf.Clamp01(color.a * alphaScale);
        Color endColor = startColor;
        endColor.a = 0f;

        return DOTween.To(
                () => 0f,
                t =>
                {
                    float easedExpand = DOVirtual.EasedValue(0f, 1f, t, Ease.OutQuad);
                    float easedFade = DOVirtual.EasedValue(0f, 1f, t, Ease.InOutSine);
                    LayoutAuraLayer(layer, Mathf.Lerp(startExpand, maxExpand, easedExpand));
                    layer.color = Color.LerpUnclamped(startColor, endColor, easedFade);
                },
                1f,
                waveDuration)
            .SetEase(Ease.Linear)
            .OnRewind(() =>
            {
                LayoutAuraLayer(layer, startExpand);
                layer.color = startColor;
            });
    }
}
