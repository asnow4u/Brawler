using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.SceneObjects.Movement
{
    public class MovementInputData : ScriptableObject
    {    
        public AnimationClip Animation;
        public float AnimationSpeedMultiplier;
        public MovementType Type;
    }
}



