using UnityEngine;

[CreateAssetMenu(fileName = "IdleAnimationCollection", menuName = "ScriptableObjects/SceneObject/Animation/IdleAnimations")]
public class BaseAnimationCollection : ScriptableObject
{
    public AnimationClip GroundIdleAnimation;
    public AnimationClip AirIdleAnimation;

    public AnimationClip HitStunAnimation;
}
