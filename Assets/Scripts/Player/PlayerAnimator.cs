using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimator : AnimatorStateMachine
{
    public override void OnAnimationDisableColliders(AnimationClip clip)
    {
        base.OnAnimationDisableColliders(clip);

        Debug.Log("PlayerAnimation: Animation Disable Colliders " + clip.name);        
    }

    public override void OnAnimationEnableColliders(AnimationClip clip)
    {
        base.OnAnimationEnableColliders(clip);

        Debug.Log("PlayerAnimation: Animation Enable Colliders " + clip.name);
    }

    public override void OnAnimationEnd(AnimationClip clip)
    {
        base.OnAnimationEnd(clip);

        Debug.Log("PlayerAnimation: Animation Ended " + clip.name);
        //TODO: Switch to idle state based on isgrounded

    }

    public override void OnAnimationStarted(AnimationClip clip)
    {
        base.OnAnimationStarted(clip);

        Debug.Log("PlayerAnimation: Animation Started " + clip.name);
    }
}
