using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.InputSystem.Utilities;
using System.Threading.Tasks;

public enum SceneObjectType { Player, Enemy, Object }
public enum GroundedState { Airborn, Grounded, Sliding }

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(MovementInputHandler))]
[RequireComponent(typeof(AttackInputHandler))]
[RequireComponent(typeof(AnimationStateHandler))]
public abstract class SceneObject : MonoBehaviour, ITakeDamage
{
    [Header("SceneObject")]
    public string UniqueId;
    public SceneObjectType ObjectType;

    [Header("Ground Status")]
    [SerializeField] private GroundedState curGroundedState;
    [SerializeField] private float maxSlopeAngle;

    [Header("Hit/Damage")]
    [SerializeField] protected float damageTaken;

    //Damage Handlers
    private KnockbackCalculator knockbackHandler;    


    //Hit Stun
    private Coroutine hitStunTimer;


    public bool InHitStun;
    private float maxHitVelocity = 10f;    
    private float hitDecelerationRate = 2;
    
    private KillZone killZone;

    //Handlers
    public IEquipment EquipmentHandler;
    public IInteraction InteractionHandler;

    //Getters
    public IAnimator AnimationStateHandler => GetComponentInChildren<IAnimator>();
    public MovementInputHandler MovementInputHandler => GetComponent<MovementInputHandler>();
    public AttackInputHandler AttackInputHandler => GetComponent<AttackInputHandler>();
    
    public GroundedState GroundedState => curGroundedState;   
    public Rigidbody Rb => GetComponent<Rigidbody>();
    private Collider collider => GetComponent<Collider>();


    //Events
    public event Action<GroundedState> GroundedStateChangeEvent;

    #region Initialize

    private void Start()
    {
        Initialize();
    }
    

    /// <summary>
    /// Create Unique ID
    /// Setup handlers
    /// </summary>
    protected virtual void Initialize()
    {               
        UniqueId = Guid.NewGuid().ToString();

        knockbackHandler = new KnockbackCalculator();

        InitializeInteractionHandler();
        InitializeEquipmentHandler();
        InitializeMovementHandler();
        InitializeAttackHandler();
        InitializeAnimationStateHandler();
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

    private void InitializeAnimationStateHandler()
    {
        AnimationStateHandler.SetUp();
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


    #region Fixed Update

    protected virtual void FixedUpdate()
    {  
        MovementInputHandler.UpdateMovement();
        
        PredictHitStunBounce();

        //Grounded Status
        CheckGroundedStatus();
    }

    #endregion


    #region Ground Status

    /// <summary>
    /// Use raycasts to determine current status of the ground
    /// </summary>
    private void CheckGroundedStatus()
    {
        if (TryGetSlopeAngle(out Vector3 slopeAngle))
        {
            float angle = Vector3.Angle(transform.right, slopeAngle);

            switch (curGroundedState)
            {
                case GroundedState.Grounded:

                    if (angle > maxSlopeAngle)
                    {
                        //Face direction of downward slope
                        //if (slopeAngle.y > 0)
                        //    TurnAround();

                        curGroundedState = GroundedState.Sliding;

                        GroundedStateChangeEvent?.Invoke(curGroundedState);
                    }
                    break;

                case GroundedState.Sliding:

                    if (angle < maxSlopeAngle)
                    {                     
                        curGroundedState = GroundedState.Grounded;

                        GroundedStateChangeEvent?.Invoke(curGroundedState);
                    }
                    break;

                case GroundedState.Airborn:

                    if (angle > maxSlopeAngle)
                        curGroundedState = GroundedState.Sliding;
                    else
                        curGroundedState = GroundedState.Grounded;

                    //End animation at or below attacking state
                    AnimationStateHandler.EndCurrentAnimation(ActionState.Attacking);

                    GroundedStateChangeEvent?.Invoke(curGroundedState);
                    
                    break;
            }
        }

        else
        {
            if (curGroundedState != GroundedState.Airborn)
            {
                curGroundedState = GroundedState.Airborn;

                //End animation at or below attacking state
                AnimationStateHandler.EndCurrentAnimation(ActionState.Attacking);

                GroundedStateChangeEvent?.Invoke(curGroundedState);
            }
        }
    }


    /// <summary>
    /// Attempt to get the current slope of the environment under the sceneObject
    /// Casts 10 rays based on the left/right most point of the collider
    /// </summary>
    /// <param name="slopeAngle"></param>
    /// <returns></returns>
    public bool TryGetSlopeAngle(out Vector3 slopeAngle)
    {
        List<RaycastHit> hits = new List<RaycastHit>();

        //Create raycasts
        Vector3 leftSidePoint = collider.bounds.center + Vector3.left * collider.bounds.extents.x;
        Vector3 rightSidePoint = collider.bounds.center + Vector3.right * collider.bounds.extents.x;
        float spaceBetweenRays = (rightSidePoint.x - leftSidePoint.x) / 10;

        //Raycast
        for (int i = 0; i < 10; i++)
        {
            Vector3 origin = leftSidePoint + Vector3.right * spaceBetweenRays * i;

            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, collider.bounds.extents.y + 0.3f, LayerMask.GetMask("Environment")))
            {
                hits.Add(hit);
            }
        }

        if (hits.Count > 0)
        {
            //Average normals
            Vector3 avgNormal = Vector3.zero;

            foreach (RaycastHit hit in hits)
            {
                avgNormal += hit.normal;
            }

            avgNormal /= 10;

            //Determine slope angle
            slopeAngle = Vector3.Cross(avgNormal, transform.forward).normalized;
            
            Debug.DrawRay(collider.bounds.center + Vector3.down * collider.bounds.extents.y, avgNormal, Color.green);
            Debug.DrawRay(collider.bounds.center + Vector3.down * collider.bounds.extents.y, slopeAngle, Color.red);

            return true;
        }

        slopeAngle = Vector3.zero;
        return false;
    }

    #endregion


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

    /// <summary>
    /// Add an amount of damage based on the provided percent <\br>
    /// </summary>
    /// <param name="percent"></param>
    public void AddDamage(float percent)
    {
        damageTaken += percent;
    }

    /// <summary>
    /// Remove an amount of damage based on the provided percent <\br>
    /// Cant drop below 0
    /// </summary>
    /// <param name="percent"></param>
    public void RemoveDamage(float percent)
    {
        damageTaken -= percent;

        if (damageTaken < 0)
            damageTaken = 0;
    }


    /// <summary>
    /// Reset any damage that was previously taken
    /// </summary>
    public void ResetDamage()
    {
        damageTaken = 0;
    }


    public void HitByAttack(AttackColliderType attackType, float attackDamage, float launchAngle)
    {
        Debug.LogWarning(gameObject.name + " Hit by attack " + launchAngle);        
       
        AddDamage(attackDamage);
        
        //TODO: Determine if force pushes into ground/wall, in which bounce should occure (Eventally should pass past player

        //Launch knockback
        Vector3 launchForce = knockbackHandler.CalculateForceKnockBack(attackType, damageTaken, Rb.mass, launchAngle);
        Rb.AddForce(launchForce, ForceMode.Impulse);

        Debug.DrawRay(transform.position, launchForce.normalized, Color.black);


        //HitStun
        SetHitStun(launchForce.magnitude);


        //KillZone
        //if (killZone != null)
        //    Destroy(killZone.gameObject);

        //killZone = KillZoneFactory.instance.Spawn(forceDirection.x > 0 ? true : false, false, this.UniqueId);
    }

    #endregion


    #region HitStun

    //TODO: Seperate into its own handler class
    //TODO: Bounce timer should be based on damage (more damage = more emphisis on bounce)

    public enum HitStunState { Movement, PredictedBounce, Bounce }

    [Header("HitStun")]
    [SerializeField] private HitStunState hitStunState;
    [SerializeField] private Vector3 bounceVelocity;
    [SerializeField] private float bounceDegrade = 0.9f;
    [SerializeField] private float bounceFrameTimer;

    private void SetHitStun(float launchForce)
    {   
        //TODO: This does not incorperate different weapons yet
        AnimationStateHandler.PlayAnimation(new AnimationStateData(gameObject.name + "BaseHit", ActionState.HitStun, null));

        if (hitStunTimer != null)
        {
            Debug.LogWarning("Combo");
            StopCoroutine(hitStunTimer);
        }

        hitStunTimer = StartCoroutine(HitStunTimer(launchForce / 1000));
    }


    public IEnumerator HitStunTimer(float timer)
    {
        while (timer > 0)
        {
            timer -= Time.deltaTime;
            yield return null;
        }

        AnimationStateHandler.EndCurrentAnimation(ActionState.Admin);

        hitStunTimer = null;
    }


    /// <summary>
    /// Looks ahead to help calculate a bounce
    /// </summary>
    private void PredictHitStunBounce()
    {
        if (AnimationStateHandler.CurActionState == ActionState.HitStun &&
            hitStunState == HitStunState.Movement)
        {
            float distance = Rb.velocity.magnitude * Time.fixedDeltaTime;
            Vector3 direction = Rb.velocity.normalized;

            RaycastHit[] hits = Rb.SweepTestAll(direction, distance);

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Environment"))
                {
                    Debug.Log("HitStun Perdicted", hit.collider.gameObject);
                    bounceVelocity = Vector3.Reflect(Rb.velocity, hit.normal) * bounceDegrade;
                    hitStunState = HitStunState.PredictedBounce;

                    break;
                }
            }            
        }
    }


    /// <summary>
    /// Used to slow down the bounce effect when a scene object hits a environment surface
    /// </summary>
    /// <returns></returns>
    private IEnumerator BounceTimer()
    {
        hitStunState = HitStunState.Bounce;
        Debug.Log("HitStun BounceTimer started");

        int frameCount = 0;

        while (frameCount < bounceFrameTimer)
        {
            frameCount++;
            yield return null;
        }

        Debug.Log("HitStun BounceTimer ended");

        Rb.velocity = bounceVelocity;

        hitStunState = HitStunState.Movement;
    }


    #endregion


    #region Collision

    private void OnCollisionEnter(Collision col)
    {
        //Environment
        if (col.gameObject.layer == LayerMask.NameToLayer("Environment"))
        {
            if (AnimationStateHandler.CurActionState == ActionState.HitStun)
            {
                Debug.Log("HitStun Collided");
                StartCoroutine(BounceTimer());
            }
        }
    }

    #endregion


    private void OnDestroy()
    {
        if (killZone != null)
            Destroy(killZone.gameObject);
    }    
}

