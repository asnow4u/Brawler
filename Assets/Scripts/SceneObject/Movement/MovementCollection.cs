using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MovementType { Move, Jump, AirJump, Fall, Land, Roll }

public class MovementCollection : MonoBehaviour
{
    [SerializeField] private List<MovementData> movements;

    public bool GetMovementByType(MovementType movementType, out MovementData requestedMovement) 
    {
        requestedMovement = null;

        foreach (MovementData move in movements)
        {
            if (move.Type == movementType)
            {
                requestedMovement = move;
                return true;
            }
        }

        return false;
    }


    

}
