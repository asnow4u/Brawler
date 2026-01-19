using Game.SceneObjects;
using Game.SceneObjects.ActionStates;
using UnityEngine;

public class Rock : SceneObject
{
    [SerializeField] private float launchAngle = 45f;
    [SerializeField] private float damage = 5;

    private DamageCollider damageCollider;


    protected override void Initialize()
    {
        base.Initialize();

        damageCollider = GetComponentInChildren<DamageCollider>();       
    }


    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        if (Rb.linearVelocity.x != 0)
            damageCollider.Enable(HandleCollision);
        else
            damageCollider.Disable();
    }

    private void HandleCollision(ITakeDamage hitTarget, Collider col)
    {
        if (hitTarget.CheckForImmunity(this))
            return;

        hitTarget.HitByAttack(col.ClosestPoint(col.transform.position), 1, damage, launchAngle);
    }
}
