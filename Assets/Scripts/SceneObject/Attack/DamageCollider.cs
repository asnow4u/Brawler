using Game.SceneObjects;
using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DamageCollider : MonoBehaviour
{
    protected Collider damageCollider;
    protected IDealDamage sourceHitData;

    public event Action<DamageCollisionHitData> OnHit;

    public virtual void Awake()
    {
        damageCollider = GetComponent<Collider>();        
        Disable();
    }

    public void Enable(IDealDamage sourceData)
    {
        sourceHitData = sourceData;
        damageCollider.enabled = true;
    }

    public void Disable()
    {
        sourceHitData = null;
        damageCollider.enabled = false;
    }

    private void OnTriggerEnter(Collider col)
    {
        if (col.gameObject.layer == LayerMask.NameToLayer("Ragdoll") ||
            col.gameObject.layer == LayerMask.NameToLayer("DamageHitBox"))
        {
            SceneObject target = col.GetComponentInParent<SceneObject>();
            if (target != null)
            {
                DamageCollisionHitData hitData = new DamageCollisionHitData
                {                    
                    SourceData = sourceHitData,
                    TargetData = new TargetHitData
                    {
                        SceneObject = target,
                        HitCollider = col,
                        ContactPoint = col.ClosestPoint(transform.position)
                    }
                };

                OnHit?.Invoke(hitData);
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
