using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RushDown : Enemy
{
    private PlatformPathNavigator navigator;

    //TEMP
    public Transform pathTarget;

    protected override void Initialize()
    {
        base.Initialize();

        navigator = GetComponent<PlatformPathNavigator>();                
        navigator.SetDestination(pathTarget.position);

        navigator.PathMovementEvent += NavigatorMovement;
    }


    /// <summary>
    /// Traverse to the next point in the path by moving
    /// </summary>
    private void NavigatorMovement(EdgeType type, float movementInfluence)
    {   
        switch (type)
        {
            case EdgeType.Ground:                
                MovementInputHandler.PerformMovement(new Vector2(movementInfluence, 0));
                break;

            case EdgeType.Jump:                
                if (MovementInputHandler.IsGrounded)
                    MovementInputHandler.PerformJump(movementInfluence);

                break;
        }
    } 
}
