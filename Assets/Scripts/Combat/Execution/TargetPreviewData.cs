/// <summary>
/// Immutable snapshot of what will happen to a target if the current skill is cast.
/// UI reads this to render preview overlays; it never calculates combat math itself.
/// </summary>
public struct TargetPreviewData
{
    /// <summary>True nếu preview hợp lệ (có đủ data để render).</summary>
    public bool valid;

    // --- HP / Guard ---
    public int currentHp;
    public int currentMaxHp;
    public int currentGuard;

    /// <summary>HP sau khi action xong.</summary>
    public int previewHpAfter;
    /// <summary>Guard sau khi action xong.</summary>
    public int previewGuardAfter;

    /// <summary>Lượng HP thật sự sẽ mất (phần cam trên thanh HP).</summary>
    public int hpLost;
    /// <summary>Lượng Guard thật sự sẽ mất.</summary>
    public int guardLost;

    // --- Stagger ---
    public bool currentlyStaggered;
    /// <summary>True nếu Guard sẽ bị phá bởi action này → tạo Stagger mới.</summary>
    public bool willBreakGuard;
    /// <summary>True nếu target đã Stagger sẵn VÀ action này consume Stagger (×1.2).</summary>
    public bool willConsumeStagger;

    // --- Status preview ---
    public int currentBurn;
    public int currentBleed;
    public bool currentMarked;
    public bool currentFrozen;

    public int previewBurnAfter;
    public int previewBleedAfter;
    public bool previewMarkedAfter;
    public bool previewFrozenAfter;

    /// <summary>True nếu skill target self (Guard/Heal) — preview sẽ hiện khác.</summary>
    public bool isSelfTarget;
    /// <summary>Guard sẽ nhận thêm (cho Self target).</summary>
    public int selfGuardGain;
}
