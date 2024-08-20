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

    [Header("Bounce")]
    [SerializeField] private float bounceDegrade = 0.9f;


    //KillZones
    private KillZone[] killZones;


    //Getters
    private SceneObject sceneObject => GetComponent<SceneObject>();
    private Collider collider => GetComponent<Collider>();


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
        Vector3 launchForce = knockbackHandler.CalculateForceKnockBack(influence, damageTaken, launchAngle, sceneObject.Rb);
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
            sceneObject.ActionStateHandler.ChangeState(ActionState.HitStun);

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

                    //Bounce
                    PerdictHitStunBounce();

                    //Ragdoll
                    if (ragdoll != null)
                    {
                        if (!ragdoll.enabled && sceneObject.CoreRigidBody.velocity.magnitude > 10)
                            EnableRagdoll();

                        if (hitStunTimer < ragdoll.ExitTransitionTime)
                            DisableRagdoll();
                    }

                    //Timer
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

    
    /// <summary>
    /// Looks ahead to help calculate a bounce
    /// </summary>
    public void PerdictHitStunBounce()
    {
        if (ragdoll != null && ragdoll.enabled)
            ragdoll.CheckRagdollBounce(collider.bounds, bounceDegrade);

        else
            CheckCoreBounce(collider.bounds);        
    }


    /// <summary>
    /// Check if a bounce is going to occure. Adjust velocity accordingly
    /// </summary>
    /// <param name="bounds"></param>
    private void CheckCoreBounce(Bounds bounds)
    {
        Vector3 velocity = sceneObject.CoreRigidBody.velocity;
        float distance = velocity.magnitude * Time.fixedDeltaTime;
        Vector3 direction = velocity.normalized;

        if (Physics.BoxCast(bounds.center, bounds.extents, direction, out RaycastHit hit, Quaternion.identity, distance, LayerMask.GetMask("Environment")))
        {
            Vector3 bounceVelocity = Vector3.Reflect(velocity, hit.normal) * bounceDegrade;
            sceneObject.CoreRigidBody.velocity = bounceVelocity;
        }
    }

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
