using UnityEngine;

public class MovementStatData
{
    public bool GroundedMovementValid = false;
    public bool DashValid = false;
    public bool AerialMovementValid = false;
    public bool GroundedJumpValid = false;
    public bool AerialJumpValid = false;
    public bool WallJumpValid = false;
    public bool LedgeClimbValid = false;
    public bool WallLeanValid = false;
    public bool WallSlideValid = false;

    //Grounded Movement
    public float MaxGroundedVelocity;
    public float GroundedAcceleration;
    public float GroundedDecceleration;
    public float GroundedAttackDecceleration;

    //Dash Movement
    public float WaveLandDashVelocity;
    public float WaveLandDashDuration;
    public float InitialDashVelocity;
    public float InitialDashDuration;
    public float InputDashVelocity;
    public float InputDashDuration;
    public float HorizontalEndDashVelocity;
    public float VerticalEndDashVelocity;
    public float DashSpeedHoldPercentage;

    //Aerial Movement
    public float MaxAerialXVelocity;
    public float AerialXAcceleration;
    public float AerialXDecceleration;
    public float MaxAerialRisingVelocity;
    public float MaxFallVelocity;
    public float AerialRisingDecceleration;

    //Gravity
    public float GravityRaising;
    public float GravityFalling;
    public float GravityFastFalling;
    public float GravityHitStunTravel;
    public float GravityHitStunRecovery;

    //Jump
    public float JumpVelocity;
    public float ShortHopVelocity;
    public float JumpSquatDuration;

    //Aerial Jump
    public float AirJumpVelocity;
    public int AirJumpsAvailable;

    //Wall Jump
    public float WallJumpVelocity;
    public float WallJumpAngle;

    //Wall Slide
    public float MaxWallSlideVelocity;
    public float WallSlideDeceleration;
}
