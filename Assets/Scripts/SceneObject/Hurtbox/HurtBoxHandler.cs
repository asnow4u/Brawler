using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ISceneObject))]
[RequireComponent(typeof(ActionStateHandler))]
[RequireComponent(typeof(StatHandler))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class HurtBoxHandler : MonoBehaviour, IHurtBoxHandler, IHurtBoxHandlerEditor
{
    //Dependecies
    private ISceneObject sceneObject;
    private IActionState actionState;
    private IStats statHandler;

    //Componenets
    private Rigidbody rb;
    private Collider physicalCollider;

    //Movement Data
    private MovementStatData curMovementData;

    //Hurt boxs
    private HurtBox[] hurtBoxes;

    private List<Guid> lastHitBy;
    public Guid[] LastHitBy => lastHitBy.ToArray();

    [Header("Damage")]
    [SerializeField] protected float damageTaken = 0;
    

    [Header("Knockback")]
    [Tooltip("Base knockback force applied to any hit (before mass division). Sets the minimum-feel velocity at 0% damage.")]
    [SerializeField] private float baseForce = 1200f;
    [Tooltip("Exponential growth of damage-scaled knockback. Lower = smoother curve, higher = sharper ramp at high damage.")]
    [SerializeField] private float exGrowth = 2.0f;
    [Tooltip("Multiplier on the damage-scaled portion. Controls how much added force comes from damage scaling.")]
    [SerializeField] private float damageForceScale = 0.2f;
    private Vector3 knockBackVelocity;

    [Header("Hit Stun")]
    [SerializeField] private HitStunState curHitStunState;
    public HitStunState CurHitStunState => curHitStunState;
    
        [Header("Launch")]
    [Tooltip("Base launch duration (seconds) - always applied regardless of knockback strength")]
    [SerializeField] private float baseLaunchDuration = 0.05f;
    [Tooltip("How much additional launch time is added per unit of knockback magnitude")]
    [SerializeField] private float launchKnockbackScale = 0.005f;
    [Tooltip("Maximum launch duration in seconds (soft cap)")]
    [SerializeField] private float maxLaunchDuration = 0.2f;

    [Header("Travel")]
    [Tooltip("Fraction of time-to-apex Travel will run for (0-1). Lower = Recovery starts sooner before apex.")]
    [Range(0f, 1f)]
    [SerializeField] private float apexApproach = 0.9f;
    [Tooltip("Minimum Travel duration in seconds (used for downward/horizontal hits or very weak hits)")]
    [SerializeField] private float minTravelDuration = 0.05f;

    [Header("Recovery")]
    [Tooltip("Recovery duration in seconds - the follow-up window before the defender regains control")]
    [SerializeField] private float recoveryDuration = 0.25f;
    private float hitStunTime = 0;
    private float hitStunDuration = 0;
    private Coroutine hitStunTimerCoroutine;

    public event Action<KnockBackHitData> OnHitEvent;
    public event Action<HitStunState> HitStunStateChangedEvent;

    private void Awake()
    {
        sceneObject = GetComponent<ISceneObject>();
        actionState = GetComponent<IActionState>();
        statHandler = GetComponent<IStats>();
        rb = GetComponent<Rigidbody>();
        physicalCollider = GetComponent<Collider>();
        lastHitBy = new List<Guid>();

        //Get hurtboxs
        hurtBoxes = GetComponentsInChildren<HurtBox>(true);
        if (hurtBoxes.Length == 0)
            Debug.LogWarning("HurtBoxHandler: No Hurtboxs where found on", gameObject);

        RegisterToEvents();
    }

    private void RegisterToEvents()
    {
        statHandler.MovementStatsChangedEvent += OnMovementStatsChanged;

        foreach (HurtBox hurtBox in hurtBoxes)
            hurtBox.OnHitEvent += OnHit;
    }

    private void OnDestroy()
    {
        UnRegisterFromEvents();
    }

    private void UnRegisterFromEvents()
    {
        statHandler.MovementStatsChangedEvent -= OnMovementStatsChanged;

        foreach (HurtBox hurtBox in hurtBoxes)
            hurtBox.OnHitEvent -= OnHit;
    }

    private void OnMovementStatsChanged(MovementStatData movementStats)
    {
        curMovementData = movementStats;
    }

    private void OnHit(HitData hitData)
    {
        if (hitData == null)
            return;
        
        if (hitData is SceneObjectHitData sceneObjectHitData)
            lastHitBy.Add(sceneObjectHitData.SceneObjectAttackerID);

        //Damage and Knockback
        damageTaken += hitData.Damage;
        knockBackVelocity = CalculateKnockbackVelocity(hitData.Influence, damageTaken, hitData.LauchAngle, rb.mass);

        OnHitEvent?.Invoke(new KnockBackHitData(knockBackVelocity, hitData));

        //Compute per-phase durations from the knockback and current gravity
        float launchDuration = CalculateLaunchDuration(knockBackVelocity.magnitude);
        float travelDuration = CalculateTravelDuration(knockBackVelocity.y);

        //Hitstun
        ApplyHitStun(hitData.StunTime, launchDuration, travelDuration, recoveryDuration);
    }

    /// <summary>
    /// Calculate knockback velocity from influence, damage, launch angle, and mass.
    /// Force = minKnockBackForce + influence * damage^exGrowth. Velocity = Force / mass.
    /// Mass divides once (F = ma), so heavier characters take less knockback from all hits.
    /// </summary>
    /// <summary>
    /// Calculate knockback velocity from influence, damage, launch angle, and mass.
    /// Force = baseForce + influence * damage^exGrowth * damageForceScale. Velocity = Force / mass.
    /// Mass divides once (F = ma), so heavier characters take less knockback from all hits.
    /// baseForce sets the minimum-feel velocity (every hit registers). damageForceScale + exGrowth shape the ramp.
    /// </summary>
    public Vector3 CalculateKnockbackVelocity(float influence, float totalDamage, float launchAngle, float mass)
    {
        float force = baseForce + influence * Mathf.Pow(totalDamage, exGrowth) * damageForceScale;
        float velocity = force / mass;

        float xLaunch = Mathf.Cos(launchAngle * Mathf.Deg2Rad);
        float yLaunch = Mathf.Sin(launchAngle * Mathf.Deg2Rad);
        Vector3 launchDirection = new Vector2(xLaunch, yLaunch);

        return launchDirection * velocity;
    }

    private float CalculateLaunchDuration(float knockbackMagnitude)
    {
        float duration = baseLaunchDuration + knockbackMagnitude * launchKnockbackScale;
        return Mathf.Min(duration, maxLaunchDuration);
    }

    private float CalculateTravelDuration(float knockbackVelocityY)
    {
        if (knockbackVelocityY <= 0f)
            return minTravelDuration;

        if (curMovementData == null)
            return minTravelDuration;

        float gravityForce = Mathf.Abs(curMovementData.GravityHitStunTravel);
        if (gravityForce <= 0f)
            return minTravelDuration;

        float timeToApex = knockbackVelocityY / gravityForce;
        float scaled = timeToApex * apexApproach;

        //Floor at minTravelDuration; never exceed true apex time
        float result = Mathf.Max(scaled, minTravelDuration);
        result = Mathf.Min(result, timeToApex);
        return result;
    }

    private void ChangeHitStunState(HitStunState newState)
    {
        if (newState == curHitStunState)
            return;

        if (newState == HitStunState.Null)
        {
            physicalCollider.enabled = true;
            actionState.ChangeState(ActionState.Idle);

            lastHitBy.Clear();
            knockBackVelocity = Vector3.zero;
            hitStunTime = 0;

            if (hitStunTimerCoroutine != null)
                StopCoroutine(hitStunTimerCoroutine);
        }
        else
        {
            physicalCollider.enabled = false;
            actionState.ChangeState(ActionState.HitStun);
        }

        curHitStunState = newState;
        HitStunStateChangedEvent?.Invoke(newState);
    }

    private void ApplyHitStun(float pauseDuration, float launchDuration, float travelDuration, float recoveryDuration)
    {
        if (hitStunTimerCoroutine != null)
            StopCoroutine(hitStunTimerCoroutine);

        hitStunTimerCoroutine = StartCoroutine(HitStunTimer(pauseDuration, launchDuration, travelDuration, recoveryDuration));
    }

    private IEnumerator HitStunTimer(float pauseDuration, float launchDuration, float travelDuration, float recoveryDuration)
    {
        hitStunTime = 0;
        hitStunDuration = pauseDuration + launchDuration + travelDuration + recoveryDuration;

        float launchStart = pauseDuration;
        float travelStart = launchStart + launchDuration;
        float recoveryStart = travelStart + travelDuration;

        ChangeHitStunState(HitStunState.Pause);

        while (hitStunTime < hitStunDuration)
        {
            hitStunTime += Time.deltaTime;

            if (hitStunTime >= launchStart && curHitStunState == HitStunState.Pause)
                ChangeHitStunState(HitStunState.Launch);
            else if (hitStunTime >= travelStart && curHitStunState == HitStunState.Launch)
                ChangeHitStunState(HitStunState.Travel);
            else if (hitStunTime >= recoveryStart && curHitStunState == HitStunState.Travel)
                ChangeHitStunState(HitStunState.Recovery);

            yield return null;
        }

        hitStunTime = 0;
        hitStunDuration = 0;
        ChangeHitStunState(HitStunState.Null);
    }


    #region Editor Debug

    [Header("Debug")]
    [SerializeField, HideInInspector] private bool debugMode;
    [SerializeField, HideInInspector] private float debugInfluence = 1f;
    [SerializeField, HideInInspector] private float debugLaunchAngle = 45f;
    [SerializeField, HideInInspector] private float debugDamage = 10f;
    [SerializeField, HideInInspector] private float debugDelaySeconds = 0f;

    public bool DebugMode => debugMode;
    public float DebugInfluence => debugInfluence;
    public float DebugLaunchAngle => debugLaunchAngle;
    public float DebugDamage => debugDamage;
    public float DebugDelaySeconds => debugDelaySeconds;

    public void SetDebugMode(bool value)
    {
        debugMode = value;
    }

    public void SetDebugInfluence(float value)
    {
        debugInfluence = Mathf.Clamp01(value);
    }

    public void SetDebugLaunchAngle(float value)
    {
        debugLaunchAngle = Mathf.Clamp(value, 0f, 360f);
    }

    public void SetDebugDamage(float value)
    {
        debugDamage = value;
    }

    public void SetDebugDelaySeconds(float value)
    {
        debugDelaySeconds = Mathf.Max(0f, value);
    }

    public void ApplyDebugDamage()
    {
        if (debugDelaySeconds <= 0f)
        {
            HitData immediateHitData = new HitData(debugInfluence, debugLaunchAngle, debugDamage, 0f, transform.position, 0);
            OnHit(immediateHitData);
            return;
        }

        StartCoroutine(ApplyDebugDamageAfterDelay(debugDelaySeconds));
    }

    private IEnumerator ApplyDebugDamageAfterDelay(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);

        HitData hitData = new HitData(debugInfluence, debugLaunchAngle, debugDamage, 0f, transform.position, 0);
        OnHit(hitData);
    }

    #endregion
}
