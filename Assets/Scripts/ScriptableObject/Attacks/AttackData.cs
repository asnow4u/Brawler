using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Attack", menuName = "ScriptableObject/Attack")]
public class AttackData : ScriptableObject
{
    [Header("Animation")]
    public AnimationClip AttackAnimation;

    [Header("Attack Details")]
    [SerializeField] private float influence;
    [SerializeField] private AnimationCurve damageCurve;
    [SerializeField] private AnimationCurve launchAngleCurve;    

    [Header("Triggers")]
    [Tooltip("Trigger to be fired when attack colliders should be enabled")]
    [SerializeField] private AnimationTrigger enableCollider;
    
    [Space]
    [Tooltip("Trigger to be fired when attack colliders should be disabled")]
    [SerializeField] private AnimationTrigger disableCollider;

    [Space]
    [Tooltip("Trigger to be fired when attack animation is ending")]
    [SerializeField] private AnimationTrigger end;

    [Space]
    [Tooltip("Any additional triggers to be called during the animation")]
    [SerializeField] private List<AnimationTrigger> otherTriggers;

    [Header("Colliders")]
    public List<AttackColliderType> ColliderType;


    public float GetInfluence()
    {
        return influence;
    }


    public float GetAttackDamage(float frameNumber)
    {
        return damageCurve.Evaluate(frameNumber);
    }


    public float GetAttackLaunchAngle(float frameNumber)    
    {
        return launchAngleCurve.Evaluate(frameNumber);
    }


    public AnimationTrigger[] GetAttackTriggers()
    {
        List<AnimationTrigger> triggers = new List<AnimationTrigger>
        {
            enableCollider,
            disableCollider,
            end
        };

        triggers.AddRange(otherTriggers);

        return triggers.ToArray();
    }
}
