using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.SceneObjects.Movement
{
    public class MovementInputData : ScriptableObject
    {   
        public MovementType Type;
        public AnimationClip Animation;
        public float AnimationSpeedMultiplier;
    }
}



