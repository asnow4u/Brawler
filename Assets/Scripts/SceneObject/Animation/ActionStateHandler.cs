using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ActionState //TODO: come up with a better name for moveTransition (Landing)
{
    Null, Idle, Moving, Attacking, MoveTransition, HitStun
};


public class ActionStateHandler : MonoBehaviour
{
    [SerializeField] private ActionState curActionState;
    public ActionState CurActionState => curActionState;

    public event Action<ActionState> ActionStateChangedEvent;

    #region Initialize

    public void SetUp()
    {
        curActionState = ActionState.Idle;
    }

    #endregion


    #region Events

    //TODO:

    #endregion


    #region Action State

    /// <summary>
    /// Atempt to change action state
    /// </summary>
    /// <param name="newState"></param>
    /// <returns></returns>
    public bool TryChangeState(ActionState newState)
    {   
        if (newState >= curActionState)
        {
            Debug.Log("STATE: " + newState);
            curActionState = newState;

            ActionStateChangedEvent?.Invoke(curActionState);

            return true;
        }

        return false;
    }


    /// <summary>
    /// Reset State to Idle
    /// </summary>
    private void ResetState()
    {
        curActionState = ActionState.Idle;

        ActionStateChangedEvent?.Invoke(curActionState);
    }

    #endregion


   

}