using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AnimationStateData
{
    public string ClipName;
    public ActionState State;
    public AnimationTrigger[] Triggers;

    public AnimationStateData(string clipName, ActionState state, AnimationTrigger[] triggers)
    {
        ClipName = clipName;
        State = state;
        Triggers = triggers;
    }

    public AnimationStateData(AnimationClip animation, ActionState state, AnimationTrigger[] triggers)
    {
        this.ClipName = animation.name;
        this.State = state;
        this.Triggers = triggers;
    }
}
