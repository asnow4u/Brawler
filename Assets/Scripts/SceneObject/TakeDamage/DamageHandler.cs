using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum HitStunState { Movement, PredictedBounce, Bounce }

public class DamageHandler : MonoBehaviour, ITakeDamage
{
    [Header("Damage")]
    [SerializeField] protected float damageTaken;

    [Header("HitStun")]
    [SerializeField] private HitStunState hitStunState;
    [SerializeField] private float bounceDegrade = 0.9f;
    //TODO: Bounce timer should be based on damage(more damage = more emphisis on bounce)
    [SerializeField] private float bounceFrameTimer;

    private KnockbackCalculator knockbackHandler;
    private Coroutine hitStunTimer;

    private Vector3 predictedBounceVelocity;

    private KillZone killZone;

    //Getters
    private SceneObject sceneObject => GetComponent<SceneObject>();
    private Rigidbody rb => GetComponent<Rigidbody>();
    private Collider collider => GetComponent<Collider>();


    public void Initialize()
    {
        knockbackHandler = new KnockbackCalculator();
    }


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


    /// <summary>
    /// Hit by an attack, apply damage and force
    /// </summary>
    /// <param name="attackType"></param>
    /// <param name="attackDamage"></param>
    /// <param name="launchAngle"></param>
    public void HitByAttack(AttackColliderType attackType, float attackDamage, float launchAngle)
    {        
        AddDamage(attackDamage);

        //Launch knockback
        Vector3 launchForce = knockbackHandler.CalculateForceKnockBack(attackType, damageTaken, rb.mass, launchAngle);

        launchForce = CheckForImmediateBounce(launchForce);

        rb.AddForce(launchForce, ForceMode.Impulse);

        Debug.DrawRay(transform.position, launchForce.normalized, Color.black, 2f);

        //HitStun
        SetHitStun(launchForce.magnitude);

        SetUpKillZone();        
    }


    private Vector3 CheckForImmediateBounce(Vector3 initialForce)
    {
        //Check for bounce
        if (initialForce.x > 0 &&
            Physics.Raycast(collider.bounds.center, transform.right, collider.bounds.extents.x + 0.01f, LayerMask.GetMask("Environment")))
        {
            Debug.Log("LaunchForce Bounce on right side");
            initialForce *= bounceDegrade;
            initialForce.x *= -1;
        }

        if (initialForce.x < 0 &&
            Physics.Raycast(collider.bounds.center, -transform.right, collider.bounds.extents.x + 0.01f, LayerMask.GetMask("Environment")))
        {
            Debug.Log("LaunchForce Bounce on left side");
            initialForce *= bounceDegrade;
            initialForce.x *= -1;
        }

        if (initialForce.y > 0 &&
            Physics.Raycast(collider.bounds.center, transform.up, collider.bounds.extents.y + 0.01f, LayerMask.GetMask("Environment")))
        {
            Debug.Log("LaunchForce Bounce on top side");
            initialForce *= bounceDegrade;
            initialForce.y *= -1;
        }

        if (initialForce.y < 0 &&
            Physics.Raycast(collider.bounds.center, -transform.up, collider.bounds.extents.y + 0.01f, LayerMask.GetMask("Environment")))
        {
            Debug.Log("LaunchForce Bounce on bottom side");
            initialForce *= bounceDegrade;
            initialForce.y *= -1;
        }


        return initialForce;
    }


    #endregion


    #region HitStun
    
    /// <summary>
    /// Change ActionState and start timer
    /// </summary>
    /// <param name="launchForce"></param>
    private void SetHitStun(float launchForce)
    {
        //TODO: This does not incorperate different weapons yet
        sceneObject.AnimationStateHandler.PlayAnimation(new AnimationStateData(gameObject.name + "BaseHit", ActionState.HitStun, null));

        if (hitStunTimer != null)
        {
            Debug.LogWarning("Combo");
            StopCoroutine(hitStunTimer);
        }

        hitStunTimer = StartCoroutine(HitStunTimer(launchForce / 1000));
    }


    /// <summary>
    /// Timer for hitStun
    /// </summary>
    /// <param name="timer"></param>
    /// <returns></returns>
    private IEnumerator HitStunTimer(float timer)
    {
        while (timer > 0)
        {
            timer -= Time.deltaTime;
            yield return null;
        }

        sceneObject.AnimationStateHandler.EndCurrentAnimation(ActionState.Admin);

        hitStunTimer = null;
    }


    /// <summary>
    /// Looks ahead to help calculate a bounce
    /// </summary>
    public void PredictHitStunBounce()
    {
        if (sceneObject.AnimationStateHandler.CurActionState == ActionState.HitStun &&
            hitStunState == HitStunState.Movement)
        {
            float distance = rb.velocity.magnitude * Time.fixedDeltaTime;
            Vector3 direction = rb.velocity.normalized;

            RaycastHit[] hits = rb.SweepTestAll(direction, distance);

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Environment"))
                {
                    Debug.Log("HitStun Perdicted", hit.collider.gameObject);
                    predictedBounceVelocity = Vector3.Reflect(rb.velocity, hit.normal) * bounceDegrade;
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

        rb.velocity = predictedBounceVelocity;

        hitStunState = HitStunState.Movement;
    }

    #endregion


    #region Collision

    private void OnCollisionEnter(Collision col)
    {
        //Environment
        if (col.gameObject.layer == LayerMask.NameToLayer("Environment"))
        {
            if (sceneObject.AnimationStateHandler.CurActionState == ActionState.HitStun)
            {
                Debug.Log("HitStun Collided");
                StartCoroutine(BounceTimer());
            }
        }
    }

    #endregion


    #region KillZone

    private void SetUpKillZone()
    {
        if (killZone != null)
            Destroy(killZone.gameObject);

        //TODO: Spawn both killzones (bounce makes this less predictable)
        //killZone = KillZoneFactory.instance.Spawn(forceDirection.x > 0 ? true : false, false, sceneObject.UniqueId);
    }


    private void OnDestroy()
    {
        if (killZone != null)
            Destroy(killZone.gameObject);
    }

    #endregion
}
