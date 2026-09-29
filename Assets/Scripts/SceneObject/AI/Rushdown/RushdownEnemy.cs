using UnityEngine;

/// <summary>
/// Rushes the player once engaged. When the player can be reached, closes in and swings on a
/// cooldown. When the player cannot be reached, stops, switches to a ranged weapon, and throws with
/// the grounded attack whose angle lines up with an arc to the player. Attacks only while grounded.
/// </summary>
internal class RushdownEnemy : Enemy
{
    private static readonly AttackState[] ThrowSlots = { AttackState.UpTilt, AttackState.DownTilt, AttackState.ForwardTilt };

    [Header("Melee")]
    [Tooltip("Distance at which the enemy stops and attacks, in world units.")]
    [SerializeField] private float attackRange = 1.75f;
    [Tooltip("Height difference above which an up or down attack is used instead of a forward one, in world units.")]
    [SerializeField] private float verticalSlotThreshold = 1f;
    [SerializeField] private Cooldown attackCooldown = new Cooldown(1.2f, 0.5f);

    [Header("Ranged")]
    [SerializeField] private Cooldown throwCooldown = new Cooldown(1.5f, 0.75f);
    [Tooltip("Time between searches for a spot to throw from, in seconds.")]
    [SerializeField] private float throwSpotSearchInterval = 0.5f;
    [Tooltip("Distance between sampled spots along the ground, in world units.")]
    [SerializeField] private float throwSpotSampleSpacing = 0.5f;
    [Tooltip("Distance kept from the ends of the ground when choosing a spot, in world units.")]
    [SerializeField] private float throwSpotEdgeInset = 0.5f;
    [Tooltip("Radius of the thrown object used to check its flight for obstructions, in world units.")]
    [SerializeField] private float throwClearanceRadius = 0.25f;

    private Vector3 throwSpot;
    private bool hasThrowSpot;
    private float nextThrowSpotSearchTime;


    protected override void Start()
    {
        base.Start();

        attackCooldown.Stagger();
        throwCooldown.Stagger();
    }


    #region Behavior

    protected override void OnEngaged(Vector3 playerCenter)
    {
        if (PlayerReachable)
            EngageMelee(playerCenter);
        else
            EngageRanged(playerCenter);
    }

    /// <summary>Closes on the player and swings, switching away from a ranged weapon first.</summary>
    private void EngageMelee(Vector3 playerCenter)
    {
        if (HasRangedWeaponEquipped)
        {
            RequestWeaponSwap();
            MoveTo(LastKnownPlayerPosition);
            return;
        }

        if (TryMeleeAttack(playerCenter))
        {
            StopMoving();
            return;
        }

        MoveTo(LastKnownPlayerPosition);
    }

    /// <summary>
    /// Switches to a ranged weapon first. Throws when a slot lines up from here; otherwise walks to the
    /// nearest spot on its ground where one does, or holds facing the player when there is none.
    /// </summary>
    private void EngageRanged(Vector3 playerCenter)
    {
        if (!HasRangedWeaponEquipped)
        {
            StopMoving();
            RequestWeaponSwap();
            return;
        }

        if (IsAttacking)
        {
            StopMoving();
            return;
        }

        if (TryChooseThrowSlot(Bounds.center, playerCenter, out AttackState slot))
        {
            StopMoving();
            hasThrowSpot = false;
            TryThrow(slot, playerCenter);
            return;
        }

        Reposition(playerCenter);
    }

    #endregion


    #region Melee

    /// <summary>Attacks when in range and ready. True while the enemy should hold position.</summary>
    private bool TryMeleeAttack(Vector3 playerCenter)
    {
        if (IsAttacking)
            return true;

        if (!IsGrounded || !IsWithinOfSelf(playerCenter, attackRange))
            return false;

        if (!attackCooldown.IsReady)
            return true;

        PressAttack(AttackDirectionTo(playerCenter, verticalSlotThreshold));
        attackCooldown.Trigger();

        return true;
    }

    #endregion


    #region Ranged

    /// <summary>
    /// Throws with the given slot when ready. Turns to face the player first for up and down throws,
    /// which do not turn the enemy.
    /// </summary>
    private void TryThrow(AttackState slot, Vector3 playerCenter)
    {
        if (!IsGrounded || !throwCooldown.IsReady)
            return;

        if (slot != AttackState.ForwardTilt && !IsFacing(playerCenter))
        {
            FaceToward(playerCenter);
            return;
        }

        PressAttack(ThrowDirection(slot, playerCenter));
        throwCooldown.Trigger();
    }

    /// <summary>
    /// Walks toward the nearest spot on its ground where a slot lines up, searching again on an
    /// interval. Holds facing the player when no spot exists.
    /// </summary>
    private void Reposition(Vector3 playerCenter)
    {
        if (Time.time >= nextThrowSpotSearchTime)
        {
            nextThrowSpotSearchTime = Time.time + throwSpotSearchInterval;
            hasThrowSpot = TryFindThrowSpot(playerCenter, out throwSpot);
        }

        if (hasThrowSpot)
        {
            MoveTo(throwSpot);
            return;
        }

        StopMoving();

        if (!IsFacing(playerCenter))
            FaceToward(playerCenter);
    }

    /// <summary>
    /// The spot on the ground under the enemy, nearest to it, from which a slot lines up. Searched
    /// outward from the enemy's position, alternating sides.
    /// </summary>
    private bool TryFindThrowSpot(Vector3 playerCenter, out Vector3 spot)
    {
        spot = Vector3.zero;

        if (!TryGetGroundSpan(out float minX, out float maxX, out float surfaceY))
            return false;

        float centerHeight = Bounds.center.y - Bounds.min.y;
        float spacing = Mathf.Max(0.1f, throwSpotSampleSpacing);
        float startX = minX + throwSpotEdgeInset;
        float endX = maxX - throwSpotEdgeInset;
        float currentX = Mathf.Clamp(Bounds.center.x, startX, endX);

        int steps = Mathf.CeilToInt((endX - startX) / spacing);

        for (int step = 0; step <= steps; step++)
        {
            float[] candidates = step == 0
                ? new[] { currentX }
                : new[] { currentX + step * spacing, currentX - step * spacing };

            foreach (float x in candidates)
            {
                if (x < startX || x > endX)
                    continue;

                Vector3 origin = new Vector3(x, surfaceY + centerHeight, 0f);

                if (!TryChooseThrowSlot(origin, playerCenter, out _))
                    continue;

                spot = new Vector3(x, surfaceY + 0.1f, 0f);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The grounded slot whose trajectory angle is closest to a clear arc from the origin that reaches
    /// the player, when that difference is within the trajectory's assist correction and the player is
    /// within its assist range. Evaluated as if facing the player.
    /// </summary>
    private bool TryChooseThrowSlot(Vector3 origin, Vector3 playerCenter, out AttackState slot)
    {
        slot = AttackState.Null;

        TrajectoryData trajectory = RangedTrajectory;
        if (trajectory == null)
            return false;

        Vector3 offset = playerCenter - origin;

        if (offset.magnitude > trajectory.AssistRange)
            return false;

        if (!trajectory.TrySolveAngles(offset, trajectory.Speed, out float lowAngle, out float highAngle))
            return false;

        float side = offset.x >= 0f ? 1f : -1f;
        float bestCorrection = trajectory.AssistMaxCorrection;

        foreach (AttackState candidate in ThrowSlots)
        {
            float angle = MirrorToSide(trajectory.GetAngle((int)candidate), side);

            // The arc the projectile flies: the solution closest to the slot's angle.
            float arc = Mathf.Abs(Mathf.DeltaAngle(angle, lowAngle)) <= Mathf.Abs(Mathf.DeltaAngle(angle, highAngle)) ? lowAngle : highAngle;
            float correction = Mathf.Abs(Mathf.DeltaAngle(angle, arc));

            if (correction > bestCorrection)
                continue;

            if (!trajectory.IsFlightClear(origin, playerCenter, arc, trajectory.Speed, SightBlockingMask, throwClearanceRadius))
                continue;

            bestCorrection = correction;
            slot = candidate;
        }

        return slot != AttackState.Null;
    }

    private Vector2 ThrowDirection(AttackState slot, Vector3 playerCenter)
    {
        switch (slot)
        {
            case AttackState.UpTilt: return Vector2.up;
            case AttackState.DownTilt: return Vector2.down;
            default: return playerCenter.x >= Bounds.center.x ? Vector2.right : Vector2.left;
        }
    }

    /// <summary>An angle authored facing right, mirrored when the side is negative.</summary>
    private static float MirrorToSide(float angle, float side)
    {
        return side >= 0f ? angle : 180f - angle;
    }

    #endregion


    #region Gizmos

    protected override void DrawArchetypeGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(Bounds.center, attackRange);
    }

    #endregion
}
