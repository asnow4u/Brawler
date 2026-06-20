using UnityEngine;

public class MovementStatData
{
    public bool GroundedMovementValid = false;
    public bool DashValid = false;
    public bool AerialMovementValid = false;
    public bool ClimbMovementValid = false;
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

    //Dash Movement
    public float WaveLandDashVelocityScaler;
    public float WaveLandDashDuration;
    public float InitialDashVelocityScaler;
    public float InitialDashDuration;
    public float HorizontalDashVelocity;
    public float HorizontalDashDuration;

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

    //Climb Movement
    public float MaxClimbXVelocity;
    public float MaxClimbUpYVelocity;
    public float MaxClimbDownYVelocity;
    public float ClimbSlideDecceleration;

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
