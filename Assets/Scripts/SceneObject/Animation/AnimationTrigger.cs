using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AnimationTriggerType { Start, End, EnableCollider, DisableCollider, ChargeAction }

[Serializable]
public class AnimationTrigger
{

    public float TriggerFrame;
    public AnimationTriggerType TriggerType;
    [HideInInspector]
    public bool WasTriggered = false;


    public void Reset()
    {
        WasTriggered = false;
    }
}
