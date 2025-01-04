using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AttackCollision", menuName = "ScriptableObjects/DamageCollision")]
public class DamageCollisionData : ScriptableObject
{
    [Header("Trigger Animations")]
    [SerializeField] private List<AnimationClip> triggerAnimations;


    public bool Contains(AnimationClip clip)
    {
        return triggerAnimations.Contains(clip);
    }
}
