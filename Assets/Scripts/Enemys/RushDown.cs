using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(PlatformPathNavigator))]
public class RushDown : Enemy
{
    private PlatformPathNavigator navigator => GetComponent<PlatformPathNavigator>();

    [Header("Patrol")]
    [SerializeField] private List<Transform> patrolPoints = new List<Transform>();
    private int patrolPointIndex;

    protected override void Initialize()
    {
        base.Initialize();

        SetUpEvents();

        NavigateToPatrolPoint(0);
    }

    #region Events

    private void SetUpEvents()
    {
        //Navigator
        navigator.PathMovementEvent += OnNavigatorMovement;
        navigator.DestinationReachedEvent += OnDestinationReached;
    }


    /// <summary>
    /// Traverse to the next point in the path by moving
    /// </summary>
    private void OnNavigatorMovement(EdgeType type, float movementInfluence)
    {   
        switch (type)
        {
            case EdgeType.Ground:                
                MovementInputHandler.PerformMovement(new Vector2(movementInfluence, 0));
                break;

            case EdgeType.Jump:                
                MovementInputHandler.PerformJump(movementInfluence);
                break;
        }
    }
    

    private void OnDestinationReached()
    {
        NavigateToNextPatrolPoint();
    }

    #endregion


    #region Patrol Points

    /// <summary>
    /// Begin navigating to the desired patrol point
    /// Handles if over/under the amount within patrolPoints
    /// </summary>
    /// <param name="index"></param>
    private void NavigateToPatrolPoint(int index)
    {
        if (patrolPoints.Count > 0)
        {
            //If over do the max amount
            if (index >= patrolPoints.Count)
            {
                navigator.SetDestination(patrolPoints.Last().position);
                patrolPointIndex = patrolPoints.Count - 1;
            }

            //If under do the min
            else if (index <= 0)
            {
                navigator.SetDestination(patrolPoints[0].position);
                patrolPointIndex = 0;
            }

            else
            {
                navigator.SetDestination(patrolPoints[index].position);
                patrolPointIndex = index;
            }
        }
    }


    /// <summary>
    /// Start navigating towards the next patrol point
    /// </summary>
    private void NavigateToNextPatrolPoint()
    {
        if (patrolPoints.Count > 0)
        {
            patrolPointIndex++;

            //Check for overlap
            if (patrolPointIndex == patrolPoints.Count)
                patrolPointIndex = 0;

            navigator.SetDestination(patrolPoints[patrolPointIndex].position);
        }
    }

    #endregion

    //TODO:
    //Need to be able to wander
    //Wandering will need to be somewhat preset

    //If player is seen attempt to chase after player
    //Will need to update path on occation. Dont want to update the path every frame as that would be far to costly
    //Suppose could update the path when sight is lost, or when end of current path is reached (As long as the next graph calculation is fast)


    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            if (patrolPoints.Count > 0) 
            {
                Gizmos.color = Color.black;
                Gizmos.DrawLine(transform.position, patrolPoints[patrolPointIndex].position);
            }
        }

    }

}
