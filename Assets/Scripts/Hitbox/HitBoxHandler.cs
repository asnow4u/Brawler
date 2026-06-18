using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public abstract class HitBoxHandler : MonoBehaviour, IHitBoxHandler
{
    protected Rigidbody rb;
    protected IHitBox[] hitboxs;  
    protected HashSet<Guid> sceneObjectsHit = new HashSet<Guid>();

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        hitboxs = CollectHitboxs();

        if (hitboxs == null || hitboxs.Length == 0)
            Debug.LogWarning("HitBoxHandler: No Hitboxs found", gameObject);

        RegisterToEvents();
    }

    protected virtual void RegisterToEvents()
    { }

    private void OnDestroy()
    {
        UnregisterFromEvents();
    }

    protected virtual void UnregisterFromEvents()
    { }

    protected abstract IHitBox[] CollectHitboxs();

    protected virtual void EnableHitBoxs()
    {
        foreach (IHitBox hitbox in hitboxs)
        {
            hitbox.ActivateHitBox();
            hitbox.OnCollisionEntered += OnHit;    
        }
    }

    protected virtual void DisableHitboxs()
    {
        foreach (IHitBox hitbox in hitboxs)
        {
            hitbox.DeactivateHitBox();
            hitbox.OnCollisionEntered -= OnHit;            
        }

        sceneObjectsHit.Clear();
    }

    protected abstract void OnHit(IHitBox hitBox, IHurtBox hurtBox, Vector3 hitPoint);
}
