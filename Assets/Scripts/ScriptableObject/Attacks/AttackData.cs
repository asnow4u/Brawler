using Game.SceneObjects.Attack;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Attack", menuName = "ScriptableObjects/Attack/Attack")]
public class AttackData : ScriptableObject
{
    [Header("AttackType")]
    public AttackType Type;

    [Header("Animation")]
    public AnimationClip Animation;
    public float AnimationSpeedMultiplier;

    [Header("Attack Details")]

    //NOTE: This is to help differentiate between attacks from different weapons. A attack from a dagger should not launch the same distance as a warhammers.
    //      Perhaps instead of each attack being defined, the influence could be defined based on the weapon type.
    [Tooltip("Influence defines the amount of knockback velocity that will be applied when the attack makes contact\n 0 is no additional knockback\n 1 is full knockback")]
    [Range(0, 1)]
    [SerializeField] private float influence;

    [SerializeField] private AnimationCurve damageCurve;

    [Tooltip("Launch angle is assuming that\n 0 is the forward\n 90 is the up\n 180 is backwards\n 270 is down")]
    [SerializeField] private float launchAngle;    

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
    [Tooltip("Trigger to be fired to determine if attack is a strong attack")]
    [SerializeField] private AnimationTrigger strongAttack;

    [Space]
    [Tooltip("Any additional triggers to be called during the animation")]
    [SerializeField] private List<AnimationTrigger> otherTriggers;


    public float Influence => influence;
    public float LaunchAngle => launchAngle;


    public float GetAttackDamage(float frameNumber)
    {
        return damageCurve.Evaluate(frameNumber);
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
