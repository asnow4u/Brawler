using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Attack", menuName = "ScriptableObject/Attack")]
public class AttackData : ScriptableObject
{
    [Header("Animation")]
    public AnimationClip AttackAnimation;

    [Header("Attack Details")]
    [SerializeField] private AnimationCurve damageCurve;
    [SerializeField] private AnimationCurve knockbackCurve;
    [SerializeField] private AnimationCurve launchAngleCurve;
    [Range(0f, 1f)]
    [SerializeField] private float damageInfluence;
    
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
    public List<AttackCollider.Type> ColliderType;


    public float GetAttackDamage(float frameNumber)
    {
        return damageCurve.Evaluate(frameNumber);
    }

    public float GetAttackInflucence()
    {
        return damageInfluence;
    }


    public float GetAttackKnockBack(float frameNumber)
    {
        return knockbackCurve.Evaluate(frameNumber);
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
