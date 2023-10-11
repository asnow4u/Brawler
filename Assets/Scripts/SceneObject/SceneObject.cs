using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.InputSystem.Utilities;

[RequireComponent(typeof(Rigidbody))]
public abstract class SceneObject : MonoBehaviour, IDamage
{
    [Header("SceneObject")]
    public string UniqueId;
    public SceneObjectType.Type ObjectType;
    [SerializeField] protected float damageTaken;

    [Header("Physics")]
    public Rigidbody rb;
    public float maxVelocity = 10f;
    public bool isGrounded;
    public bool inHitStun;
    public float DecelerationRate = 2;

    [Header("Componenets")]
    [SerializeField] private bool ApplyMovement;
    [SerializeField] private bool ApplyAttacking;

    //Handlers
    public IActionState stateHandler;
    public IAnimator animator;
    public IEquipment equipmentHandler;
    public IInteraction interactionHandler;
    protected IMovement movementHandler;
    protected IAttack attackHandler;
    
    //Damage Calculation
   
    const float minKnockBackForce = 200f;
    
    private KillZone killZone;
    private Coroutine hitStunTimer;



    private void Start()
    {
        Initialize();
    }
    

    protected virtual void Initialize()
    {               
        UniqueId = Guid.NewGuid().ToString();

        rb = GetComponent<Rigidbody>();

        InitializeInteractionHandler();
        InitializeEquipmentHandler();
        InitializeAnimator();
        InitializeStateHandler();
        InitializeMovementHandler();
        InitializeAttackHandler();
    }


    private void InitializeInteractionHandler()
    {
        interactionHandler = new InteractionHandler();
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
        if (ApplyMovement) 
        {
            movementHandler = gameObject.AddComponent<MovementInputHandler>();
            movementHandler.Setup(this);
        }
    }

    private void InitializeAttackHandler()
    {
        if (ApplyAttacking)
        {
            attackHandler = gameObject.AddComponent<AttackInputHandler>();
            attackHandler.Setup(this);
        }
    }



    //TODO: make two rays on each side to prevent landing just on the edge and not getting jump reset
    protected virtual void FixedUpdate()
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
        float damageForce = damageInfluence * Mathf.Pow((baseKnockBack * damageTaken) / Mathf.Pow(rb.mass, 1.75f), 2);
        float totalForce = Mathf.Max(minKnockBackForce, baseKnockBack + damageForce);

        rb.AddForce(new Vector3(forceDirection.x, forceDirection.y, 0) * totalForce, ForceMode.Impulse);
       
        //Reset KillZone
        if (killZone != null)        
            Destroy(killZone.gameObject);

        killZone = KillZoneFactory.instance.Spawn(forceDirection.x > 0 ? true : false, false, this.UniqueId);

        //Start hitstun coroutine
        if (hitStunTimer != null)
        {
            StopCoroutine(hitStunTimer);
        }

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

        Destroy(killZone.gameObject);

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
                    rb.velocity -= transform.right * DecelerationRate * Time.deltaTime;
                }

                //Left Direction
                else
                {
                    rb.velocity += transform.right * DecelerationRate * Time.deltaTime;
                }
            }
            
            //Vertical
            if (Mathf.Abs(rb.velocity.y) > maxVelocity)
            {
                //Up
                if (rb.velocity.y > maxVelocity)
                {
                    rb.velocity -= transform.up * DecelerationRate * Time.deltaTime;
                }

                //Down
                else
                {
                    rb.velocity += transform.up * DecelerationRate * Time.deltaTime;
                }
            }

            yield return null;
        }
    }

    private void OnDestroy()
    {
        if (killZone != null)
            Destroy(killZone.gameObject);
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

