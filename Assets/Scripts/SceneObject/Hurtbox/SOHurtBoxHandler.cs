using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(ActionStateHandler))]
[RequireComponent(typeof(StatHandler))]
public class SOHurtBoxHandler : HurtBoxHandler, ISOHurtBoxHandler
{
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

    private IActionState actionState;
    private IStats statHandler;

    private MovementStatData curMovementData;

    public event Action<HitStunState> HitStunStateChangedEvent;

    protected override void Awake()
    {
        actionState = GetComponent<IActionState>();
        statHandler = GetComponent<IStats>();

        base.Awake();
    }

    protected override void RegisterToEvents()
    {
        statHandler.MovementStatsChangedEvent += OnMovementStatsChanged;

        base.RegisterToEvents();
    }

    protected override void UnRegisterFromEvents()
    {
        statHandler.MovementStatsChangedEvent -= OnMovementStatsChanged;

        base.UnRegisterFromEvents();
    }

    private void OnMovementStatsChanged(MovementStatData movementStats)
    {
        curMovementData = movementStats;
    }

    protected override void OnHit(HitData hitData)
    {
        if (hitData == null)
            return;

        base.OnHit(hitData);

        if (hitData is SceneObjectHitData sceneObjectHitData)
            lastHitBy.Add(sceneObjectHitData.SceneObjectAttackerID);

        //Compute per-phase durations from the knockback and current gravity
        float launchDuration = CalculateLaunchDuration(knockBackVelocity.magnitude);
        float travelDuration = CalculateTravelDuration(knockBackVelocity.y);

        //Hitstun
        ApplyHitStun(hitData.StunTime, launchDuration, travelDuration, recoveryDuration);
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
}
