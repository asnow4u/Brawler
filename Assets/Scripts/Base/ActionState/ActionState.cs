using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionState
{
    public enum State { Idle, Moving, Attacking, Damaged, Dead };
    private State curState;
    public State CurState => curState;

    public bool ChangeState(State state)
    {
        if (curState < state)
        {
            curState = state;
            return true;
        }

        if (curState == state)
        {
            return true;
        }

        return false;
    }


    public void ResetState()
    {
        curState = State.Idle;
    }
}

