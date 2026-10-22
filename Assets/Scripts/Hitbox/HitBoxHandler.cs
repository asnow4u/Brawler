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
    public event Action<HitSenderData, HitResponse> OnHitBlocked;


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


    /// <summary>Delivers the hit to the hurtbox, then raises OnHitConnected or OnHitBlocked from the response.</summary>
    protected HitResponse DeclareHit(HitSenderData hitBoxConnectedData, IHurtBox hurtBox, HitData hitData)
    {
        HitResponse response = hurtBox.Hit(hitData);

        if (response.Result == HitResult.Hit)
            OnHitConnected?.Invoke(hitBoxConnectedData);
        else if (response.Result == HitResult.Blocked)
            OnHitBlocked?.Invoke(hitBoxConnectedData, response);

        return response;
    }
}
