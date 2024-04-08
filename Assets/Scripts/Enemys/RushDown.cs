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
    


    //TODO:
    //Need to be able to wander
    //Wandering will need to be somewhat preset

    //If player is seen attempt to chase after player
    //Will need to update path on occation. Dont want to update the path every frame as that would be far to costly
    //Suppose could update the path when sight is lost, or when end of current path is reached (As long as the next graph calculation is fast)
   
    
}
