using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementCollection : MonoBehaviour
{
    [SerializeField] private List<Movement> movements;

    [System.Serializable]
    public class Movement
    {
        public MovementType.Type type; 
        public AnimationClip animationClip;
    }


    public bool GetMovementByType(MovementType.Type movementType, out Movement requestedMovement) 
    {
        requestedMovement = null;

        foreach (Movement move in movements)
        {
            if (move.type == movementType)
            {
                requestedMovement = move;
                return true;
            }
        }

        return false;
    }
}
