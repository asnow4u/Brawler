using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RushDown : Enemy
{
    protected override void Initialize()
    {
        base.Initialize();

        pathFinder = gameObject.AddComponent<PlatformPathFinder>();
    }


    protected override void Idle()
    {
        if (patrolSpots.Count > 0 && patrolTimer == 0 )  
        {            
            if (CheckRangeOfTarget())
            {
                StartCoroutine(StartPartrolWaitTimer());
            }

            else
            {
                MoveTowardsTargetSpot();
            }            
        }

        else
        {
            Animator.PlayIdleAnimation();
        }
    }


    protected override void Alert()
    {
        Animator.PlayAnimation("RushDownBaseAlert");
    }

    protected override void Attack()
    {
        //Rush down
    }

}
