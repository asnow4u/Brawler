using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class AnimationTrigger
{
    public enum Type { Start, End, EnableCollider, DisableCollider }

    public float TriggerFrame;
    public Type TriggerType;
    [HideInInspector]
    public bool WasTriggered = false;


    public void Reset()
    {
        WasTriggered = false;
    }
}
