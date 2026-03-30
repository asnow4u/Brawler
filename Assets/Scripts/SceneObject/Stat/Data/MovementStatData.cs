using UnityEngine;

public class MovementStatData
{
    //Grounded Movement
    public float MaxGroundedVelocity;
    public float GroundedAcceleration;
    public float GroundedDecceleration;

    //Aerial Movement
    public float MaxAerialXVelocity;
    public float AerialXAcceleration;
    public float AerialXDecceleration;
    public float MaxAerialYVelocity;
    public float AerialYAcceleration;
    public float AerialUpYDecceleration;
    public float AerialDownYDecceleration;
    public float GravityMultiplier;    

    //Climb Movement
    public float MaxClimbXVelocity;
    public float MaxClimbUpYVelocity;
    public float MaxClimbDownYVelocity;
    public float ClimbSlideDecceleration;

    //Jump
    public float InitialJumpVelocity;
    public float JumpAcceleration;

    //Aerial Jump
    public float InitialAirJumpVelocity;
    public float AirJumpAcceleration;
    public int AirJumpsAvailable;    
}
