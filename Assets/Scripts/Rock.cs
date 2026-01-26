using Game.SceneObjects;
using Game.SceneObjects.ActionStates;
using UnityEngine;

public class Rock : SceneObject, IDealDamage
{
    [SerializeField] private float launchAngle = 45f;
    [SerializeField] private float damage = 5;

    private DamageCollider damageCollider;

    public float Influence => 1;
    public float Damage => 5;
    public float LaunchAngle => 45;

    protected override void Initialize()
    {
        base.Initialize();

        damageCollider = GetComponentInChildren<DamageCollider>();       
    }


    //TODO: This should be moved to CollisionHandler
    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        if (Rb.linearVelocity.x != 0)
            damageCollider.Enable(this);
        else
            damageCollider.Disable();
    }
}
