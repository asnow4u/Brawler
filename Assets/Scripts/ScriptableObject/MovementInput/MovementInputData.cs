using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.SceneObjects.Movement
{
    public class MovementInputData : ScriptableObject
    {   
        public int Index; //NOTE: This gets set by MovementInputCollection.IndexData()
        public MovementType Type;
        public AnimationClip Animation;
        public float AnimationSpeedMultiplier;
    }
}



