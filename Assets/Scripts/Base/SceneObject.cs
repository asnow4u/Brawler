using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AnimationHandler))]
public abstract class SceneObject : MonoBehaviour, IActionState, IDamage
{
    //public ActionRouter router;
    public Rigidbody rb;
    public IAnimator animator;
    public bool isGrounded;

    protected float damageTaken;

    private void Start()
    {
        Initialize();
    }

    

    protected virtual void Initialize()
    {               
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<IAnimator>();     
        
        animator.SetUp(this);
    }


    //TODO: make two rays on each side to prevent landing just on the edge and not getting jump reset
    protected void FixedUpdate()
    {
        if (Physics.Raycast(GetComponent<Collider>().bounds.center, Vector3.down, transform.GetComponent<Collider>().bounds.size.y / 2 + 0.1f, ~LayerMask.NameToLayer("Ground")))
        {
            if (rb.velocity.y < 0 && !isGrounded)
            {
                isGrounded = true;
                //router.OnLand(rb.velocity.y);
            }
        }
        else
        {
            isGrounded = false;
        }
    }


    public void TurnAround()
    {
        transform.Rotate(transform.up, 180f);
    }


    public bool IsFacingRightDirection()
    {
        float angleRightDiff = Vector3.Angle(transform.right, Vector3.right);
        float angleLeftDiff = Vector3.Angle(transform.right, Vector3.left);

        if (angleRightDiff < angleLeftDiff)
        {
            return true;
        }

        return false;
    }      


    public void ResetDamaget()
    {
        damageTaken = 0;
    }

    public void AddDamage(float percent)
    {
        damageTaken += percent;
    }   

    public void RemoveDamage(float percent)
    {
        damageTaken -= percent;
    }   

    public void ApplyForce(float basePower, Vector2 direction)
    {

        //Grab here in case change to mass
        Rigidbody rb = GetComponent<Rigidbody>();

        float force = rb.mass * (basePower * damageTaken);

        rb.AddForce(new Vector3(direction.x, direction.y, 0) * force, ForceMode.Impulse);

        //TODO: Set drag amount to 0.1 ish
        //Drag should reset when ground is touched or immobile is finished
    }

    public bool ChangeState(int stateIndex)
    {
        throw new NotImplementedException();
    }

    public void ResetState()
    {
        throw new NotImplementedException();
    }
}
