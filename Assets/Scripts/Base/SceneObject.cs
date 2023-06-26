using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody))]
public abstract class SceneObject : MonoBehaviour, IActionState, IDamage
{
    //public ActionRouter router;
    public Rigidbody rb;
    public bool isGrounded;

    public IAnimator animator;
    
    protected IMovement movementHandler;
    protected IAttack attackHandler;

    protected EquipmentHandler equipmentHandler;

    protected float damageTaken;

    private void Start()
    {
        Initialize();
    }
    

    protected virtual void Initialize()
    {               
        rb = GetComponent<Rigidbody>();
        equipmentHandler = new EquipmentHandler(this);        

        InitializeAnimator();
        InitializeMovementHandler();
        InitializeAttackHandler();

        equipmentHandler.SwapWeapon(0);
    }


    private void InitializeAnimator()
    {
        animator = GetComponentInChildren<IAnimator>();
        animator.SetUp(this);
    }


    private void InitializeMovementHandler()
    {
        if (TryGetComponent(out IMovement handler)) 
        {
            movementHandler = handler;
            movementHandler.Setup(this, equipmentHandler);
        }
    }

    private void InitializeAttackHandler()
    {
        if (TryGetComponent(out IAttack handler))
        {
            attackHandler = handler;
            attackHandler.Setup(this, equipmentHandler);

        }
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
