using System.Collections.Generic;
using UnityEngine;

public class AnimationStatData
{
    public AnimationClip GroundedIdleAnimation;
    public AnimationClip AirIdleAnimation;
    public AnimationClip ClimbIdleAnimation;

    public Dictionary<int, AnimationClip> MovementAnimations;
    public Dictionary<int, AnimationClip> AttackAnimations;

    public AnimationClip HitStunAnimation;
}
