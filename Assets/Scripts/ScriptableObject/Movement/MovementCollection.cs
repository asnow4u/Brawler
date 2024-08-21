using System;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "MovementCollection", menuName = "ScriptableObject/Movement/Collection")]
public class MovementCollection : ScriptableObject
{
    public MoveData MoveData;
    public JumpData JumpData;
    public AirJumpData AirJumpData;
    public LandData LandData;

    
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
        }

        requestedMovement = null;
        return false;
    }


    /// <summary>
    /// Get maxVelocity stored in collection
    /// </summary>
    /// <returns></returns>
    public float GetMaxXVelocity()
    {
        if (MoveData != null) 
            return MoveData.MaxXVelocity;

        return 0;
    }


    /// <summary>
    /// Get ground acceneration in collection
    /// </summary>
    /// <returns></returns>
    public float GetGroundedXAcceleration()
    {
        if (MoveData != null)
            return MoveData.GroundedXAcceleration;

        return 0;
    }


    /// <summary>
    /// Get ground decelleration in collection
    /// </summary>
    /// <returns></returns>
    public float GetGroundedXDeceleration()
    {
        if (MoveData != null)
            return MoveData.GroundedXDeceleration;

        return 0;
    }


    /// <summary>
    /// Get air acceleration in collection
    /// </summary>
    /// <returns></returns>
    public float GetArialXAcceleration()
    {
        if (MoveData != null)
            return MoveData.ArialXAcceleration;

        return 0;
    }


    /// <summary>
    /// Get air deceleration
    /// </summary>
    /// <returns></returns>
    public float GetArialXDeceleration()
    {
        if (MoveData != null)
            return MoveData.ArialXDeceleration;

        return 0;
    }


    //TODO: Change this to TryGetJumpVelocity, Because jump is not required by sceneobjects
    public float GetJumpVelocity()
    {
        if (JumpData != null) 
            return JumpData.JumpVelocity;

        return 0;
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
