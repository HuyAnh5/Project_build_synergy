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
        bool isNumericFace = !isBrokenFace && DiceFaceEnchantUtility.IsNumericFace(faceEnchant);
        bool isUsable = !isBrokenFace;
        bool isCrit = isUsable && isNumericFace && (d.IsCritValue(rolled) || DiceFaceEnchantUtility.CountsAsCritForConditions(faceEnchant));
        bool isFail = isUsable && isNumericFace && (d.IsFailValue(rolled) || DiceFaceEnchantUtility.CountsAsFailForConditions(faceEnchant));
        bool grantsCritBonus = isCrit && !DiceFaceEnchantUtility.SuppressesCritBonus(faceEnchant);
        bool appliesFailPenalty = isFail && !DiceFaceEnchantUtility.SuppressesFailPenalty(faceEnchant);

        int outputBaseValue = effectiveEnchant == DiceFaceEnchantKind.Stone
            ? 0
            : effectiveEnchant == DiceFaceEnchantKind.Double
                ? Mathf.Max(0, rolled * 2)
                : rolled;

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

        RollInfo info = LastRollInfos[slot0];
        die.SetCombatRollFeedback(info.isCrit, info.isFail);
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
        EnsureSlots();
        _consumePreviewCount = Mathf.Max(0, diceCount);
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

        _consumePreviewInvalid = _consumePreviewCount > available;
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

        int consumed = 0;
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
                ui.ClearPreviewTint();
                die.ClearAllFacePreviews();
                ui.ClearPreviewSpentLike(true);
                continue;
            }

            if (!die.IsCurrentFaceUsable())
            {
                ui.SetPreviewTint(new Color(0.55f, 0.1f, 0.1f, 0.85f), true);
                ui.SetPreviewSpentLike(true, false);
                continue;
            }

            if (consumed < _consumePreviewCount)
            {
                if (die.GetCurrentFaceEnchant() == DiceFaceEnchantKind.Double && die.LastFaceIndex >= 0)
                    die.SetFacePreviewValue(die.LastFaceIndex, die.GetDisplayedRolledValue() * 2, true);
                // Dice sẽ bị consume: vàng nhấp nháy
                float alpha = Mathf.Lerp(minAlpha, 1f, t);
                ui.SetPreviewTint(new Color(0.62f, 0.62f, 0.62f, alpha), false);
                ui.SetPreviewSpentLike(true, false);
                consumed++;
            }
            else
            {
                die.ClearAllFacePreviews();
                // Dice không bị consume: bình thường
                ui.ClearPreviewTint();
                ui.ClearPreviewSpentLike(true);
            }
        }
    }
}
