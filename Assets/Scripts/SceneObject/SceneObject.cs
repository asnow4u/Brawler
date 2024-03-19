using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.InputSystem.Utilities;

public enum SceneObjectType { Player, Enemy, Object }

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(MovementInputHandler))]
[RequireComponent(typeof(AttackInputHandler))]
public abstract class SceneObject : MonoBehaviour, IDamage
{
    [Header("SceneObject")]
    public string UniqueId;
    public SceneObjectType ObjectType;

    //Physics
    public Rigidbody rb => GetComponent<Rigidbody>();

    [Header("Hit/Damage")]
    public bool InHitStun;
    [SerializeField] protected float damageTaken;
    private float maxHitVelocity = 10f;    
    private float hitDecelerationRate = 2;
    const float minKnockBackForce = 200f;
    private KillZone killZone;
    private Coroutine hitStunTimer;

    //Handlers
    public IActionState StateHandler;
    public IEquipment EquipmentHandler;
    public IInteraction InteractionHandler;
    public MovementInputHandler MovementInputHandler => GetComponent<MovementInputHandler>();
    public AttackInputHandler AttackInputHandler => GetComponent<AttackInputHandler>();
    public IAnimator AnimationHandler => GetComponentInChildren<IAnimator>();

    #region Initialize

    private void Start()
    {
        Initialize();
    }
    

    protected virtual void Initialize()
    {               
        UniqueId = Guid.NewGuid().ToString();        

        InitializeInteractionHandler();
        InitializeEquipmentHandler();
        InitializeAnimationHandler();
        InitializeStateHandler();
        InitializeMovementHandler();
        InitializeAttackHandler();
    }


    private void InitializeInteractionHandler()
    {
        InteractionHandler = new InteractionHandler();
    }

    private void InitializeEquipmentHandler()
    {
        //TODO: Rework
        //EquipmentHandler = new EquipmentHandler(this);
    }

    private void InitializeAnimationHandler()
    {
        AnimationHandler.SetUp(this);
    }


    private void InitializeStateHandler()
    {
        StateHandler = new ActionStateHandler();
        StateHandler.Setup(this);
        StateHandler.ResetState();
    }


    private void InitializeMovementHandler()
    {         
        MovementInputHandler.Setup();        
    }


    private void InitializeAttackHandler()
    {        
        AttackInputHandler.Setup();        
    }

    #endregion


    //TODO: make two rays on each side to prevent landing just on the edge and not getting jump reset
    protected virtual void FixedUpdate()
    {
        
    }

    #region Direction

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

    #endregion


    #region Damage


    [Header("Force Test")]
    public float testForce;
    public Vector2 testDirection;

    [ContextMenu("TestForce")]
    public void TestForce()
    {
        ApplyForceBasedOnDamage(testForce, 0f, testDirection);
    }


    public void ResetDamage()
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
        ////TODO: Remove
        //InHitStun = true;

        //StateHandler.ChangeState(ActionState.State.HitStun);
        //float timer = totalForce / 1000f;

        //Debug.Log("HitStun: " + timer);

        //while (timer > 0)
        //{
        //    timer -= Time.deltaTime;
        //    yield return null;
        //}

        //Destroy(killZone.gameObject);

        //StateHandler.ResetState();

        ////TODO: Remove
        //InHitStun = false;

        //while (Mathf.Abs(rb.velocity.x) > maxHitVelocity || Mathf.Abs(rb.velocity.y) > maxHitVelocity)
        //{
        //    //Horizontal
        //    if (Mathf.Abs(rb.velocity.x) > maxHitVelocity)
        //    {
        //        //Right Direction
        //        if (rb.velocity.x > maxHitVelocity)
        //        {
        //            rb.velocity -= transform.right * hitDecelerationRate * Time.deltaTime;
        //        }

        //        //Left Direction
        //        else
        //        {
        //            rb.velocity += transform.right * hitDecelerationRate * Time.deltaTime;
        //        }
        //    }

        //    //Vertical
        //    if (Mathf.Abs(rb.velocity.y) > maxHitVelocity)
        //    {
        //        //Up
        //        if (rb.velocity.y > maxHitVelocity)
        //        {
        //            rb.velocity -= transform.up * hitDecelerationRate * Time.deltaTime;
        //        }

        //        //Down
        //        else
        //        {
        //            rb.velocity += transform.up * hitDecelerationRate * Time.deltaTime;
        //        }
        //    }

            yield return null;
        //}
    }

    #endregion

    private void OnDestroy()
    {
        if (killZone != null)
            Destroy(killZone.gameObject);
    }
}

