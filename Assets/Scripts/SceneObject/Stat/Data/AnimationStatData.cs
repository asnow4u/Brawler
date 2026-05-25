using System.Collections.Generic;
using UnityEngine;

public class AnimationStatData
{    
    public class AnimationData
    {
        public AnimationClip Animation;
        public float AnimationSpeed;

        public AnimationData(AnimationClip animation, float animationSpeed)
        {
            Animation = animation;
            AnimationSpeed = animationSpeed;
        }
    }

    public AnimationData GroundedIdleAnimation;
    public AnimationData AirIdleAnimation;
    public AnimationData ClimbIdleAnimation;

    public Dictionary<int, AnimationData> MovementAnimations;
    public Dictionary<int, AnimationData> AttackAnimations;

    public AnimationData HitStunAnimation;
}
