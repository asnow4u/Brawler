using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionStateHandler : IActionState
{
    private ActionState.State curState;

    private SceneObject sceneObj;
    private IAnimator animator { get { return sceneObj.Animator; } }

    public void Setup(SceneObject obj)
    {
        sceneObj = obj;
    }

    public bool ChangeState(ActionState.State requestedState)
    {
        if (curState < requestedState)
        {
            //Debug.Log("Action State " + curState.ToString() + " Changed to " + requestedState.ToString());
            curState = requestedState;
            return true;
        }

        if (curState == requestedState)
        {
            return true;
        }

        return false;
    }


    public void ResetState()
    {
        curState = ActionState.State.Idle;        
        animator.PlayIdleAnimation();
    }


    public bool CompairState(ActionState.State state)
    {
        if (state == curState) 
        {
            return true;
        }

        return false;
    }
}

