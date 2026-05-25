using System;
using UnityEngine;

[CreateAssetMenu(fileName = "IdleAnimationCollection", menuName = "ScriptableObjects/SceneObject/Animation/IdleAnimations")]
public class BaseAnimationCollection : ScriptableObject
{
    public AnimationData GroundIdleAnimation;
    public AnimationData AirIdleAnimation;
    public AnimationData HitStunAnimation;
}
