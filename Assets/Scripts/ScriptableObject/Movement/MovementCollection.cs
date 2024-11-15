using System;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "MovementCollection", menuName = "ScriptableObject/Movement/Collection")]
public class MovementCollection : ScriptableObject
{
    public MoveData MoveData;
    public AirMoveData AirMoveData;
    public JumpData JumpData;
    public AirJumpData AirJumpData;
    public LandData LandData;
    public WallLeanData WallLeanData;
    public WallSlideData WallSlideData;
    public WallJumpData WallJumpData;

    
    /// <summary>
    /// Try get movement data based on type
    /// </summary>
    /// <param name="movementType"></param>
    /// <param name="requestedMovement"></param>
    /// <returns></returns>
    public bool TryGetMovementByType(MovementType movementType, out MovementData requestedMovement) 
    {
        switch (movementType)
        {
            case MovementType.Move:
                if (MoveData != null)
                {
                    requestedMovement = MoveData;
                    return true;
                }
                break;

            case MovementType.AirMove:
                if (AirMoveData != null)
                {
                    requestedMovement = AirMoveData;
                    return true;
                }
                break;

            case MovementType.Jump:
                if (JumpData != null)
                {
                    requestedMovement = JumpData;
                    return true;
                }
                break;

            case MovementType.AirJump:
                if (AirJumpData != null)
                {
                    requestedMovement = AirJumpData;
                    return true;
                }
                break;

            case MovementType.Landing:
                if (LandData != null)
                {
                    requestedMovement = LandData;
                    return true;
                }
                break;

            case MovementType.WallLean:
                if (WallLeanData != null)
                {
                    requestedMovement = WallLeanData;
                    return true;
                }
                break;

            case MovementType.WallSlide:
                if (WallSlideData != null)
                {
                    requestedMovement = WallSlideData;
                    return true;
                }
                break;

            case MovementType.WallJump:
                if (WallJumpData != null)
                {
                    requestedMovement = WallJumpData;
                    return true;
                }
                break;

        }

        requestedMovement = null;
        return false;
    }


    /// <summary>
    /// Get maxVelocity stored in collection
    /// </summary>
    /// <returns></returns>
    public float GetGroundedMaxXVelocity()
    {
        if (MoveData != null) 
            return MoveData.GroundedMaxXVelocity;

        throw new NullReferenceException("MoveData is not set");
    }


    /// <summary>
    /// Get ground acceneration in collection
    /// </summary>
    /// <returns></returns>
    public float GetGroundedXAcceleration()
    {
        if (MoveData != null)
            return MoveData.GroundedXAcceleration;

        throw new NullReferenceException("MoveData is not set");
    }


    /// <summary>
    /// Get ground decelleration in collection
    /// </summary>
    /// <returns></returns>
    public float GetGroundedXDeceleration()
    {
        if (MoveData != null)
            return MoveData.GroundedXDeceleration;

        throw new NullReferenceException("MoveData is not set");
    }


    /// <summary>
    /// Get air max velocity
    /// </summary>
    /// <returns></returns>
    public float GetAerialMaxVelocity()
    {
        if (AirMoveData != null)
            return AirMoveData.AerialMaxXVelocity;

        throw new NullReferenceException("AirMoveData is not set");
    }


    /// <summary>
    /// Get air acceleration in collection
    /// </summary>
    /// <returns></returns>
    public float GetArialXAcceleration()
    {
        if (AirMoveData != null)
            return AirMoveData.AerialXAcceleration;

        throw new NullReferenceException("AirMoveData is not set");
    }


    /// <summary>
    /// Get air deceleration
    /// </summary>
    /// <returns></returns>
    public float GetAerialXDeceleration()
    {
        if (AirMoveData != null)
            return AirMoveData.AerialXDeceleration;

        throw new NullReferenceException("AirMoveData is not set");
    }


    //TODO: Change this to TryGetJumpVelocity, Because jump is not required by sceneobjects
    public float GetJumpVelocity()
    {
        if (JumpData != null) 
            return JumpData.JumpVelocity;

        return 0;
    }


    /// <summary>
    /// Return the velocity for wall jump if available
    /// </summary>
    public bool TryGetWallJumpVelocity(out float jumpVelocity)
    {
        if (WallJumpData != null)
        {
            jumpVelocity = WallJumpData.JumpVelocity;
            return true;
        }

        jumpVelocity = 0;
        return false;
    }


    /// <summary>
    /// Get gravity scaler if jumpData exists
    /// </summary>
    /// <param name="gravityScaler"></param>
    /// <returns></returns>
    public bool TryGetGravityScaler(out float gravityScaler)
    {
        if (JumpData != null)
        {
            gravityScaler = JumpData.GravityScaler;
            return true;
        }

        gravityScaler = 1;
        return false;
    }


    /// <summary>
    /// Get movement data from animation
    /// </summary>
    /// <param name="animationName"></param>
    /// <param name="movement"></param>
    /// <returns></returns>
    public bool TryGetMovementFromAnimation(AnimationClip animation, out MovementData movement)
    {
        foreach (MovementType type in Enum.GetValues(typeof(MovementType)))
        {
            if (TryGetMovementByType(type, out movement))
            {
                if (movement.Animation == animation)
                    return true;                
            }
        }

        movement = null;
        return false;
    }
}
