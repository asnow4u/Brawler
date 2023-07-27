using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.InputSystem.Utilities;

public abstract class SceneObject : MonoBehaviour, IDamage
{
    //Handlers
    public IActionState stateHandler;
    public IAnimator animator;
    public EquipmentHandler equipmentHandler;
    protected IMovement movementHandler;
    protected IAttack attackHandler;

    //Physics
    public Rigidbody rb;
    public bool isGrounded;
    public float maxVelocity = 10f;
    public float decelerationRate;

    //Damage
    const float minForce = 200f;
    [Range(1f, 10f)]
    public float forceScaler;
    [SerializeField] protected float damageTaken;

    //TODO: Remove
    public bool inHitStun;

    private Coroutine hitStunTimer;



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


    /// <summary>
    /// The equation is used to calculate the force that will be applied based on its mass and the amount of damage dealt to it, and the knockback of the attack. 
    /// The idea is that the more mass the target has, the more force is required to knock it the same distance compared to an object with less mass. 
    /// Similarly, the more total damage that is dealt to the target, will result in higher force values.
    /// Similarly, the more baseKnockBack being applied will result in higher force values.
    /// Lastly the damageInfluence helps determine how much influence the damage will have on the final force value
    /// The equation breaks down into three main parts.
    /// </summary>
    /// <param name="baseKnockBack"></param>
    /// <param name="forceDirection"></param>
    public void ApplyForceBasedOnDamage(float baseKnockBack, float damageInfluence, Vector2 forceDirection)
    {
        float damageForce = damageInfluence * forceScaler * Mathf.Pow((baseKnockBack * damageTaken) / Mathf.Pow(rb.mass, 1.75f), 2);

        float totalForce = Mathf.Max(minForce, baseKnockBack + damageForce);

        string damageDebug = "Damage: \n";
        damageDebug += "Knockback Force: " + baseKnockBack + "\n";
        damageDebug += "DamageForce: " + damageForce + "\n";
        damageDebug += "Total Force: " + totalForce;
        Debug.Log(damageDebug);        

        rb.AddForce(new Vector3(forceDirection.x, forceDirection.y, 0) * totalForce, ForceMode.Impulse);

        //Start hitstun coroutine
        if (hitStunTimer != null)
            StopCoroutine(hitStunTimer);

        hitStunTimer = StartCoroutine(ApplyHitStun(totalForce));       
    }


    public IEnumerator ApplyHitStun(float totalForce)
    {
        //TODO: Remove
        inHitStun = true;

        stateHandler.ChangeState(ActionState.State.HitStun);
        float timer = totalForce / 1000f;

        Debug.Log("HitStun: " + timer);

        while (timer > 0)
        {
            timer -= Time.deltaTime;
            yield return null;
        }

        stateHandler.ResetState();

        //TODO: Remove
        inHitStun = false;

        while (Mathf.Abs(rb.velocity.x) > maxVelocity || Mathf.Abs(rb.velocity.y) > maxVelocity)
        {
            //Horizontal
            if (Mathf.Abs(rb.velocity.x) > maxVelocity)
            {
                //Right Direction
                if (rb.velocity.x > maxVelocity)
                {
                    rb.velocity -= transform.right * decelerationRate * Time.deltaTime;
                }

                //Left Direction
                else
                {
                    rb.velocity += transform.right * decelerationRate * Time.deltaTime;
                }
            }
            
            //Vertical
            if (Mathf.Abs(rb.velocity.y) > maxVelocity)
            {
                //Up
                if (rb.velocity.y > maxVelocity)
                {
                    rb.velocity -= transform.up * decelerationRate * Time.deltaTime;
                }

                //Down
                else
                {
                    rb.velocity += transform.up * decelerationRate * Time.deltaTime;
                }
            }

            yield return null;
        }
    }



    [Header("Force Test")]
    public float testForce;
    public Vector2 testDirection;

    [ContextMenu("TestForce")]
    public void TestForce()
    {
        ApplyForceBasedOnDamage(testForce, 0f, testDirection);
    }
}

