using UnityEngine;

public partial class MovementHandler
{
    [Header("Hit Stun Drag")]
    [Tooltip("Per-FixedUpdate X velocity multiplier during Travel for an influence=0 hit (snappy stop, the setup feel).")]
    [Range(0.5f, 1f)]
    [SerializeField] private float travelDragSetup = 0.95f;
    [Tooltip("Per-FixedUpdate X velocity multiplier during Travel for an influence=1 hit (carries through, the finisher feel).")]
    [Range(0.5f, 1f)]
    [SerializeField] private float travelDragFinisher = 0.995f;
    [Tooltip("Per-FixedUpdate X velocity multiplier during Recovery, regardless of attack. Light drag, this is the DI/follow-up window.")]
    [Range(0.5f, 1f)]
    [SerializeField] private float recoveryDrag = 0.98f;
    private Vector3 pendingKnockbackVelocity = Vector3.zero;
    private float pendingInfluence = 0f;

    [Header("Hit Stun Collision")]
    [Tooltip("Physics material applied to the physical collider while in hitstun.")]
    [SerializeField] private PhysicsMaterial hitStunPhysicsMaterial;
    private PhysicsMaterial defaultPhysicsMaterial;
    private LayerMask sceneObjectLayerMask;
    private LayerMask environmentLayerMask;
    private float currentHitInfluence = 0f;

    [Header("Bounce")]
    [SerializeField] private float bounceDegrade = 0.9f;
    [Tooltip("Reflected velocity magnitudes below this threshold are zeroed instead of bouncing.")]
    [SerializeField] private float minBounceVelocity = 0.9f;
    [Tooltip("Reflected velocity magnitudes at or above this threshold trigger a brief hold before resuming.")]
    [SerializeField] private float splatVelocityThreshold = 15f;
    [Tooltip("Duration of the bounce splat hold in seconds.")]
    [SerializeField] private float splatHoldDuration = 0.1f;
    private bool isSplatHolding = false;
    private Vector3 preSolveVelocity = Vector3.zero;
    private float splatHoldEndTime = 0f;
    private Vector3 splatHeldVelocity = Vector3.zero;


    private void InitHitStunCollision()
    {
        defaultPhysicsMaterial = col.sharedMaterial;
        sceneObjectLayerMask = LayerMask.GetMask("SceneObject");
        environmentLayerMask = LayerMask.GetMask("Environment");
    }

    protected virtual void OnRecievedHitStunKnockback(KnockBackHitData hitData)
    {
        pendingKnockbackVelocity = hitData.KnockBackVelocity;
        pendingInfluence = hitData.Influence;
    }

    private void OnHitStunStateChanged(HitStunState state)
    {
        if (isSplatHolding)
            EndBounceSplat();

        bool inHitStun = state != HitStunState.Null;

        if (UsesNativeGravity)
            rb.useGravity = !inHitStun;

        ApplyHitStunCollisionState(inHitStun);

        switch (state)
        {
            case HitStunState.Pause:
                rb.linearVelocity = Vector3.zero;
                break;

            case HitStunState.Launch:
                rb.linearVelocity = ApplyLaunchVelocity(pendingKnockbackVelocity);
                pendingKnockbackVelocity = Vector3.zero;
                currentHitInfluence = pendingInfluence;
                pendingInfluence = 0f;
                break;

            case HitStunState.Null:
                pendingKnockbackVelocity = Vector3.zero;
                pendingInfluence = 0f;
                currentHitInfluence = 0f;
                break;
        }
    }    

    private void ApplyHitStunCollisionState(bool inHitStun)
    {
        col.excludeLayers = inHitStun ? sceneObjectLayerMask : 0;
        col.sharedMaterial = inHitStun ? hitStunPhysicsMaterial : defaultPhysicsMaterial;

        //Passive objects sleep at rest by design. A sleeping body generates no collision
        //callbacks, so a hit landed on a resting object would never reach the bounce logic.
        if (inHitStun)
            rb.WakeUp();
    }

    protected virtual void UpdateHitStunMovement()
    {
        if (isSplatHolding)
        {
            UpdateBounceSplat();
            return;
        }

        switch (hurtBoxHandler.CurHitStunState)
        {
            case HitStunState.Travel:
            case HitStunState.Recovery:
                UpdateHitStunDeceleration();
                ApplyGravity();
                break;
        }
    }

    private void UpdateHitStunDeceleration()
    {
        float drag;

        if (hurtBoxHandler.CurHitStunState == HitStunState.Recovery)
            drag = recoveryDrag;
        else
            drag = Mathf.Lerp(travelDragSetup, travelDragFinisher, currentHitInfluence);

        Vector3 v = rb.linearVelocity;
        v.x *= drag;
        rb.linearVelocity = v;

        ApplyHitStunDrift();
    }

    protected virtual void ApplyHitStunDrift() { }

    protected virtual Vector3 ApplyLaunchVelocity(Vector3 knockbackVelocity)
    {
        return knockbackVelocity;
    }



    private void OnCollisionEnter(Collision collision)
    {
        TryBounce(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        TryBounce(collision);
    }

    
    #region Bounce

    private void TryBounce(Collision collision)
    {
        if (actionState == null || actionState.CurActionState != ActionState.HitStun)
            return;

        if (isSplatHolding)
            return;

        if ((environmentLayerMask.value & (1 << collision.gameObject.layer)) == 0)
            return;

        if (preSolveVelocity.sqrMagnitude < minBounceVelocity * minBounceVelocity)
            return;

        if (!TryGetAverageContactNormal(collision, out Vector3 averageNormal))
            return;

        Vector3 bounceVelocity = CalculateBounceVelocity(averageNormal);

        HitStunState curState = hurtBoxHandler.CurHitStunState;
        bool splatEligible = curState == HitStunState.Launch || curState == HitStunState.Travel;

        if (splatEligible && bounceVelocity.magnitude >= splatVelocityThreshold)
        {
            BeginBounceSplat(bounceVelocity);
            return;
        }

        rb.linearVelocity = bounceVelocity;
    }

    private bool TryGetAverageContactNormal(Collision collision, out Vector3 averageNormal)
    {
        averageNormal = Vector3.zero;

        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 normal = collision.GetContact(i).normal;

            //Only include normals from surfaces velocity is moving INTO
            if (Vector3.Dot(normal, preSolveVelocity) >= 0)
                continue;

            averageNormal += normal;
        }

        if (averageNormal.sqrMagnitude < 1e-6f)
            return false;

        averageNormal.Normalize();
        return true;
    }

    private Vector3 CalculateBounceVelocity(Vector3 surfaceNormal)
    {
        Vector3 bounceVelocity = Vector3.Reflect(preSolveVelocity, surfaceNormal) * bounceDegrade;
        bounceVelocity.z = 0;

        if (bounceVelocity.magnitude < minBounceVelocity)
            return Vector3.zero;

        return bounceVelocity;
    }

    private void BeginBounceSplat(Vector3 reflectedVelocity)
    {
        splatHeldVelocity = reflectedVelocity;
        splatHoldEndTime = Time.time + splatHoldDuration;
        isSplatHolding = true;
        rb.linearVelocity = Vector3.zero;
    }

    private void EndBounceSplat()
    {
        rb.linearVelocity = splatHeldVelocity;
        splatHeldVelocity = Vector3.zero;
        splatHoldEndTime = 0f;
        isSplatHolding = false;
    }

    private void UpdateBounceSplat()
    {
        if (Time.time >= splatHoldEndTime)
        {
            EndBounceSplat();
            return;
        }

        //Held rather than set once - the collider is live now, so PhysX depenetration
        //would otherwise drift the object during the hold.
        rb.linearVelocity = Vector3.zero;
    }

    #endregion
}
