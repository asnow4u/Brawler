using UnityEngine;

/// <summary>
/// Reachable bounds for a character through the air, for jumping and for walking off a ledge.
/// </summary>
public struct AirborneEnvelope
{
    /// <summary>False until movement stats have arrived.</summary>
    public bool Valid;

    // Vertical
    /// <summary>Upward velocity at launch.</summary>
    public float LaunchVelocity;
    /// <summary>Rising speed above which RisingDecceleration applies.</summary>
    public float MaxRisingVelocity;
    /// <summary>Deceleration added to gravity while rising faster than MaxRisingVelocity.</summary>
    public float RisingDecceleration;
    public float GravityRaising;
    public float GravityFalling;
    public float MaxFallVelocity;

    // Horizontal
    /// <summary>Horizontal speed on leaving the ground.</summary>
    public float LaunchXVelocity;
    public float MaxAerialXVelocity;
    public float AerialXAcceleration;
    public float AerialXDecceleration;


    #region Rise

    /// <summary>Peak height of a jump above its launch surface.</summary>
    public float ApexHeight
    {
        get
        {
            RisePhases(out _, out float height);
            return height;
        }
    }

    /// <summary>Time from launch to apex.</summary>
    public float TimeToApex
    {
        get
        {
            RisePhases(out float time, out _);
            return time;
        }
    }

    /// <summary>
    /// Time and height from launch to apex. When launch exceeds MaxRisingVelocity, the rise bleeds
    /// down to it under gravity plus RisingDecceleration, then coasts under gravity alone.
    /// </summary>
    private void RisePhases(out float time, out float height)
    {
        time = 0f;
        height = 0f;

        if (!Valid || GravityRaising <= 0f || LaunchVelocity <= 0f)
            return;

        float launch = LaunchVelocity;
        float cap = MaxRisingVelocity;

        if (cap > 0f && launch > cap && RisingDecceleration > 0f)
        {
            float bleedRate = GravityRaising + RisingDecceleration;

            float bleedTime = (launch - cap) / bleedRate;
            float bleedHeight = (launch * launch - cap * cap) / (2f * bleedRate);

            float coastTime = cap / GravityRaising;
            float coastHeight = (cap * cap) / (2f * GravityRaising);

            time = bleedTime + coastTime;
            height = bleedHeight + coastHeight;
            return;
        }

        time = launch / GravityRaising;
        height = (launch * launch) / (2f * GravityRaising);
    }

    #endregion


    #region Jump Reach

    /// <summary>Time until a jump descends to a surface dy above launch. False when dy is above the apex.</summary>
    public bool TryGetJumpFlightTime(float dy, out float time)
    {
        time = 0f;

        if (!Valid || GravityRaising <= 0f || GravityFalling <= 0f)
            return false;

        float fallDistance = ApexHeight - dy;

        if (fallDistance < 0f)
            return false;

        time = TimeToApex + FallTime(fallDistance);
        return true;
    }

    /// <summary>Furthest horizontal distance of a running jump landing dy above launch.</summary>
    public float MaxJumpReach(float dy)
    {
        if (!TryGetJumpFlightTime(dy, out float time))
            return 0f;

        return HorizontalDistance(LaunchXVelocity, time);
    }

    /// <summary>Horizontal distance of a jump from a standing start landing dy above launch.</summary>
    public float StandingJumpReach(float dy)
    {
        if (!TryGetJumpFlightTime(dy, out float time))
            return 0f;

        return HorizontalDistance(0f, time);
    }

    /// <summary>Whether a jump can land dx across and dy up, staying verticalMargin below the apex.</summary>
    public bool CanJumpTo(float dx, float dy, float verticalMargin = 0.25f)
    {
        if (!Valid)
            return false;

        if (dy > ApexHeight - verticalMargin)
            return false;

        if (!TryGetJumpFlightTime(dy, out float time))
            return false;

        return Mathf.Abs(dx) <= HorizontalDistance(LaunchXVelocity, time);
    }

    #endregion


    #region Fall Reach

    /// <summary>Time to fall to a surface dy below a ledge after walking off it. False unless dy is downward.</summary>
    public bool TryGetFallTime(float dy, out float time)
    {
        time = 0f;

        if (!Valid || GravityFalling <= 0f || dy >= 0f)
            return false;

        time = FallTime(-dy);
        return true;
    }

    /// <summary>Whether walking off a ledge can land dx across and dy down.</summary>
    public bool CanFallTo(float dx, float dy)
    {
        if (!TryGetFallTime(dy, out float time))
            return false;

        return Mathf.Abs(dx) <= HorizontalDistance(LaunchXVelocity, time);
    }

    #endregion


    #region Integration

    /// <summary>Time to fall a distance, respecting terminal velocity.</summary>
    private float FallTime(float distance)
    {
        if (distance <= 0f)
            return 0f;

        if (MaxFallVelocity <= 0f)
            return Mathf.Sqrt(2f * distance / GravityFalling);

        float terminalDistance = (MaxFallVelocity * MaxFallVelocity) / (2f * GravityFalling);

        if (distance <= terminalDistance)
            return Mathf.Sqrt(2f * distance / GravityFalling);

        float timeToTerminal = MaxFallVelocity / GravityFalling;
        return timeToTerminal + (distance - terminalDistance) / MaxFallVelocity;
    }

    /// <summary>
    /// Horizontal distance covered in a given time starting at v0 and holding a direction,
    /// accelerating or decelerating toward MaxAerialXVelocity.
    /// </summary>
    private float HorizontalDistance(float v0, float time)
    {
        float cap = MaxAerialXVelocity;

        if (time <= 0f)
            return 0f;

        if (Mathf.Approximately(v0, cap))
            return cap * time;

        if (v0 < cap)
        {
            if (AerialXAcceleration <= 0f)
                return v0 * time;

            float timeToCap = (cap - v0) / AerialXAcceleration;

            if (time <= timeToCap)
                return v0 * time + 0.5f * AerialXAcceleration * time * time;

            float distanceToCap = v0 * timeToCap + 0.5f * AerialXAcceleration * timeToCap * timeToCap;
            return distanceToCap + cap * (time - timeToCap);
        }

        if (AerialXDecceleration <= 0f)
            return v0 * time;

        float timeToBleed = (v0 - cap) / AerialXDecceleration;

        if (time <= timeToBleed)
            return v0 * time - 0.5f * AerialXDecceleration * time * time;

        float distanceToBleed = v0 * timeToBleed - 0.5f * AerialXDecceleration * timeToBleed * timeToBleed;
        return distanceToBleed + cap * (time - timeToBleed);
    }

    #endregion
}
