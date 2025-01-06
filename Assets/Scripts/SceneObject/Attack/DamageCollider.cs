using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DamageCollider : MonoBehaviour
{
    private Collider damageCollider;
    [SerializeField] private DamageCollisionData triggerAnimationData;

    Action<ITakeDamage, Collider> hitCallback;


    public void Awake()
    {
        if (triggerAnimationData == null)
            Debug.LogException(new NullReferenceException("Damage Collision Data Not Set"), this);

        damageCollider = GetComponent<Collider>();
        Disable();
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


    /// <summary>
    /// Disable collider. Nullify any established callbacks
    /// </summary>
    public void Disable()
    {
        damageCollider.enabled = false;
        hitCallback = null;
    }



    private void OnTriggerEnter(Collider col)
    {
        if (col.gameObject.layer == LayerMask.NameToLayer("Ragdoll") ||
            col.gameObject.layer == LayerMask.NameToLayer("DamageHitBox"))
        {
            ITakeDamage hitTarget = col.GetComponentInParent<ITakeDamage>();
            if (hitTarget != null)
            {
                hitCallback?.Invoke(hitTarget, col);
            }
        }
    }



    public void OnDrawGizmos()
    {
        if (Application.isPlaying && GizmosHandler.DamageColliderGizmosEnabled)
        {
            if (damageCollider.enabled)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.color = Color.red;
                
                if (damageCollider is BoxCollider boxCollider2)
                    Gizmos.DrawWireCube(boxCollider2.center, boxCollider2.size);

                else if (damageCollider is SphereCollider sphereCollider)
                    Gizmos.DrawWireSphere(sphereCollider.center, sphereCollider.radius);

                else if (damageCollider is CapsuleCollider capsuleCollider)
                {
                    Gizmos.DrawWireSphere(capsuleCollider.center - capsuleCollider.transform.up * capsuleCollider.height / 2, capsuleCollider.radius);
                    Gizmos.DrawWireSphere(capsuleCollider.center + capsuleCollider.transform.up * capsuleCollider.height / 2, capsuleCollider.radius);
                }
            }
        }
    }
}
