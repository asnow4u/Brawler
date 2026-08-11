using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public abstract class HitBoxHandler : MonoBehaviour, IHitBoxHandler
{
    protected Rigidbody rb;
    protected IHitBox[] hitboxs;  
    protected HashSet<Guid> sceneObjectsHit = new HashSet<Guid>();

    public event Action<HitSenderData> OnHitConnected;


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
        if (hitboxs == null)
            return;

        foreach (IHitBox hitbox in hitboxs)
        {
            hitbox.ActivateHitBox();

            //Detach first so a second enable without an intervening disable cannot double subscribe
            hitbox.OnCollisionEntered -= OnHit;
            hitbox.OnCollisionEntered += OnHit;    
        }
    }

    protected virtual void DisableHitboxs()
    {
        if (hitboxs == null)
            return;

        foreach (IHitBox hitbox in hitboxs)
        {
            hitbox.DeactivateHitBox();
            hitbox.OnCollisionEntered -= OnHit;            
        }
    }

    protected void ClearHitRecord()
    {
        sceneObjectsHit.Clear();
    }

    protected abstract void OnHit(IHitBox hitBox, IHurtBox hurtBox, Vector3 hitPoint);


    protected void DeclareHit(HitSenderData hitBoxConnectedData, IHurtBox hurtBox, HitData hitData)
    {
        OnHitConnected?.Invoke(hitBoxConnectedData);
        hurtBox.Hit(hitData);
    }
}
