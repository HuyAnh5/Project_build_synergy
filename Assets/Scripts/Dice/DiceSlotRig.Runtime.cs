using UnityEngine;

public partial class DiceSlotRig
{
    private void CacheRollInfos()
    {
        EnsureSlots();
        for (int i = 0; i < 3; i++)
        {
            CacheRollInfoForSlot(i);
        }
    }

    private void CacheRollInfoForSlot(int slot0)
    {
        EnsureSlots();
        if (slot0 < 0 || slot0 >= LastRollInfos.Length)
            return;

        if (!IsSlotActive(slot0))
        {
            LastRollInfos[slot0] = default;
            return;
        }

        DiceSpinnerGeneric d = GetDice(slot0);
        if (d == null)
        {
            LastRollInfos[slot0] = default;
            return;
        }

        d.GetRollExtents(out int minFace, out int maxFace);
        int rolled = d.GetDisplayedRolledValue();
        DiceFaceEnchantKind faceEnchant = d.GetCurrentFaceEnchant();
        DiceFaceEnchantKind effectiveEnchant = GetEffectiveCurrentFaceEnchant(slot0);
        bool isBrokenFace = d.IsCurrentFaceBroken();
        bool isNumericFace = !isBrokenFace && DiceFaceEnchantUtility.IsNumericFace(effectiveEnchant);
        bool isUsable = !isBrokenFace;
        int outputBaseValue = effectiveEnchant == DiceFaceEnchantKind.Stone
            ? 0
            : effectiveEnchant == DiceFaceEnchantKind.Double
                ? Mathf.Max(0, rolled * 2)
                : rolled;
        bool isCrit = isUsable && isNumericFace && (d.IsResolvedCritValue(outputBaseValue) || DiceFaceEnchantUtility.CountsAsCritForConditions(effectiveEnchant));
        bool isFail = isUsable && isNumericFace && (d.IsResolvedFailValue(outputBaseValue) || DiceFaceEnchantUtility.CountsAsFailForConditions(effectiveEnchant));
        bool grantsCritBonus = isCrit && !DiceFaceEnchantUtility.SuppressesCritBonus(effectiveEnchant);
        bool appliesFailPenalty = isFail && !DiceFaceEnchantUtility.SuppressesFailPenalty(effectiveEnchant);

        int genericAdded = 0;
        if (grantsCritBonus) genericAdded = FloorScaled(outputBaseValue, GenericCritPercent);
        genericAdded += d.GetCurrentPhaseValueModifier();
        genericAdded += DiceFaceEnchantUtility.GetOnUseAddedValue(effectiveEnchant);

        int genericResolved = isUsable ? outputBaseValue + genericAdded : 0;
        if (genericResolved < 1) genericResolved = 1;
        if (!isUsable) genericResolved = 0;

        LastRollInfos[slot0] = new RollInfo
        {
            rolledValue = rolled,
            minFaceAtRoll = minFace,
            maxFaceAtRoll = maxFace,
            faceEnchant = faceEnchant,
            isCrit = isCrit,
            isFail = isFail,
            grantsCritBonus = grantsCritBonus,
            appliesFailPenalty = appliesFailPenalty,
            isNumericFace = isNumericFace,
            isBrokenFace = isBrokenFace,
            isUsable = isUsable,
            genericAddedValue = genericAdded,
            genericResolvedValue = genericResolved
        };
    }

    private void ClearRollInfos()
    {
        EnsureSlots();
        for (int i = 0; i < LastRollInfos.Length; i++)
            LastRollInfos[i] = default;
    }

    private void BindDieRollCallbacks()
    {
        EnsureSlots();
        for (int i = 0; i < slots.Length; i++)
        {
            DiceSpinnerGeneric die = GetDice(i);
            if (die == null)
                continue;

            die.onRollComplete -= HandleDieRollComplete;
            die.onRollComplete += HandleDieRollComplete;
        }
    }

    private void HandleDieRollComplete(DiceSpinnerGeneric die)
    {
        if (die == null)
            return;

        int slot0 = FindSlotIndex(die);
        if (slot0 < 0)
            return;

        CacheRollInfoForSlot(slot0);

        ApplyBaseRollFeedback(slot0, die);
    }

    private int FindSlotIndex(DiceSpinnerGeneric die)
    {
        if (die == null || slots == null)
            return -1;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null && slots[i].dice == die)
                return i;
        }

        return -1;
    }

    private void ClearCombatRollFeedback()
    {
        EnsureSlots();
        for (int i = 0; i < slots.Length; i++)
        {
            DiceSpinnerGeneric die = GetDice(i);
            if (die != null)
                die.SetCombatRollFeedback(false, false);
        }
    }

    public bool HasAnySkillAffectingRollFeedbackThisTurn()
    {
        EnsureSlots();
        if (!HasRolledThisTurn)
            return false;

        for (int i = 0; i < LastRollInfos.Length; i++)
        {
            if (!IsSlotActive(i))
                continue;

            RollInfo info = LastRollInfos[i];
            if (info.isCrit || info.isFail || info.faceEnchant != DiceFaceEnchantKind.None || info.isBrokenFace)
                return true;
        }

        return false;
    }

    public DiceSpinnerGeneric GetDice(int slot0)
    {
        EnsureSlots();
        if (slot0 < 0 || slot0 >= slots.Length) return null;
        return slots[slot0] != null ? slots[slot0].dice : null;
    }

    private static int FindPreviousSlotIndex(DiceSpinnerGeneric[] previousDice, DiceSpinnerGeneric target)
    {
        if (previousDice == null || target == null)
            return -1;

        for (int i = 0; i < previousDice.Length; i++)
        {
            if (previousDice[i] == target)
                return i;
        }

        return -1;
    }

    private static bool IsRootAlreadyUsed(GameObject[] previousRoots, bool[] used, GameObject candidate)
    {
        if (previousRoots == null || used == null || candidate == null)
            return false;

        for (int i = 0; i < previousRoots.Length && i < used.Length; i++)
        {
            if (previousRoots[i] == candidate)
                return used[i];
        }

        return false;
    }

    private static GameObject TakeFirstUnusedRoot(GameObject[] previousRoots, bool[] used)
    {
        if (previousRoots == null || used == null)
            return null;

        for (int i = 0; i < previousRoots.Length && i < used.Length; i++)
        {
            if (previousRoots[i] == null || used[i])
                continue;

            used[i] = true;
            return previousRoots[i];
        }

        return null;
    }

    private void EnsureSlots()
    {
        if (slots == null || slots.Length != 3)
            slots = new Entry[3];

        for (int i = 0; i < 3; i++)
            if (slots[i] == null)
                slots[i] = new Entry();

        if (LastRollInfos == null || LastRollInfos.Length != 3)
            LastRollInfos = new RollInfo[3];
    }

    // ---------------------------
    // Dice Consume Preview
    // ---------------------------

    private bool _consumePreviewActive;
    private int _consumePreviewCount;
    private bool _consumePreviewInvalid; // true = thiếu dice
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
    {
        ShowConsumePreview(null, diceCount, spentDice);
    }

    public void ShowConsumePreview(
        DiceCombatEnchantRuntimeUtility.CommittedFaceUsePlan previewPlan,
        int diceCount,
        System.Collections.Generic.HashSet<DiceSpinnerGeneric> spentDice = null)
    {
        EnsureSlots();
        _consumePreviewCount = Mathf.Max(0, diceCount);
        _consumePreviewPlan = previewPlan != null ? previewPlan.Clone() : null;
        _cachedDiceUIs = UnityEngine.Object.FindObjectsOfType<DiceDraggableUI>(true);

        // Đếm dice available (active + chưa spent)
        int available = 0;
        for (int i = 0; i < slots.Length; i++)
        {
            if (!IsSlotActive(i)) continue;
            DiceSpinnerGeneric die = GetDice(i);
            if (die == null) continue;
            if (spentDice != null && spentDice.Contains(die)) continue;
            if (!die.IsCurrentFaceUsable()) continue;
            available++;
        }

        int previewContribution = _consumePreviewPlan != null
            ? _consumePreviewPlan.totalContribution
            : available;
        _consumePreviewInvalid = _consumePreviewCount > previewContribution || _consumePreviewCount > available;
        _consumePreviewActive = true;
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
                die.ClearAllFacePreviews();
                RestoreDieRollFeedback(i, die);
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
        UpdateConsumePreviewVisuals(spentDice, _consumePreviewPlan);
    }

    public void UpdateConsumePreviewVisuals(
        System.Collections.Generic.HashSet<DiceSpinnerGeneric> spentDice,
        DiceCombatEnchantRuntimeUtility.CommittedFaceUsePlan previewPlan)
    {
        if (!_consumePreviewActive)
            return;

        _consumePreviewPlan = previewPlan != null ? previewPlan.Clone() : _consumePreviewPlan;

        if (_cachedHud == null)
            _cachedHud = FindObjectOfType<CombatHUD>(true);

        float blinkSpeed = (_cachedHud != null) ? _cachedHud.consumePreviewBlinkSpeed : 3f;
        float minAlpha = (_cachedHud != null) ? _cachedHud.consumePreviewMinAlpha : 0.5f;
        float invalidMinAlpha = (_cachedHud != null) ? _cachedHud.consumePreviewInvalidMinAlpha : 0.6f;

        EnsureSlots();
        float t = Mathf.PingPong(Time.time * blinkSpeed, 1f);

        for (int i = 0; i < slots.Length; i++)
        {
            if (!IsSlotActive(i)) continue;
            DiceSpinnerGeneric die = GetDice(i);
            if (die == null) continue;

            DiceDraggableUI ui = FindDiceUI(die);
            if (ui == null) continue;

            ui.ClearPreviewTint();
            die.ClearAllFacePreviews();
            RestoreDieRollFeedback(i, die);
            ui.ClearPreviewSpentLike(true);

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
                ui.ClearPreviewTint();
                die.ClearAllFacePreviews();
                RestoreDieRollFeedback(i, die);
                ui.ClearPreviewSpentLike(true);
                continue;
            }

            if (!die.IsCurrentFaceUsable())
            {
                ui.SetPreviewTint(new Color(0.55f, 0.1f, 0.1f, 0.85f), true);
                ui.SetPreviewSpentLike(true, false);
                RestoreDieRollFeedback(i, die);
                continue;
            }

            bool isSelectedByPlan = _consumePreviewPlan != null && _consumePreviewPlan.IsSelected(i);
            bool isSelectedFallback = _consumePreviewPlan == null && CountFallbackSelectedBefore(i, spentDice) < _consumePreviewCount;
            if (isSelectedByPlan || isSelectedFallback)
            {
                ApplyCommittedPreviewVisuals(i, die);
                // Dice sẽ bị consume: vàng nhấp nháy
                float alpha = Mathf.Lerp(minAlpha, 1f, t);
                ui.SetPreviewTint(new Color(0.62f, 0.62f, 0.62f, alpha), false);
                ui.SetPreviewSpentLike(true, false);
            }
            else
            {
                die.ClearAllFacePreviews();
                RestoreDieRollFeedback(i, die);
                // Dice không bị consume: bình thường
                ui.ClearPreviewTint();
                ui.ClearPreviewSpentLike(true);
            }
        }
    }

    private int CountFallbackSelectedBefore(int slotExclusive, System.Collections.Generic.HashSet<DiceSpinnerGeneric> spentDice)
    {
        int selected = 0;
        for (int i = 0; i < slotExclusive && i < slots.Length; i++)
        {
            if (!IsSlotActive(i))
                continue;

            DiceSpinnerGeneric die = GetDice(i);
            if (die == null || !die.IsCurrentFaceUsable())
                continue;
            if (spentDice != null && spentDice.Contains(die))
                continue;

            selected++;
        }

        return selected;
    }

    private void ApplyCommittedPreviewVisuals(int slot0, DiceSpinnerGeneric die)
    {
        if (die == null)
            return;

        DiceFaceEnchantKind effectiveEnchant = GetEffectiveCurrentFaceEnchant(slot0);
        string previewText = BuildCommittedPreviewText(slot0, die, effectiveEnchant);
        if (die.LastFaceIndex >= 0 && !string.IsNullOrEmpty(previewText))
            die.SetFacePreviewText(die.LastFaceIndex, previewText, effectiveEnchant == DiceFaceEnchantKind.Double);
        else
            die.ClearAllFacePreviews();

        RollInfo info = GetRollInfo(slot0);
        int previewValue = GetOutputBaseValue(slot0);
        bool isNumeric = info.isUsable && DiceFaceEnchantUtility.IsNumericFace(effectiveEnchant);
        bool previewCrit = isNumeric && IsPreviewCritValue(previewValue);
        bool previewFail = isNumeric && IsPreviewFailValue(previewValue);
        die.SetCombatRollFeedback(previewCrit, previewFail);
    }

    private void RestoreDieRollFeedback(int slot0, DiceSpinnerGeneric die)
    {
        if (die == null)
            return;

        ApplyBaseRollFeedback(slot0, die);
    }

    private void ApplyBaseRollFeedback(int slot0, DiceSpinnerGeneric die)
    {
        if (die == null)
            return;

        RollInfo info = GetRollInfo(slot0);
        bool isNumeric = info.isUsable && info.isNumericFace;
        die.SetCombatRollFeedback(isNumeric && info.isCrit, isNumeric && info.isFail);
    }

    private bool IsPreviewCritValue(int value)
    {
        if (value <= 0)
            return false;

        int max = int.MinValue;
        for (int i = 0; i < slots.Length; i++)
        {
            if (!IsSlotActive(i))
                continue;

            DiceSpinnerGeneric die = GetDice(i);
            if (die == null || !die.IsCurrentFaceUsable())
                continue;

            DiceFaceEnchantKind effective = GetEffectiveCurrentFaceEnchant(i);
            if (!DiceFaceEnchantUtility.IsNumericFace(effective))
                continue;

            max = Mathf.Max(max, GetOutputBaseValue(i));
        }

        return max != int.MinValue && value >= max;
    }

    private bool IsPreviewFailValue(int value)
    {
        if (value <= 0)
            return false;

        int min = int.MaxValue;
        int max = int.MinValue;
        for (int i = 0; i < slots.Length; i++)
        {
            if (!IsSlotActive(i))
                continue;

            DiceSpinnerGeneric die = GetDice(i);
            if (die == null || !die.IsCurrentFaceUsable())
                continue;

            DiceFaceEnchantKind effective = GetEffectiveCurrentFaceEnchant(i);
            if (!DiceFaceEnchantUtility.IsNumericFace(effective))
                continue;

            int output = GetOutputBaseValue(i);
            min = Mathf.Min(min, output);
            max = Mathf.Max(max, output);
        }

        return min != int.MaxValue && min != max && value <= min;
    }

    private string BuildCommittedPreviewText(int slot0, DiceSpinnerGeneric die, DiceFaceEnchantKind effectiveEnchant)
    {
        if (die == null || die.LastFaceIndex < 0)
            return null;

        int rolledValue = die.GetDisplayedRolledValue();
        int outputBaseValue = GetOutputBaseValue(slot0);
        int relayAdded = ComputePreviewRelayAddedValue(slot0);
        int selfAdded = DiceFaceEnchantUtility.GetOnUseAddedValue(effectiveEnchant);

        switch (effectiveEnchant)
        {
            case DiceFaceEnchantKind.Double:
                return outputBaseValue.ToString();
        }

        int totalAdded = Mathf.Max(0, selfAdded + relayAdded);
        return null;
    }

    private int ComputePreviewRelayAddedValue(int targetSlot0)
    {
        if (_consumePreviewPlan == null || !_consumePreviewPlan.IsSelected(targetSlot0))
            return 0;

        int sourceSlot0 = targetSlot0 - 1;
        if (sourceSlot0 < 0 || !_consumePreviewPlan.IsSelected(sourceSlot0))
            return 0;

        if (GetEffectiveCurrentFaceEnchant(sourceSlot0) != DiceFaceEnchantKind.Relay)
            return 0;

        DiceSpinnerGeneric target = GetDice(targetSlot0);
        return target != null && target.IsCurrentFaceUsable()
            ? DiceFaceEnchantUtility.RelayValueModifier
            : 0;
    }
}
