using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimator : AnimatorStateMachine
{
    public override void OnAnimationDisableColliders()
    {
        Debug.Log("Animation Disable Colliders");
        //NOTE: this is only accounting for the attacking actions and nothing else
        //This will need to account for other movement and defensive actions as well

        actionManager.AttackCollidersDisabled();
    }

    public override void OnAnimationEnableColliders()
    {
        Debug.Log("Animation Enable Colliders");
        //NOTE: this is only accounting for the attacking actions and nothing else
        //This will need to account for other movement and defensive actions as well

        actionManager.AttackCollidersEnabled();
    }

    public override void OnAnimationEnd()
    {
        base.OnAnimationEnd();

        //TODO: Switch to idle state based on isgrounded

        actionManager.AttackEnded(curAnimationPlaying);
    }

    public override void OnAnimationStarted()
    {
        //NOTE: this is only accounting for the attacking actions and nothing else
        //This will need to account for other movement and defensive actions as well

        actionManager.AttackStarted(curAnimationPlaying);

    }
}
