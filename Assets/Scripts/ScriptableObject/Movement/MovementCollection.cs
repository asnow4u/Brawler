using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "MovementCollection", menuName = "ScriptableObject/Movement/Collection")]
public class MovementCollection : ScriptableObject
{
    [SerializeField] private List<MovementData> Movements;


    public bool ContainsMovementType(MovementType type)
    {
        foreach (MovementData move in Movements)
        {
            if (move.Type == type)
                return true;
        }

        return false;
    }


    //TODO: Might remove
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


    public float GetMaxXVelocity()
    {
        foreach (MovementData move in Movements)
        {
            if (move.Type == MovementType.Move)
            {
                return ((MoveData)move).MaxXVelocity;
            }
        }

        return 0;
    }


    public float GetGroundedXAcceleration()
    {
        foreach (MovementData move in Movements)
        {
            if (move.Type == MovementType.Move)
            {
                return ((MoveData)move).GroundedXAcceleration;
            }
        }

        return 0;
    }


    public float GetGroundedXDeceleration()
    {
        foreach (MovementData move in Movements)
        {
            if (move.Type == MovementType.Move)
            {
                return ((MoveData)move).GroundedXDeceleration;
            }
        }

        return 0;
    }


    public float GetArialXAcceleration()
    {
        foreach (MovementData move in Movements)
        {
            if (move.Type == MovementType.Move)
            {
                return ((MoveData)move).ArialXAcceleration;
            }
        }

        return 0;
    }


    public float GetArialXDeceleration()
    {
        foreach (MovementData move in Movements)
        {
            if (move.Type == MovementType.Move)
            {
                return ((MoveData)move).ArialXDeceleration;
            }
        }

        return 0;
    }


    public float GetJumpVelocity()
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


    public float GetGravityScaler()
    {
        foreach (MovementData move in Movements)
        {
            if (move.Type == MovementType.Jump)
                return ((JumpData)move).GravityScaler;
        }

        return 1;
    }


    public bool TryGetMovementFromAnimation(string animationName, out MovementData movement)
    {
        foreach (MovementData move in Movements)
        {
            if (move.Animation.name == animationName)
            {
                movement = move;
                return true;
            }
        }

        movement = null;
        return false;
    }


}
