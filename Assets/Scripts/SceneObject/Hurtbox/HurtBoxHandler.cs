using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(ISceneObject))]
[RequireComponent(typeof(ActionStateHandler))]
[RequireComponent(typeof(Rigidbody))]
public class HurtBoxHandler : MonoBehaviour, IHurtBoxHandler, IHurtBoxHandlerEditor
{
    //Dependecies
    private ISceneObject sceneObject;
    private IActionState actionState;

    //Componenets
    private Rigidbody rb;

    //Hurt boxs
    private HurtBox[] hurtBoxes;

    [Header("Damage")]
    [SerializeField] protected float damageTaken = 0;

    [Header("Knockback")]
    [Tooltip("Base amount of acceleration that will be applied anytime taking a hit")]
    const float minKnockBackVelocity = 10f;
    [Tooltip("The exponential growth of knockback based on damage")]
    const float exGrowth = 2.8f;
    private Vector3 knockBackVelocity;

    [Header("Hit Stun")]
    [SerializeField] private HitStunState curHitStunState;
    [Tooltip("Normalized curve that defines velocity over hitstun time")]
    public AnimationCurve hitStunVelocityCurve;
    [Tooltip("Minimum duration of hit stun in seconds")]
    [SerializeField] private float minHitStunDuration = 0.1f;
    [Tooltip("How much should hit stun duration scale based on knockback velocity")]
    [SerializeField] private float hitStunMultiplier = 1f;
    [Tooltip("Duration of hitstun time dedicated to stun")]
    [SerializeField] private float stunDurationRatio = 0.1f;
    [Tooltip("Duration of hitstun time dedicated to launch")]
    [SerializeField] private float launchDurationRatio = 0.15f;
    [Tooltip("Duration of hitstun time dedicated to travel")]
    [SerializeField] private float travelDurationRatio = 0.5f;
    [Tooltip("Duration of hitstun time dedicated to recovery")]
    [SerializeField] private float recoveryDurationRatio = 0.25f;
    private float hitStunTime = 0;
    private float hitStunDuration = 0;
    private Coroutine hitStunTimerCoroutine;

    public event Action<HitStunData> OnHitEvent;
    public event Action<HitStunState> HitStunStateChangedEvent;

    private void Awake()
    {
        sceneObject = GetComponent<ISceneObject>();
        actionState = GetComponent<IActionState>();
        rb = GetComponent<Rigidbody>();

        if (stunDurationRatio + launchDurationRatio + travelDurationRatio + recoveryDurationRatio != 1f)
            Debug.LogError("HurtBoxHandler: Hitstun duration ratios must add up to 1. Please adjust the stunDurationRatio, launchDurationRatio, travelDurationRatio, and recoveryDurationRatio fields.", gameObject);

        //Get hurtboxs
        hurtBoxes = GetComponentsInChildren<HurtBox>(true);

        RegisterToEvents();
    }

    private void RegisterToEvents()
    {
        foreach (HurtBox hurtBox in hurtBoxes)
            hurtBox.OnHitEvent += OnHit;
    }

    private void OnDestroy()
    {
        UnRegisterFromEvents();
    }

    private void UnRegisterFromEvents()
    {
        foreach (HurtBox hurtBox in hurtBoxes)
            hurtBox.OnHitEvent -= OnHit;
    }

    private void OnHit(HitData hitData)
    {
        if (hitData == null)
            return;

        sceneObject.Log("HurtboxHandler: Hit for " + hitData);
        damageTaken += hitData.Damage;

        //Knockback
        knockBackVelocity = CalculateKnockbackVelocity(hitData.Influence, damageTaken, hitData.LauchAngle, rb.mass);

        //Hitstun
        float hitStunDuration = Mathf.Max(minHitStunDuration, knockBackVelocity.magnitude * hitStunMultiplier);
        ApplyHitStun(hitStunDuration);

        OnHitEvent?.Invoke(new HitStunData(hitData.SceneObjectID, knockBackVelocity));
    }

    public Vector3 CalculateKnockbackVelocity(float influence, float totalDamage, float launchAngle, float mass)
    {
        float minForce = mass * minKnockBackVelocity;
        float damageForce = minForce + influence * (Mathf.Pow(totalDamage, exGrowth) / mass);

        float xLaunch = Mathf.Cos(launchAngle * Mathf.Deg2Rad);
        float yLaunch = Mathf.Sin(launchAngle * Mathf.Deg2Rad);
        Vector3 launchDirection = new Vector2(xLaunch, yLaunch);

        return launchDirection * damageForce / mass;
    }

    private void ChangeHitStunState(HitStunState newState)
    {
        if (newState == curHitStunState)
            return;

        if (newState == HitStunState.Null)
        {
            actionState.ChangeState(ActionState.Idle);

            knockBackVelocity = Vector3.zero;
            hitStunTime = 0;
            hitStunDuration = 0;

            if (hitStunTimerCoroutine != null)
                StopCoroutine(hitStunTimerCoroutine);
        }
        else
        {
            actionState.ChangeState(ActionState.HitStun);
        }

        curHitStunState = newState;
        HitStunStateChangedEvent?.Invoke(newState);
    }

    private void ApplyHitStun(float hitStunDuration)
    {
        if (hitStunTimerCoroutine != null)
            StopCoroutine(hitStunTimerCoroutine);

        hitStunTimerCoroutine = StartCoroutine(HitStunTimer(hitStunDuration));
    }

    private IEnumerator HitStunTimer(float duration)
    {
        hitStunTime = 0;
        hitStunDuration = duration;

        float stunDuration = duration * stunDurationRatio;
        float launchDuration = duration * launchDurationRatio;
        float travelDuration = duration * travelDurationRatio;
        float recoveryDuration = duration * recoveryDurationRatio;

        ChangeHitStunState(HitStunState.Stun);

        while (hitStunTime < duration)
        {
            hitStunTime += Time.deltaTime;

            if (hitStunTime > stunDuration && curHitStunState == HitStunState.Stun)
                ChangeHitStunState(HitStunState.Launch);
            else if (hitStunTime > stunDuration + launchDuration && curHitStunState == HitStunState.Launch)
                ChangeHitStunState(HitStunState.Travel);
            else if (hitStunTime > stunDuration + launchDuration + travelDuration && curHitStunState == HitStunState.Travel)
                ChangeHitStunState(HitStunState.Recovery);

            yield return null;
        }

        hitStunTime = 0;
        hitStunDuration = 0;
        ChangeHitStunState(HitStunState.Null);
    }

    public Vector3 EvaluateHitStunVelocity()
    {
        if (curHitStunState == HitStunState.Null || curHitStunState == HitStunState.Stun || hitStunDuration == 0)
            return Vector3.zero;

        float normalizedVelocity = hitStunVelocityCurve.Evaluate(hitStunTime / hitStunDuration);
        return knockBackVelocity * normalizedVelocity;
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
            HitData immediateHitData = new HitData(Guid.NewGuid(), debugInfluence, debugLaunchAngle, debugDamage);
            OnHit(immediateHitData);
            return;
        }

        StartCoroutine(ApplyDebugDamageAfterDelay(debugDelaySeconds));
    }

    private IEnumerator ApplyDebugDamageAfterDelay(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);

        HitData hitData = new HitData(Guid.NewGuid(), debugInfluence, debugLaunchAngle, debugDamage);
        OnHit(hitData);
    }

    #endregion
}
