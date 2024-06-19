using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum HitStunState { None, StartUp, Base, Ending}


public class DamageHandler : MonoBehaviour, ITakeDamage
{
    [Header("Damage")]
    [SerializeField] protected float damageTaken;

    [Tooltip("How many frames pass before this sceneobject can be hit again")]
    [SerializeField] private float hitPreventionFrameCount;
    private float hitPreventionFrameTimer;    

    [Header("Knockback")]
    private KnockbackCalculator knockbackHandler;    

    [Header("HitStun")]
    [SerializeField] private HitStunState hitStunState;
    private float hitStunTimer;

    [Header("RagDoll")]
    [SerializeField] private GameObject boneRoot;    
    private Ragdoll ragdoll;


    //KillZones
    private KillZone[] killZones;


    //Getters
    public bool IsHitable => hitPreventionFrameTimer > 0;
    private SceneObject sceneObject => GetComponent<SceneObject>();
    private Collider collider => GetComponent<Collider>();





    //[SerializeField] private float bounceDegrade = 0.9f;
    ////TODO: Bounce timer should be based on damage(more damage = more emphisis on bounce)
    //[SerializeField] private float bounceFrameTimer;
    //private Vector3 predictedBounceVelocity;

    


    #region Initialize

    public void Initialize()
    {
        knockbackHandler = new KnockbackCalculator();

        if (boneRoot != null)
        {
            ragdoll = boneRoot.AddComponent<Ragdoll>();
            ragdoll.Initialize(sceneObject);            
        }        
    }

    #endregion


    #region Update

    public void HandleUpdate()
    {
        HitCoolDownUpdate();
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
    /// Hit by an attack, apply damage and force
    /// </summary>
    /// <param name="attackType"></param>
    /// <param name="attackDamage"></param>
    /// <param name="launchAngle"></param>
    public void HitByAttack(AttackColliderType attackType, Rigidbody hitRb, float attackDamage, float launchAngle)
    {
        hitPreventionFrameTimer = hitPreventionFrameCount;

        //Damage
        AddDamage(attackDamage);

        //Launch knockback
        Vector3 launchForce = knockbackHandler.CalculateForceKnockBack(attackType, damageTaken, sceneObject.Rb.mass, launchAngle);
        //launchForce = CheckForImmediateBounce(launchForce);
     
        //ragdollRoot.GetComponent<Rigidbody>().AddForce(launchForce, ForceMode.Impulse);
        hitRb.AddForce(launchForce, ForceMode.Impulse);

        //HitStun
        StartHitStun(launchForce.magnitude);
    }


    /// <summary>
    /// Provide a time in which the sceneObject cant be hit
    /// </summary>
    /// <returns></returns>
    private void HitCoolDownUpdate()
    {
        if (hitPreventionFrameTimer > 0)
            hitPreventionFrameCount--;
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
    private void StartHitStun(float launchForce)
    {    
        hitStunState = HitStunState.StartUp;

        //TODO: This does not incorperate different weapons yet
        sceneObject.AnimationStateHandler.PlayAnimation(new AnimationStateData(gameObject.name + "BaseHit", ActionState.HitStun, null));

        SetUpKillZone();

        hitStunTimer = launchForce / 500;
    }


    private void EndHitStun()
    {
        hitStunState = HitStunState.None;
        sceneObject.AnimationStateHandler.EndCurrentAnimation(ActionState.Admin);
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
                case HitStunState.StartUp:

                    //TODO: IDEA: Check if hit animation is finished (should go to idle). once finished start ragdoll 
                    // Want to see how this feels vs just enabling ragdoll strait up
                    EnableRagdoll();
                    hitStunState = HitStunState.Base;

                    break;

                case HitStunState.Base:

                    if (hitStunTimer < ragdoll.ExitTransitionTime)
                        hitStunState = HitStunState.Ending;

                    break;

                case HitStunState.Ending:

                    DisableRagdoll();
                    break;
            }
              

            //Update Timer
            hitStunTimer -= Time.deltaTime;

            if (hitStunTimer <= 0)
            {
                EndHitStun();                
            }
        }
    }

  
    //NOTE: Want to restructure how the ragdoll is created and manipulated!!!


    //private void PopulateRagdollTransition()
    //{
    //    foreach (RagdollPart part in ragdoll.RagdollParts)
    //        ragdollStartTransition.Add(new RagdollBone(part.Transform.position, part.Transform.rotation));                    

    //    foreach (AnimationClip clip in sceneObject.AnimationStateHandler.Animator.runtimeAnimatorController.animationClips)
    //    {
    //        if (sceneObject.GroundedState == GroundedState.Airborn && clip.name == gameObject.name + "BaseAirIdle")
    //        {
    //            clip.SampleAnimation(gameObject, 0);
    //            break;
    //        }
                
    //        else if (clip.name == gameObject.name + "BaseIdle")
    //        {
    //            clip.SampleAnimation(gameObject, 0);
    //            break;
    //        }
    //    }

    //    foreach (RagdollPart part in ragdoll.RagdollParts)
    //        ragdollEndTransition.Add(new RagdollBone(part.Transform.position, part.Transform.rotation);

        
    //    foreach (RagdollPart part in ragdoll.RagdollParts)
    //    {
            
    //    }
        


    //}



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
        Vector3 currentHipPos = boneRoot.transform.position;

        //Move transform to ragdoll root
        transform.position = boneRoot.transform.position - ragdoll.PosOffset;

        boneRoot.transform.position = currentHipPos;
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
