using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum HitStunState { None, StartUp, Base, Ending}


public class DamageHandler : MonoBehaviour, ITakeDamage
{
    [Header("Damage")]
    [SerializeField] protected float damageTaken; 

    [Header("Knockback")]
    private KnockbackCalculator knockbackHandler;

    [Header("HitStun")]
    [SerializeField] private HitStunState hitStunState;
    [SerializeField] private float hitStunTimer;

    [Header("RagDoll")]
    [SerializeField] private GameObject ragdollRoot;    
    private Ragdoll ragdoll;


    //KillZones
    private KillZone[] killZones;


    //Getters
    private SceneObject sceneObject => GetComponent<SceneObject>();
    private Collider collider => GetComponent<Collider>();





    //[SerializeField] private float bounceDegrade = 0.9f;
    ////TODO: Bounce timer should be based on damage(more damage = more emphisis on bounce)
    //[SerializeField] private float bounceFrameTimer;
    //private Vector3 predictedBounceVelocity;

    


    #region Initialize

    public void Initialize()
    {
        //SetUpEvents();

        knockbackHandler = new KnockbackCalculator();

        if (ragdollRoot != null)
        {
            ragdoll = ragdollRoot.AddComponent<Ragdoll>();
            ragdoll.Initialize(sceneObject);            
        }        
    }

    #endregion


    #region Events

    //private void SetUpEvents()
    //{
    //    sceneObject.AnimationStateHandler.OnAnimationUpdateEvent += OnAnimationUpdated;
    //}


    //private void OnAnimationUpdated(string animation, AnimationTrigger.Type triggerType)
    //{
    //    if (animation.Contains("Hit"))
    //    {
    //        if (triggerType == AnimationTrigger.Type.Start)
    //        {
    //            hitStunState = HitStunState.StartUp;
    //        }

    //        else if (triggerType == AnimationTrigger.Type.End)
    //        {
    //            Debug.Log("HITSTUN Animation ended");
    //            EnableRagdoll();
    //            hitStunState = HitStunState.Base;
    //        }
    //    }
    //}

    #endregion


    #region Update

    public void HandleUpdate()
    {
        HitStunStateUpdate();
        RagdollUpdate();
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

        sceneObject.UIHandler.UpdateDamageDisplay(damageTaken);        
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


    /// <summary>
    /// Hit by an attack, apply damage and store force
    /// </summary>
    /// <param name="attackType"></param>
    /// <param name="attackDamage"></param>
    /// <param name="launchAngle"></param>
    public void HitByAttack(float influence, Vector3 attackPoint, float attackDamage, float launchAngle)
    {                
        //Damage bubble                    
        UIFactory.Instance.SpawnDamageBubble(attackPoint, attackDamage);

        //Damage
        AddDamage(attackDamage);

        //Launch knockback
        Vector3 launchForce = knockbackHandler.CalculateForceKnockBack(influence, damageTaken, sceneObject.Rb.mass, launchAngle);
        ApplyLaunchForce(launchForce);

        //HitStun
        ApplyHitStun(launchForce.magnitude);                        
    }  


    /// <summary>
    /// Apply the stored force
    /// </summary>
    private void ApplyLaunchForce(Vector3 launchForce)
    {        
        if (ragdoll != null && ragdoll.enabled)
        {
            //Set mass to coreRigidbody to get similar force effect
            float mass = ragdoll.RB.mass;
            ragdoll.RB.mass = sceneObject.CoreRigidBody.mass;

            ragdoll.RB.AddForce(launchForce, ForceMode.Impulse);

            ragdoll.RB.mass = mass;
        }

        else
            sceneObject.CoreRigidBody.AddForce(launchForce, ForceMode.Impulse);
    }

    //private Vector3 CheckForImmediateBounce(Vector3 initialForce)
    //{
    //    //Check for bounce
    //    if (initialForce.x > 0 &&
    //        Physics.Raycast(collider.bounds.center, transform.right, collider.bounds.extents.x + 0.01f, LayerMask.GetMask("Environment")))
    //    {
    //        Debug.Log("LaunchForce Bounce on right side");
    //        initialForce *= bounceDegrade;
    //        initialForce.x *= -1;
    //    }

    //    if (initialForce.x < 0 &&
    //        Physics.Raycast(collider.bounds.center, -transform.right, collider.bounds.extents.x + 0.01f, LayerMask.GetMask("Environment")))
    //    {
    //        Debug.Log("LaunchForce Bounce on left side");
    //        initialForce *= bounceDegrade;
    //        initialForce.x *= -1;
    //    }

    //    if (initialForce.y > 0 &&
    //        Physics.Raycast(collider.bounds.center, transform.up, collider.bounds.extents.y + 0.01f, LayerMask.GetMask("Environment")))
    //    {
    //        Debug.Log("LaunchForce Bounce on top side");
    //        initialForce *= bounceDegrade;
    //        initialForce.y *= -1;
    //    }

    //    if (initialForce.y < 0 &&
    //        Physics.Raycast(collider.bounds.center, -transform.up, collider.bounds.extents.y + 0.01f, LayerMask.GetMask("Environment")))
    //    {
    //        Debug.Log("LaunchForce Bounce on bottom side");
    //        initialForce *= bounceDegrade;
    //        initialForce.y *= -1;
    //    }


    //    return initialForce;
    //}


    #endregion


    #region HitStun

    /// <summary>
    /// Change ActionState and start timer
    /// </summary>
    /// <param name="launchForce"></param>
    private void ApplyHitStun(float launchForceMagnitude)
    {    
        if (hitStunState == HitStunState.None)
        {
            //TODO: This does not incorperate different weapons yet
            sceneObject.AnimationStateHandler.PlayAnimation(new AnimationStateData(gameObject.name + "BaseHit", ActionState.HitStun, null));

            SetUpKillZone();

            //TODO: Determine equation for hitstun time
            hitStunTimer = launchForceMagnitude / 1500;

            hitStunState = HitStunState.Base;
        }

        else
        {
            //TODO: Determine equation for hitstun time
            hitStunTimer = launchForceMagnitude / 1500;
        }
    }


    private void EndHitStun()
    {
        hitStunState = HitStunState.None;       
        DestroyKillZones();
    }
    

    /// <summary>
    /// Updates everything based on the state of hitstun
    /// </summary>
    private void HitStunStateUpdate()
    {
        if (hitStunState != HitStunState.None)
        {
            switch (hitStunState)
            {
                case HitStunState.Base:

                    if (ragdoll != null)
                    {
                        if (!ragdoll.enabled && sceneObject.CoreRigidBody.velocity.magnitude > 10)
                            EnableRagdoll();

                        if (hitStunTimer < ragdoll.ExitTransitionTime)
                            DisableRagdoll();
                    }

                    if (hitStunTimer < 0)
                        hitStunState = HitStunState.Ending;                    

                    break;

                case HitStunState.Ending:
                    
                    EndHitStun();                
                    break;
            }             

            hitStunTimer -= Time.deltaTime;
        }
    }


    //TODO: Need to fix for ragdoll
    /// <summary>
    /// Looks ahead to help calculate a bounce
    /// </summary>
    public void PredictHitStunBounce()
    {
        //if (sceneObject.AnimationStateHandler.CurActionState == ActionState.HitStun &&
        //    hitStunState == HitStunState.Movement)
        //{
        //    float distance = rb.velocity.magnitude * Time.fixedDeltaTime;
        //    Vector3 direction = rb.velocity.normalized;

        //    RaycastHit[] hits = rb.SweepTestAll(direction, distance);

        //    foreach (RaycastHit hit in hits)
        //    {
        //        if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Environment"))
        //        {
        //            Debug.Log("HitStun Perdicted", hit.collider.gameObject);
        //            predictedBounceVelocity = Vector3.Reflect(rb.velocity, hit.normal) * bounceDegrade;
        //            hitStunState = HitStunState.PredictedBounce;

        //            break;
        //        }
        //    }
        //}
    }

    
    /// <summary>
    /// Used to slow down the bounce effect when a scene object hits a environment surface
    /// </summary>
    /// <returns></returns>
    //private IEnumerator BounceTimer()
    //{
        //hitStunState = HitStunState.Bounce;
        //Debug.Log("HitStun BounceTimer started");

        //int frameCount = 0;

        //while (frameCount <= bounceFrameTimer)
        //{
        //    frameCount++;
        //    yield return null;
        //}

        //Debug.Log("HitStun BounceTimer ended");

        //rb.velocity = predictedBounceVelocity;

        //hitStunState = HitStunState.Movement;
    //}

    #endregion


    #region Collision

    //private void OnCollisionEnter(Collision col)
    //{
    //    //Environment
    //    if (col.gameObject.layer == LayerMask.NameToLayer("Environment"))
    //    {
    //        if (sceneObject.AnimationStateHandler.CurActionState == ActionState.HitStun)
    //        {
    //            Debug.Log("HitStun Collided");
    //            StartCoroutine(BounceTimer());
    //        }
    //    }
    //}

    #endregion


    #region Ragdoll
       
    private void EnableRagdoll()
    {        
        if (ragdoll != null)
        {
            if (!ragdoll.enabled)
                ragdoll.enabled = true;            
        }
    }

    
    private void DisableRagdoll()
    {
        if (ragdoll != null)
        {            
            if (ragdoll.enabled)
                ragdoll.enabled = false;          
        }
    }


    private void RagdollUpdate()
    {
        if (ragdoll != null)
        {
            if (ragdoll.enabled)
                RepositionToRagDoll();
        }
    }


    /// <summary>
    /// Position sceneObject to realign with moving ragdoll
    /// </summary>
    private void RepositionToRagDoll()
    {
        Vector3 currentHipPos = ragdollRoot.transform.position;

        //Move transform to ragdoll root
        transform.position = ragdollRoot.transform.position - ragdoll.PosOffset;

        ragdollRoot.transform.position = currentHipPos;
    }

    #endregion


    #region KillZone

    /// <summary>
    /// Spawn killzones
    /// </summary>
    private void SetUpKillZone()
    {
        if (killZones == null)
        {
            killZones = KillZoneFactory.instance.SpawnGhostKillZones(sceneObject.UniqueId);            
        }
    }


    /// <summary>
    /// Destroy current killzones
    /// </summary>
    private void DestroyKillZones()
    {
        if (killZones != null)
        {
            foreach (KillZone killZone in killZones)
            {
                Destroy(killZone.gameObject);
            }
        }

        killZones = null;
    }

    #endregion

    private void OnDestroy()
    {
        DestroyKillZones();   
    }
}
