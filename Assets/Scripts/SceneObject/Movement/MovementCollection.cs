using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementCollection : MonoBehaviour
{
    [SerializeField] private List<MovementData> movements;

    public bool GetMovementByType(MovementType.Type movementType, out MovementData requestedMovement) 
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
