using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class AnimationTrigger
{
    public enum Type { Start, End, EnableCollider, DisableCollider }

    [Range(0f, 1f)]
    public float NoramlizedTriggerTime;
    public Type TriggerType;
    public bool WasTriggered = false;
}
