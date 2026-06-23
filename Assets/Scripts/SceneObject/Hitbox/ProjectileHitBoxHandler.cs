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

    private void Start()
    {
        foreach (IHitBox hitbox in hitboxs)
            hitbox.SetOwner(sceneObject.UniqueID);
    }

    public void SetHitData(HitData hitData)
    {
        baseHitData = hitData;
        EnableHitBoxs();
    }    

    protected override void OnHit(IHitBox hitBox, IHurtBox hurtBox, Vector3 hitPoint)
    {
        if (sceneObjectsHit.Contains(hurtBox.OwnerID))
            return;

        sceneObjectsHit.Add(hurtBox.OwnerID);
        
        float launchAngle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
        if (launchAngle < 0f)
            launchAngle += 360f;

        HitData hitData = new HitData(
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
