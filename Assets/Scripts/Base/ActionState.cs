using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Animation
{
    public class AnimationPriorityState : MonoBehaviour
    {
        public enum State { Idle, Moveing, Jumping, Landing, Attacking, Damaged, Dead };
    }
}
