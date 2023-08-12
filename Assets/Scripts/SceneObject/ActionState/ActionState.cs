using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionState
{
    public enum State 
    { 
        Idle, 
        Moving, 
        Attacking, 
        HitStun, 
        Dead 
    };
}

