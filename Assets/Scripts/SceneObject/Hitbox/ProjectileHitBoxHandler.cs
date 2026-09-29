using System;
using UnityEngine;

[RequireComponent(typeof(ISceneObject))]
public class ProjectileHitBoxHandler : HitBoxHandler
{
    private ISceneObject sceneObject;
    private HitData baseHitData;

    protected override void Awake()
    {
        sceneObject = GetComponent<ISceneObject>();

        base.Awake();
    }


    protected override IHitBox[] CollectHitboxs()
    {
        return GetComponentsInChildren<IHitBox>();
    }

    /// <summary>Sets the owner the hitboxes ignore and the hit data applied on contact, then enables the hitboxes.</summary>
    public void Initialize(Guid ownerID, HitData hitData)
    {
        foreach (IHitBox hitbox in hitboxs)
            hitbox.SetOwner(ownerID);

        baseHitData = hitData;
        ClearHitRecord();
        EnableHitBoxs();
    }

    /// <summary>Sets a new owner for the hitboxes to ignore and clears the hit record.</summary>
    public void TransferOwnership(Guid ownerID)
    {
        foreach (IHitBox hitbox in hitboxs)
            hitbox.SetOwner(ownerID);

        ClearHitRecord();
    }

    protected override void OnHit(IHitBox hitBox, IHurtBox hurtBox, Vector3 hitPoint)
    {
        // A projectile never hits its own hurtbox.
        if (hurtBox.OwnerID == sceneObject.UniqueID)
            return;

        if (sceneObjectsHit.Contains(hurtBox.OwnerID))
            return;

        sceneObjectsHit.Add(hurtBox.OwnerID);
        
        float launchAngle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
        if (launchAngle < 0f)
            launchAngle += 360f;

        HitData hitData = new HitData(
            baseHitData.BaseForce,            
            baseHitData.Influence,
            launchAngle,
            baseHitData.Damage,
            baseHitData.StunTime,
            hitPoint,
            baseHitData.EffectIndex
        );

        hurtBox.Hit(hitData);

        Destroy(gameObject);
    }
}
