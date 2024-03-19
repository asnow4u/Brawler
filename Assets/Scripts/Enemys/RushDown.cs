using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RushDown : Enemy
{
    private PlatformPathNavigator navigator;
    //private FlyingPathNavigator navigator;

    //TEMP
    public Transform pathTarget;

    protected override void Initialize()
    {
        base.Initialize();

        navigator = gameObject.AddComponent<PlatformPathNavigator>();
        //navigator = gameObject.AddComponent<FlyingPathNavigator>();


        //TEST PATH
        navigator.Initialize(NavigatorMovement);
        navigator.SetDestination(pathTarget.position);
    }


    /// <summary>
    /// Traverse to the next point in the path by moving
    /// </summary>
    private void NavigatorMovement(TraversalType type, float movementInfluence)
    {        
        switch (type)
        {
            case TraversalType.Move:                
                MovementInputHandler.PerformMovement(new Vector2(movementInfluence, 0));
                break;

            case TraversalType.Jump:                
                if (MovementInputHandler.IsGrounded)
                    MovementInputHandler.PerformJump(movementInfluence);

                break;
        }

    } 
}
