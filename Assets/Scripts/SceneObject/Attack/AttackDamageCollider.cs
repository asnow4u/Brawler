using System;
using System.Threading;
using UnityEngine;

public class AttackDamageCollider : DamageCollider
{
    [SerializeField] private DamageCollisionData triggerAnimationData;


    public override void Awake()
    {
        if (triggerAnimationData == null)
            Debug.LogException(new MissingReferenceException("Damage Collision Data Not Set"), this);
        
        base.Awake();
    }


    /// <summary>
    /// Enable collider if dependent on <paramref name="clip"/> and establish <paramref name="attackHitCallback"/> if collider hits
    /// </summary>
    public void Enable(AnimationClip clip, Action<ITakeDamage, Collider> attackHitCallback)
    {
        if (triggerAnimationData.Contains(clip))
        {
            damageCollider.enabled = true;
            hitCallback = attackHitCallback;
        }
    }
}
