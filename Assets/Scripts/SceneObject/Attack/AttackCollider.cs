using System;
using System.Collections.Generic;
using UnityEngine;

public class AttackCollider : MonoBehaviour
{
    private Collider collider;
    [SerializeField] private List<AnimationClip> depedentAnimations;

    Action<ITakeDamage, Collider> hitCallback;


    public void Initialize()
    {
        collider = GetComponent<Collider>();
        Disable();
    }


    /// <summary>
    /// Enable collider if dependent on <paramref name="clip"/> and establish <paramref name="attackHitCallback"/> if collider hits
    /// </summary>
    public void Enable(AnimationClip clip, Action<ITakeDamage, Collider> attackHitCallback)
    {
        if (depedentAnimations.Contains(clip))
        {
            collider.enabled = true;
            hitCallback = attackHitCallback;
        }
    }


    /// <summary>
    /// Disable collider. Nullify any established callbacks
    /// </summary>
    public void Disable()
    {
        collider.enabled = false;
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
}
