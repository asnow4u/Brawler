using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.InputSystem.Utilities;

public abstract class SceneObject : MonoBehaviour, IDamage
{
    const float minForce = 200f;

    public Rigidbody rb;
    public bool isGrounded;

    public IActionState stateHandler;
    public IAnimator animator;    
    public EquipmentHandler equipmentHandler;

    protected IMovement movementHandler;
    protected IAttack attackHandler;

    [SerializeField] protected float damageTaken;

    private void Start()
    {
        Initialize();
    }
    

    protected virtual void Initialize()
    {               
        rb = GetComponent<Rigidbody>();

        InitializeEquipmentHandler();
        InitializeAnimator();
        InitializeStateHandler();
        InitializeMovementHandler();
        InitializeAttackHandler();
    }


    private void InitializeEquipmentHandler()
    {
        equipmentHandler = new EquipmentHandler(this);
    }

    private void InitializeAnimator()
    {
        animator = GetComponentInChildren<IAnimator>();
        animator.SetUp(this);
    }


    private void InitializeStateHandler()
    {
        stateHandler = new ActionStateHandler();
        stateHandler.Setup(this);
        stateHandler.ResetState();
    }


    private void InitializeMovementHandler()
    {
        if (TryGetComponent(out IMovement handler)) 
        {
            movementHandler = handler;
            movementHandler.Setup(this);
        }
    }

    private void InitializeAttackHandler()
    {
        if (TryGetComponent(out IAttack handler))
        {
            attackHandler = handler;
            attackHandler.Setup(this);
        }
    }



    //TODO: make two rays on each side to prevent landing just on the edge and not getting jump reset
    protected void FixedUpdate()
    {
        if (Physics.Raycast(GetComponent<Collider>().bounds.center, Vector3.down, transform.GetComponent<Collider>().bounds.size.y / 2 + 0.1f, ~LayerMask.NameToLayer("Environment")))
        {
            if (rb.velocity.y < 0 && !isGrounded)
            {
                isGrounded = true;
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


    public Vector2 testDirection;
    public float testForce;

    [ContextMenu("TestForce")]
    public void TestForce()
    {
        rb.AddForce(new Vector3(testDirection.x, testDirection.y, 0) * testForce, ForceMode.Impulse);
    }


    /// <summary>
    /// The equation is used to calculate the force required to knock a target object a certain distance, given its mass and the amount of damage dealt to it. 
    /// The idea is that the more mass the target has, the more force is required to knock it the same distance compared to an object with less mass. 
    /// Similarly, the more total damage is dealt to the target, the more force is required to knock it the same distance.
    /// The equation breaks down into three main parts. 
    /// The first part, knockbackForce, calculates the scaling factor for knockback based on the target's mass. 
    /// The second part, damageForce, calculates the scaling factor for damage based on the total damage dealt to the target. The e^(Min(1, damageTaken / 100f) * Ln(10) ensures that the scaling is smooth and gradual, without sudden jumps in force values.
    /// The third part, totalForce, multiplies the two scaling factors together to get the final force value required to achieve the desired knockback. The 1000 helps get to the proper range of force.
    /// </summary>
    /// <param name="baseKnockBack"></param>
    /// <param name="forceDirection"></param>
    public void ApplyForceBasedOnDamage(float baseKnockBack, float damageInfluence, Vector2 forceDirection)
    {
        float knockBackForce = (baseKnockBack / rb.mass);

        //TODO: Want to include an influence variable. this would determine how much the damage force would be applied to the total force
        float damageForce = (Mathf.Exp(Mathf.Min(1, damageTaken / 100f) * Mathf.Log(10)) * Mathf.Sqrt(damageTaken)) * Mathf.Clamp(damageInfluence, 0, 1);        
        
        float totalForce = Mathf.Max(minForce, knockBackForce * damageForce * 10f);
        
        Debug.Log("Force: " + totalForce);        

        rb.AddForce(new Vector3(forceDirection.x, forceDirection.y, 0) * totalForce, ForceMode.Impulse);
    }
}

