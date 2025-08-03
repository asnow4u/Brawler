using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DamageCollider : MonoBehaviour
{
    protected Collider damageCollider;   
    protected Action<ITakeDamage, Collider> hitCallback;


    public virtual void Awake()
    {
        damageCollider = GetComponent<Collider>();        
        Disable();
    }


    /// <summary>
    /// Enable collider and establish <paramref name="attackHitCallback"/> if collider hits
    /// </summary>
    public void Enable(Action<ITakeDamage, Collider> attackHitCallback)
    {
        damageCollider.enabled = true;
        hitCallback = attackHitCallback;
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
                hitCallback?.Invoke(hitTarget, col);
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
