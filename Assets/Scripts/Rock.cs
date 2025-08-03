using Game.SceneObjects;
using UnityEngine;

public class Rock : SceneObject
{
    [SerializeField] private float launchAngle = 45f;
    [SerializeField] private float damage = 5;

    private DamageCollider damageCollider;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    //void Start()
    //{
    //    damageCollider = GetComponent<DamageCollider>();
    //    damageCollider.Enable(HandleCollision);
    //}


    //private void HandleCollision(ITakeDamage hitTarget, Collider col)
    //{        
    //    hitTarget.HitByAttack(1, col.ClosestPoint(col.transform.position), damage, launchAngle);
    //}


    public override void PerformMovement(Vector2 movement)
    {
        throw new System.NotImplementedException();
    }

    public override void StopHorizontalMovement()
    {
        throw new System.NotImplementedException();
    }

    public override void PerformVerticalJump(float jumpStrength)
    {
        throw new System.NotImplementedException();
    }

    public override void StopJumpMovement()
    {
        throw new System.NotImplementedException();
    }

    public override bool IsHorizontalMovementActive()
    {
        throw new System.NotImplementedException();
    }

    public override bool IsVerticalJumpActive()
    {
        throw new System.NotImplementedException();
    }

    public override void PerformUpAttack()
    {
        throw new System.NotImplementedException();
    }

    public override void PerformDownAttack()
    {
        throw new System.NotImplementedException();
    }

    public override void PerformLeftAttack()
    {
        throw new System.NotImplementedException();
    }

    public override void PerformRightAttack()
    {
        throw new System.NotImplementedException();
    }

    public override bool IsUpAttackActive()
    {
        throw new System.NotImplementedException();
    }

    public override bool IsDownAttackActive()
    {
        throw new System.NotImplementedException();
    }

    public override bool IsLeftAttackActive()
    {
        throw new System.NotImplementedException();
    }

    public override bool IsRightAttackActive()
    {
        throw new System.NotImplementedException();
    }

    public override void PerformInteraction()
    {
        throw new System.NotImplementedException();
    }
}
