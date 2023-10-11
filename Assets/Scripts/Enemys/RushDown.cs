using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RushDown : Enemy
{
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
            animator.PlayIdleAnimation();
        }
    }


    protected override void Alert()
    {
        animator.PlayAnimation("RushDownBaseAlert");
    }

    protected override void Attack()
    {
        //Rush down
    }

}
