using System.Collections.Generic;
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
    

    // Attack's baked influence for the current hit; drives the Travel drag lerp. Set at Launch, not player input.
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
    private float splatHoldEndTime = 0f;
    private Vector3 splatHeldVelocity = Vector3.zero;

    protected virtual void OnRecievedHitStunKnockback(KnockBackHitData hitData)
    {
        pendingKnockbackVelocity = hitData.KnockBackVelocity;
        pendingInfluence = hitData.Influence;
    }

    private void OnHitStunStateChanged(HitStunState state)
    {
        if (isSplatHolding)
            EndBounceSplat();

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
                CheckForHitStunBounce();
                break;

            case HitStunState.Null:
                pendingKnockbackVelocity = Vector3.zero;
                pendingInfluence = 0f;
                currentHitInfluence = 0f;
                break;
        }
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
            case HitStunState.Launch:
                CheckForHitStunBounce();
                break;

            case HitStunState.Travel:
                UpdateHitStunDeceleration();
                ApplyGravity();
                CheckForHitStunBounce();
                break;

            case HitStunState.Recovery:
                UpdateHitStunDeceleration();
                ApplyGravity();
                CheckForHitStunBounce();
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

    /// <summary>
    /// Input-driven mid-flight drift (DI). No-op on the base handler; InputMovementHandler overrides it.
    /// </summary>
    protected virtual void ApplyHitStunDrift() { }

    protected virtual Vector3 ApplyLaunchVelocity(Vector3 knockbackVelocity)
    {
        return knockbackVelocity;
    }

    private void CheckForHitStunBounce()
    {
        if (rb.linearVelocity.sqrMagnitude < minBounceVelocity * minBounceVelocity)
            return;

        if (!TryGetBounceNormals(out List<Vector3> hitNormals))
            return;

        Vector3 bounceVelocity = CalculateBounceVelocity(hitNormals);

        HitStunState curState = hurtBoxHandler.CurHitStunState;
        bool splatEligible = curState == HitStunState.Launch || curState == HitStunState.Travel;

        if (splatEligible && bounceVelocity.magnitude >= splatVelocityThreshold)
        {
            BeginBounceSplat(bounceVelocity);
            return;
        }

        rb.linearVelocity = bounceVelocity;
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
            EndBounceSplat();
    }

    private bool TryGetBounceNormals(out List<Vector3> normals)
    {
        normals = new List<Vector3>();
        Vector3 velocity = rb.linearVelocity;
        LayerMask environmentMask = LayerMask.GetMask("Environment");

        //Contact pass - check current contact against surfaces opposing each velocity axis
        if (velocity.x > 0 && sceneObject.TryDetectCollision(Direction.Right, 0.05f, environmentMask, out _))
            normals.Add(Vector3.left);
        else if (velocity.x < 0 && sceneObject.TryDetectCollision(Direction.Left, 0.05f, environmentMask, out _))
            normals.Add(Vector3.right);

        if (velocity.y > 0 && sceneObject.TryDetectCollision(Direction.Up, 0.05f, environmentMask, out _))
            normals.Add(Vector3.down);
        else if (velocity.y < 0 && sceneObject.TryDetectCollision(Direction.Down, 0.05f, environmentMask, out _))
            normals.Add(Vector3.up);

        if (normals.Count > 0)
            return true;

        //Predictive raycast pass
        Bounds bounds = sceneObject.Bounds;
        Vector3 direction = velocity.normalized;
        float distance = velocity.magnitude * Time.fixedDeltaTime;
        float centralZ = (bounds.max.z + bounds.min.z) / 2;

        List<Vector3> boundPoints = new List<Vector3>();
        if (direction.x > 0)
        {
            boundPoints.Add(new Vector3(bounds.max.x, bounds.max.y, centralZ));
            boundPoints.Add(new Vector3(bounds.max.x, bounds.center.y, centralZ));
            boundPoints.Add(new Vector3(bounds.max.x, bounds.min.y, centralZ));
        }
        else if (direction.x < 0)
        {
            boundPoints.Add(new Vector3(bounds.min.x, bounds.max.y, centralZ));
            boundPoints.Add(new Vector3(bounds.min.x, bounds.center.y, centralZ));
            boundPoints.Add(new Vector3(bounds.min.x, bounds.min.y, centralZ));
        }

        if (direction.y > 0)
        {
            boundPoints.Add(new Vector3(bounds.min.x, bounds.max.y, centralZ));
            boundPoints.Add(new Vector3(bounds.center.x, bounds.max.y, centralZ));
            boundPoints.Add(new Vector3(bounds.max.x, bounds.max.y, centralZ));
        }
        else if (direction.y < 0)
        {
            boundPoints.Add(new Vector3(bounds.min.x, bounds.min.y, centralZ));
            boundPoints.Add(new Vector3(bounds.center.x, bounds.min.y, centralZ));
            boundPoints.Add(new Vector3(bounds.max.x, bounds.min.y, centralZ));
        }

        if (boundPoints.Count == 0)
            return false;

        foreach (Vector3 point in boundPoints)
        {
            if (!Physics.Raycast(point, direction, out RaycastHit hit, distance, environmentMask))
                continue;

            //Only include normals from surfaces velocity is moving INTO
            if (Vector3.Dot(hit.normal, velocity) >= 0)
                continue;

            normals.Add(hit.normal);
        }

        return normals.Count > 0;
    }

    private Vector3 CalculateBounceVelocity(List<Vector3> hitNormals)
    {
        if (hitNormals == null || hitNormals.Count == 0)
            return Vector3.zero;

        Vector3 averageNormal = Vector3.zero;
        foreach (Vector3 normal in hitNormals)
            averageNormal += normal;
        averageNormal /= hitNormals.Count;
        averageNormal.Normalize();

        Vector3 bounceVelocity = Vector3.Reflect(rb.linearVelocity, averageNormal) * bounceDegrade;

        if (bounceVelocity.magnitude < minBounceVelocity)
            return Vector3.zero;

        return bounceVelocity;
    }
}
