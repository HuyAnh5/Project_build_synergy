using UnityEngine;

public partial class DiceSlotRig
{
    // ---------------------------
    // Dice Consume Preview
    // ---------------------------

    private bool _consumePreviewActive;
    private int _consumePreviewCount;
    private bool _consumePreviewInvalid; // true = thiếu dice
    private int _consumePreviewMask = -1;
    private DiceCombatEnchantRuntimeUtility.CommittedFaceUsePlan _consumePreviewPlan;
    private CombatHUD _cachedHud;

    // Cache all DiceDraggableUI instances once per show/clear cycle
    private DiceDraggableUI[] _cachedDiceUIs;

    private DiceDraggableUI FindDiceUI(DiceSpinnerGeneric die)
    {
        if (die == null) return null;
        if (_cachedDiceUIs == null)
            _cachedDiceUIs = UnityEngine.Object.FindObjectsOfType<DiceDraggableUI>(true);
        for (int i = 0; i < _cachedDiceUIs.Length; i++)
        {
            if (_cachedDiceUIs[i] != null && _cachedDiceUIs[i].dice == die)
                return _cachedDiceUIs[i];
        }
        return null;
    }

    /// <summary>
    /// Hiển thị preview dice sẽ bị consume khi hover/drag skill.
    /// diceCount = số dice skill cần (slotsRequired).
    /// spentDice = set dice đã dùng trong turn này, để bỏ qua khi đếm available.
    /// </summary>
    public void ShowConsumePreview(int diceCount, System.Collections.Generic.HashSet<DiceSpinnerGeneric> spentDice = null)
        => ShowConsumePreview(diceCount, spentDice, -1);

    public void ShowConsumePreview(int diceCount, System.Collections.Generic.HashSet<DiceSpinnerGeneric> spentDice, int paymentMask)
    {
        EnsureSlots();
        _consumePreviewCount = Mathf.Max(0, diceCount);
        _cachedDiceUIs = UnityEngine.Object.FindObjectsOfType<DiceDraggableUI>(true);

        int available = CountAvailableConsumePreviewContribution(spentDice);
        _consumePreviewInvalid = paymentMask < 0 && _consumePreviewCount > available;
        _consumePreviewActive = true;
        _consumePreviewMask = paymentMask;
        _consumePreviewPlan = paymentMask >= 0
            ? DiceCombatEnchantRuntimeUtility.BuildPaymentPlanFromMask(this, paymentMask)
            : null;
    }

    private int CountAvailableConsumePreviewContribution(System.Collections.Generic.HashSet<DiceSpinnerGeneric> spentDice)
    {
        int available = 0;
        for (int i = 0; i < slots.Length; i++)
        {
            if (!CanPreviewPayWithSlot(i, spentDice))
                continue;

            available += GetPaymentContribution(i);
        }

        return available;
    }

    private int BuildFallbackConsumePreviewMask(System.Collections.Generic.HashSet<DiceSpinnerGeneric> spentDice)
    {
        int previewPaymentMask = 0;
        int previewContribution = 0;
        for (int i = 0; i < slots.Length && previewContribution < _consumePreviewCount; i++)
        {
            if (!CanPreviewPayWithSlot(i, spentDice))
                continue;

            previewPaymentMask |= 1 << i;
            previewContribution += GetPaymentContribution(i);
        }

        return previewPaymentMask;
    }

    private bool CanPreviewPayWithSlot(int slot0, System.Collections.Generic.HashSet<DiceSpinnerGeneric> spentDice)
    {
        if (!IsSlotActive(slot0))
            return false;

        DiceSpinnerGeneric die = GetDice(slot0);
        if (die == null)
            return false;

        if (spentDice != null && spentDice.Contains(die))
            return false;

        return die.IsCurrentFaceUsable();
    }

    private int GetPaymentContribution(int slot0)
    {
        return GetEffectiveCurrentFaceEnchant(slot0) == DiceFaceEnchantKind.Heavy
            ? DiceFaceEnchantUtility.HeavyPaymentContribution
            : 1;
    }

    /// <summary>
    /// Tắt preview dice consume, trả visual về bình thường.
    /// </summary>
    public void ClearConsumePreview(System.Collections.Generic.HashSet<DiceSpinnerGeneric> keepPreviewSpentLikeDice = null)
    {
        if (!_consumePreviewActive && _cachedDiceUIs == null)
            return;

        _consumePreviewActive = false;
        _consumePreviewCount = 0;
        _consumePreviewInvalid = false;
        _consumePreviewMask = -1;
        _consumePreviewPlan = null;

        // Restore tất cả DiceDraggableUI về trạng thái bình thường
        EnsureSlots();
        for (int i = 0; i < slots.Length; i++)
        {
            DiceSpinnerGeneric die = GetDice(i);
            if (die == null) continue;
            DiceDraggableUI ui = FindDiceUI(die);
            if (ui != null)
            {
                ui.ClearPreviewTint();
                ui.ClearPreviewRollFeedback();
                die.ClearAllFacePreviews();
                bool keepSpentLike = keepPreviewSpentLikeDice != null && keepPreviewSpentLikeDice.Contains(die);
                if (!keepSpentLike)
                    ui.ClearPreviewSpentLike(true);
            }
        }

        _cachedDiceUIs = null;
    }

    /// <summary>
    /// Gọi mỗi frame khi preview đang active.
    /// Cập nhật visual nhấp nháy cho các dice sẽ bị consume.
    /// </summary>
    public void UpdateConsumePreviewVisuals(System.Collections.Generic.HashSet<DiceSpinnerGeneric> spentDice = null)
    {
        if (!_consumePreviewActive)
            return;

        if (_cachedHud == null)
            _cachedHud = FindObjectOfType<CombatHUD>(true);

        float blinkSpeed = (_cachedHud != null) ? _cachedHud.consumePreviewBlinkSpeed : 3f;
        float minAlpha = (_cachedHud != null) ? _cachedHud.consumePreviewMinAlpha : 0.5f;
        float invalidMinAlpha = (_cachedHud != null) ? _cachedHud.consumePreviewInvalidMinAlpha : 0.6f;

        EnsureSlots();
        float t = Mathf.PingPong(Time.time * blinkSpeed, 1f);

        int previewPaymentMask = _consumePreviewMask;
        if (previewPaymentMask < 0 && !_consumePreviewInvalid)
        {
            previewPaymentMask = BuildFallbackConsumePreviewMask(spentDice);
        }

        for (int i = 0; i < slots.Length; i++)
        {
            if (!IsSlotActive(i)) continue;
            DiceSpinnerGeneric die = GetDice(i);
            if (die == null) continue;

            DiceDraggableUI ui = FindDiceUI(die);
            if (ui == null) continue;

            if (_consumePreviewInvalid)
            {
                // Thiếu dice: tất cả dice đỏ nhẹ nhấp nháy, ẩn outline để hiện khối màu đỏ đồng nhất
                float alpha = Mathf.Lerp(invalidMinAlpha, 1f, t);
                ui.SetPreviewTint(new Color(1f, 0.3f, 0.3f, alpha), true);
                ui.ClearPreviewSpentLike(true);
                continue;
            }

            // Dice đã spent rồi thì skip (nó đã dim 50%)
            if (spentDice != null && spentDice.Contains(die))
            {
                if (!IsRelayPreviewTarget(i, previewPaymentMask) && !IsEchoPreviewSource(i, previewPaymentMask))
                {
                    ui.ClearPreviewTint();
                    ui.ClearPreviewRollFeedback();
                    die.ClearAllFacePreviews();
                    ui.ClearPreviewSpentLike(true);
                    continue;
                }
            }

            if (!die.IsCurrentFaceUsable())
            {
                if (IsEchoPreviewSource(i, previewPaymentMask))
                {
                    ui.SetPreviewTint(new Color32(136, 219, 255, 255), false);
                    ui.ClearPreviewRollFeedback();
                    ui.ClearPreviewSpentLike(true);
                }
                else
                {
                    ui.SetPreviewTint(new Color(0.55f, 0.1f, 0.1f, 0.85f), true);
                    ui.ClearPreviewRollFeedback();
                    ui.SetPreviewSpentLike(true, false);
                }
                continue;
            }

            if ((previewPaymentMask & (1 << i)) != 0)
            {
                ApplyCommittedBreakPreview(i, die);
                if (die.GetCurrentFaceEnchant() == DiceFaceEnchantKind.Double && die.LastFaceIndex >= 0)
                {
                    die.SetFacePreviewValue(die.LastFaceIndex, die.GetDisplayedRolledValue() * 2, true);
                    bool previewCrit = die.GetDisplayedRolledValue() * 2 >= die.GetMaxFaceValue();
                    bool previewFail = die.GetMinFaceValue() != die.GetMaxFaceValue() && die.GetDisplayedRolledValue() * 2 <= die.GetMinFaceValue();
                    ui.SetPreviewRollFeedback(previewCrit, previewFail);
                }
                else
                {
                    ui.ClearPreviewRollFeedback();
                }
                // Dice sẽ bị consume: vàng nhấp nháy
                if (IsRelayPreviewTarget(i, previewPaymentMask))
                    ui.SetPreviewTint(new Color32(137, 255, 142, 255), false);
                else if (IsEchoPreviewSource(i, previewPaymentMask))
                    ui.SetPreviewTint(new Color32(136, 219, 255, 255), false);
                else
                {
                    float alpha = Mathf.Lerp(minAlpha, 1f, t);
                    ui.SetPreviewTint(new Color(0.62f, 0.62f, 0.62f, alpha), false);
                }
                ui.SetPreviewSpentLike(true, false);
            }
            else if (IsRelayPreviewTarget(i, previewPaymentMask))
            {
                die.ClearAllFacePreviews();
                ui.SetPreviewTint(new Color32(137, 255, 142, 255), false);
                ui.ClearPreviewRollFeedback();
                ui.ClearPreviewSpentLike(true);
            }
            else if (IsEchoPreviewSource(i, previewPaymentMask))
            {
                die.ClearAllFacePreviews();
                ui.SetPreviewTint(new Color32(136, 219, 255, 255), false);
                ui.ClearPreviewRollFeedback();
                ui.ClearPreviewSpentLike(true);
            }
            else
            {
                ui.ClearPreviewRollFeedback();
                die.ClearAllFacePreviews();
                // Dice không bị consume: bình thường
                ui.ClearPreviewTint();
                ui.ClearPreviewSpentLike(true);
            }
        }
    }

    private void ApplyCommittedBreakPreview(int slot0, DiceSpinnerGeneric die)
    {
        if (die == null)
            return;

        die.ClearAllFacePreviews();

        if (_consumePreviewPlan == null || !_consumePreviewPlan.IsSelected(slot0))
            return;

        int faceIndex = _consumePreviewPlan.committedFaceIndices != null && slot0 < _consumePreviewPlan.committedFaceIndices.Length
            ? _consumePreviewPlan.committedFaceIndices[slot0]
            : die.LastFaceIndex;
        if (faceIndex < 0)
            return;

        if (_consumePreviewPlan.ShouldBreak(slot0))
            die.SetFacePreviewBroken(faceIndex, true);
    }

    private bool IsRelayPreviewTarget(int slot0, int previewPaymentMask)
    {
        int leftSlot = slot0 - 1;
        if (leftSlot < 0 || previewPaymentMask < 0)
            return false;
        if ((previewPaymentMask & (1 << leftSlot)) == 0)
            return false;

        DiceSpinnerGeneric left = GetDice(leftSlot);
        return left != null && GetEffectiveCurrentFaceEnchant(leftSlot) == DiceFaceEnchantKind.Relay;
    }

    private bool IsEchoPreviewSource(int slot0, int previewPaymentMask)
    {
        int echoSlot = slot0 + 1;
        if (echoSlot >= 3 || previewPaymentMask < 0)
            return false;
        if ((previewPaymentMask & (1 << echoSlot)) == 0)
            return false;

        DiceSpinnerGeneric echo = GetDice(echoSlot);
        if (echo == null || echo.GetCurrentFaceEnchant() != DiceFaceEnchantKind.Echo)
            return false;

        DiceSpinnerGeneric source = GetDice(slot0);
        return source != null;
    }
}
