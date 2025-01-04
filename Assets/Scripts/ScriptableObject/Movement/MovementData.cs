using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.SceneObjects.Movement
{
    public class MovementData : ScriptableObject
    {    
        public AnimationClip Animation;
        public MovementType Type;
        public List<AnimationTrigger> Triggers;
    }
}



