using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class ActorWorldUI
{
    public void ShowTargetPreview(TargetPreviewData data)
    {
        _targetPreviewActive = true;
        _previewData = data;
        EnsureHpPreviewFill();

        // --- Tính trạng thái thanh HP ---
        int maxHp = Mathf.Max(1, data.currentMaxHp);
        int hpAfter = data.previewHpAfter;
        int hpBefore = data.currentHp;
        int guardAfter = data.previewGuardAfter;

        // Stagger: nếu guard bị phá HOẶC target đã stagger sẵn
        bool willBeStaggered = data.willBreakGuard || data.currentlyStaggered;

        // --- Render HP bar chính với HP after (phần còn lại sau action) ---
        if (hpBarFill != null)
        {
            // Nếu hồi máu, hpBarFill chính vẫn giữ mức cũ (hpBefore) để fill phụ xanh nhấp nháy lộ ra.
            hpBarFill.fillAmount = data.hpLost < 0 ? Mathf.Clamp01((float)hpBefore / maxHp) : Mathf.Clamp01((float)hpAfter / maxHp);
            hpBarFill.color = willBeStaggered ? hpStaggerFillColor : (guardAfter > 0 ? hpGuardFillColor : hpFillColor);
        }

        // --- Render phần cam/xanh lá: HP sắp mất/hồi ---
        if (_hpPreviewFill != null)
        {
            if (data.hpLost > 0) // Mất máu
            {
                // FillAmount = toàn bộ phần HP trước action (cam + đỏ tạo visual đúng)
                _hpPreviewFill.fillAmount = Mathf.Clamp01((float)hpBefore / maxHp);
                _hpPreviewFill.color = hpPreviewDamageColor;
                _hpPreviewFill.gameObject.SetActive(true);
            }
            else if (data.hpLost < 0) // Hồi máu
            {
                // FillAmount = toàn bộ phần HP sau action (xanh + đỏ tạo visual đúng)
                _hpPreviewFill.fillAmount = Mathf.Clamp01((float)hpAfter / maxHp);
                _hpPreviewFill.color = hpHealBlinkColor;
                _hpPreviewFill.gameObject.SetActive(true);
            }
            else
            {
                _hpPreviewFill.gameObject.SetActive(false);
            }
        }

        // --- HP text preview ---
        if (hpText != null)
        {
            if (data.hpLost < 0)
                hpText.text = $"{Mathf.Max(0, hpAfter)}/{maxHp} (+{-data.hpLost})";
            else if (data.hpLost > 0)
                hpText.text = $"{Mathf.Max(0, hpAfter)}/{maxHp} (-{data.hpLost})";
            else
                hpText.text = $"{Mathf.Max(0, hpAfter)}/{maxHp}";
            // Color sẽ nhấp nháy trong UpdateTargetPreviewBlink
        }

        // --- Guard preview ---
        if (guardRoot != null && Application.isPlaying && autoToggleGuardRootInPlayMode)
            guardRoot.gameObject.SetActive(guardAfter > 0);
        if (guardText != null)
            guardText.text = Mathf.Max(0, guardAfter).ToString();

        // --- Outline preview ---
        if (hpBarOutline != null)
            hpBarOutline.effectColor = (willBeStaggered || guardAfter > 0) ? hpProtectedOutlineColor : hpOutlineColor;

        if (hpBarBackground != null)
            hpBarBackground.color = hpBarBackgroundColor;

        // --- Status icons preview ---
        BuildPreviewStatusBufferFromData(data);
        ApplyStatusBuffer();
    }

    /// <summary>
    /// Tắt preview, quay về hiển thị state thật của actor.
    /// </summary>
    public void ClearTargetPreview()
    {
        if (!_targetPreviewActive)
            return;

        _targetPreviewActive = false;

        if (_hpPreviewFill != null)
            _hpPreviewFill.gameObject.SetActive(false);

        // Reset lại màu text về bình thường trước khi refresh
        if (hpText != null)
            hpText.color = hpTextNormalColor;
        if (guardText != null)
            guardText.color = Color.white;

        // Force refresh lại state thật ngay lập tức
        if (actor != null)
        {
            bool staggered = actor.status != null && actor.status.staggered;
            RefreshHpAndGuard(actor.hp, actor.maxHP, actor.guardPool, staggered);
            RefreshStatusIcons(actor.status);
        }
    }

    private void UpdateTargetPreviewBlink()
    {
        float t = Mathf.PingPong(Time.time * hpPreviewBlinkSpeed, 1f);

        // --- HP preview fill (cam hoặc xanh) nhấp nháy ---
        if (_hpPreviewFill != null && _hpPreviewFill.gameObject.activeSelf)
        {
            Color baseColor = _previewData.hpLost < 0 ? hpHealBlinkColor : hpPreviewDamageColor;
            Color c = baseColor;
            c.a = Mathf.Lerp(hpPreviewMinAlpha, baseColor.a, t);
            _hpPreviewFill.color = c;
        }

        // --- HP text nhấp nháy nếu có thay đổi ---
        if (hpText != null && _previewData.hpLost != 0)
        {
            Color baseColor = _previewData.hpLost < 0 ? hpHealBlinkColor : hpPreviewDamageColor;
            Color textColor = Color.Lerp(baseColor, Color.white, t);
            hpText.color = textColor;
        }

        // --- Guard text nhấp nháy nếu có thay đổi ---
        if (guardText != null && _previewData.previewGuardAfter != _previewData.currentGuard)
        {
            Color textColor = Color.Lerp(hpPreviewDamageColor, Color.white, t);
            guardText.color = textColor;
        }

        // --- Status nhấp nháy ---
        for (int i = 0; i < statusSlots.Length; i++)
        {
            if (i >= _statusBuffer.Count) break;
            StatusIconSlot slot = statusSlots[i];
            StatusVisualData data = _statusBuffer[i];
            if (slot == null || slot.root == null || !slot.root.gameObject.activeSelf) continue;

            bool isBlinking = false;
            bool isConsume = false;
            
            if (data.shortLabel == "BU")
            {
                if (_previewData.previewBurnAfter > _previewData.currentBurn) isBlinking = true;
                else if (_previewData.previewBurnAfter < _previewData.currentBurn) { isBlinking = true; isConsume = true; }
            }
            else if (data.shortLabel == "BL")
            {
                if (_previewData.previewBleedAfter > _previewData.currentBleed) isBlinking = true;
                else if (_previewData.previewBleedAfter < _previewData.currentBleed) { isBlinking = true; isConsume = true; }
            }
            else if (data.shortLabel == "MK")
            {
                isBlinking = _previewData.willTriggerMarkShock;
            }

            if (isBlinking)
            {
                Color blinkColor = Color.Lerp(hpPreviewDamageColor, Color.white, t);
                if (isConsume)
                {
                    if (slot.iconImage != null) slot.iconImage.color = blinkColor;
                    if (slot.valueText != null) slot.valueText.color = blinkColor;
                }
                else
                {
                    if (slot.iconImage != null && data.shortLabel == "MK") slot.iconImage.color = blinkColor;
                    if (slot.valueText != null) slot.valueText.color = blinkColor;
                }
            }
        }
    }

    private void EnsureHpPreviewFill()
    {
        if (_hpPreviewFill != null)
            return;

        if (hpBarFill == null)
            return;

        // Tạo Image fill phụ nằm phía sau fill chính → phần dư ra = phần cam
        GameObject go = new GameObject("HpPreviewFill", typeof(RectTransform), typeof(Image));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(hpBarFill.rectTransform.parent, false);

        // Copy layout từ fill chính
        RectTransform fillRt = hpBarFill.rectTransform;
        rt.anchorMin = fillRt.anchorMin;
        rt.anchorMax = fillRt.anchorMax;
        rt.pivot = fillRt.pivot;
        rt.offsetMin = fillRt.offsetMin;
        rt.offsetMax = fillRt.offsetMax;
        rt.sizeDelta = fillRt.sizeDelta;
        rt.anchoredPosition = fillRt.anchoredPosition;

        // Thứ tự: Background → PreviewFill (cam) → MainFill → HpText (luôn trên cùng)
        // Đặt preview fill TRUỚC fill chính (sibling index thấp hơn = render trước)
        rt.SetSiblingIndex(fillRt.GetSiblingIndex()); // sau bước này previewFill cùng index, fillRt bị đẩy lên +1

        // Đảm bảo hpText luôn là sibling cuối (render trên cùng)
        if (hpText != null)
            hpText.rectTransform.SetAsLastSibling();

        _hpPreviewFill = go.GetComponent<Image>();
        _hpPreviewFill.sprite = hpBarFill.sprite;
        _hpPreviewFill.type = Image.Type.Filled;
        _hpPreviewFill.fillMethod = hpBarFill.fillMethod;
        _hpPreviewFill.fillOrigin = hpBarFill.fillOrigin;
        _hpPreviewFill.color = hpPreviewDamageColor;
        _hpPreviewFill.raycastTarget = false;
        go.SetActive(false);
    }

    private void BuildPreviewStatusBufferFromData(TargetPreviewData data)
    {
        _statusBuffer.Clear();

        // Rebuild status icons based on preview data
        // Freeze
        if (data.previewFrozenAfter)
            AddStatusVisual(CombatUiStatusIconKind.Freeze, "FR", string.Empty, new Color(0.4f, 0.78f, 1f, 0.96f));

        // Chilled: keep from current actor state if not frozen (preview doesn't change chilled)
        if (actor != null && actor.status != null && !data.previewFrozenAfter && actor.status.chilledTurns > 0)
            AddStatusVisual(CombatUiStatusIconKind.Chilled, "CH", actor.status.chilledTurns.ToString(), new Color(0.58f, 0.9f, 1f, 0.96f));

        // Mark
        if (data.previewMarkedAfter)
            AddStatusVisual(CombatUiStatusIconKind.Mark, "MK", string.Empty, new Color(1f, 0.88f, 0.28f, 0.96f));

        // Burn
        if (data.previewBurnAfter > 0 || data.currentBurn > 0)
            AddStatusVisual(CombatUiStatusIconKind.Burn, "BU", data.previewBurnAfter.ToString(), new Color(1f, 0.42f, 0.22f, 0.96f));

        // Bleed
        if (data.previewBleedAfter > 0 || data.currentBleed > 0)
            AddStatusVisual(CombatUiStatusIconKind.Bleed, "BL", data.previewBleedAfter.ToString(), new Color(0.82f, 0.14f, 0.2f, 0.96f));

        // Ailment: keep from current actor state (preview doesn't typically change ailments from Attack skills)
        if (actor != null && actor.status != null && actor.status.HasAilment(out AilmentType ailment, out int turnsLeft))
            AddStatusVisual(CombatUiStatusIconKind.Ailment, GetAilmentShortLabel(ailment), Mathf.Max(1, turnsLeft).ToString(), new Color(0.72f, 0.5f, 1f, 0.96f));
    }

    private void AddStatusVisual(CombatUiStatusIconKind kind, string shortLabel, string valueText, Color fallbackBackground)
    {
        if (TryGetStatusVisual(kind, out StatusVisualData data, shortLabel, valueText, fallbackBackground))
        {
            _statusBuffer.Add(data);
            return;
        }

        _statusBuffer.Add(new StatusVisualData(null, shortLabel, valueText, fallbackBackground));
    }

    private bool TryGetStatusVisual(CombatUiStatusIconKind kind, out StatusVisualData data)
    {
        return TryGetStatusVisual(kind, out data, string.Empty, string.Empty, Color.white);
    }

    private bool TryGetStatusVisual(CombatUiStatusIconKind kind, out StatusVisualData data, string shortLabel, string valueText, Color fallbackBackground)
    {
        if (iconLibrary != null && iconLibrary.TryGetStatusIcon(kind, out Sprite sprite, out Color backgroundColor, out _))
        {
            data = new StatusVisualData(sprite, shortLabel, valueText, backgroundColor);
            return true;
        }

        data = new StatusVisualData(null, shortLabel, valueText, fallbackBackground);
        return false;
    }

}

