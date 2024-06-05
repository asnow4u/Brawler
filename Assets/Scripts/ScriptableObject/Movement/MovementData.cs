using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class MovementData : ScriptableObject
{    
    public AnimationClip Animation;
    public MovementType Type;
    public List<AnimationTrigger> Triggers;
}



