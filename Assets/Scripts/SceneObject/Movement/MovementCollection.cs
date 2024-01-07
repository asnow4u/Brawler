using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MovementType { Move, Jump, AirJump, Fall, Land, Roll }

[Serializable]
public class MovementCollection
{
    public List<MovementData> Movements;

    public bool TryGetMovementByType(MovementType movementType, out MovementData requestedMovement) 
    {
        foreach (MovementData move in Movements)
        {
            if (move.Type == movementType)
            {
                requestedMovement = move;
                return true;
            }
        }

        requestedMovement = null;
        return false;
    }


    public float GetCurMovementSpeed()
    {
        foreach (MovementData move in Movements)
        {
            if (move.Type == MovementType.Move)
            {
                return ((MoveData)move).XVelocityAcceleration;
            }
        }

        return 0;
    }


    public float GetCurJumpVelocity()
    {
        foreach (MovementData move in Movements)
        {
            if (move.Type == MovementType.Jump)
            {
                return ((JumpData)move).JumpVelocity;
            }
        }

        return 0;
    }


    public float GetCurAirJumpVelocity()
    {
        foreach (MovementData move in Movements)
        {
            if (move.Type == MovementType.AirJump)
            {
                return ((AirJumpData)move).AirJumpVelocity;
            }
        }

        return 0;
    }


    public bool TryGetMovementFromAnimationClip(string clipName, out MovementData movement)
    {
        foreach (MovementData move in Movements)
        {
            if (move.Animation.name == clipName)
            {
                movement = move;
                return true;
            }
        }

        movement = null;
        return false;
    }


}
