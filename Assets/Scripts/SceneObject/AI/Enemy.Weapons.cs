using UnityEngine;

/// <summary>
/// Weapon awareness for the enemy: whether the equipped weapon is ranged, whether the player can be
/// reached, swapping weapons, and turning to face a target.
/// </summary>
internal abstract partial class Enemy
{
    [Header("Weapons")]
    [Tooltip("Time between checks of whether the player can be reached, in seconds.")]
    [SerializeField] private float reachCheckInterval = 0.5f;
    [Tooltip("Time to wait for a weapon swap to complete before trying again, in seconds.")]
    [SerializeField] private float swapTimeout = 1f;

    private IStats statHandler;
    private EnhancementStatData.EnhancementStats rangedEnhancement;

    private bool playerReachable = true;
    private float nextReachCheckTime;
    private float swapPendingUntil;

    // Horizontal input applied for one tick in place of the nav agent's. Zero when unused.
    private float faceDirection;

    protected bool HasRangedWeaponEquipped => rangedEnhancement != null;
    protected TrajectoryData RangedTrajectory => rangedEnhancement?.Trajectory;
    protected bool PlayerReachable => playerReachable;


    #region Initialize

    private void InitializeWeapons()
    {
        statHandler = GetComponent<IStats>();

        if (statHandler != null)
            statHandler.EnhancementStatsChangedEvent += OnEnhancementStatsChanged;
    }

    protected override void OnDestroy()
    {
        if (statHandler != null)
            statHandler.EnhancementStatsChangedEvent -= OnEnhancementStatsChanged;

        base.OnDestroy();
    }

    /// <summary>Records the equipped weapon's ranged attack enhancement, and ends any pending swap.</summary>
    private void OnEnhancementStatsChanged(EnhancementStatData enhancementStats)
    {
        rangedEnhancement = null;

        foreach (EnhancementStatData.EnhancementStats enhancement in enhancementStats.Enhancements)
        {
            if (enhancement.Trigger != EnhancementTrigger.Attack || !enhancement.IsRanged)
                continue;

            rangedEnhancement = enhancement;
            break;
        }

        swapPendingUntil = 0f;
    }

    #endregion


    #region Weapons

    /// <summary>Rechecks whether the player can be reached, at most once per reach check interval.</summary>
    private void UpdateReachability(Vector3 playerCenter)
    {
        if (Time.time < nextReachCheckTime)
            return;

        nextReachCheckTime = Time.time + reachCheckInterval;
        playerReachable = CanNavigate && navAgent.CanReach(playerCenter);
    }

    /// <summary>Buffers a weapon swap unless attacking or a swap is still pending.</summary>
    protected void RequestWeaponSwap()
    {
        if (IsAttacking || Time.time < swapPendingUntil)
            return;

        PressSwapWeapon();
        swapPendingUntil = Time.time + swapTimeout;
    }

    #endregion


    #region Facing

    /// <summary>Holds horizontal input toward the target for this tick, turning the enemy to face it.</summary>
    protected void FaceToward(Vector3 target)
    {
        faceDirection = target.x >= Bounds.center.x ? 1f : -1f;
    }

    protected bool IsFacing(Vector3 target)
    {
        return (target.x >= Bounds.center.x) == IsFacingRightDirection;
    }

    #endregion
}
