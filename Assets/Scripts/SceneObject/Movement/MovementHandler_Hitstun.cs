using System.Collections.Generic;
using UnityEngine;

internal partial class MovementHandler
{
    [Header("Hit Stun")]
    [Tooltip("Per-FixedUpdate velocity multiplier applied during Travel and Recovery. Lower = more drag.")]
    [Range(0.5f, 1f)]
    [SerializeField] private float hitStunDrag = 0.95f;
    private Vector3 pendingKnockbackVelocity = Vector3.zero;

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

    /// <summary>
    /// Cache knockback velocity from the hit. Application waits for the Launch state transition.
    /// </summary>
    private void ApplyHitStunKnockback(KnockBackHitData hitData)
    {
        pendingKnockbackVelocity = hitData.KnockBackVelocity;
    }


    /// <summary>
    /// React to hit stun state transitions. Pause freezes velocity. Launch applies the cached knockback once.
    /// </summary>
    /// <summary>
    /// React to hit stun state transitions. Pause freezes velocity. Launch applies the cached knockback once
    /// and immediately checks for a bounce so an at-rest hit reflects off the surface it's resting against.
    /// </summary>
    /// <summary>
    /// React to hit stun state transitions. Pause freezes velocity. Launch applies the cached knockback once
    /// and immediately checks for a bounce so an at-rest hit reflects off the surface it's resting against.
    /// Any transition during an active splat hold ends the hold, restoring held velocity first.
    /// </summary>
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
                rb.linearVelocity = pendingKnockbackVelocity;
                pendingKnockbackVelocity = Vector3.zero;
                CheckForHitStunBounce();
                break;

            case HitStunState.Null:
                pendingKnockbackVelocity = Vector3.zero;
                break;
        }
    }

    /// <summary>
    /// Apply drag to the hit stun velocity. Called during Travel and Recovery substates.
    /// </summary>
    /// <summary>
    /// Apply X-only drag to the hit stun velocity. Y is owned entirely by gravity and clamps.
    /// Called during Travel and Recovery substates.
    /// </summary>
    private void UpdateHitStunDeceleration()
    {
        Vector3 v = rb.linearVelocity;
        v.x *= hitStunDrag;
        rb.linearVelocity = v;
    }

    /// <summary>
    /// Detect a bounce condition and, if found, reflect velocity off the impacted surface(s).
    /// If the reflected magnitude meets the splat threshold during Launch or Travel, hold velocity briefly
    /// for a dramatic pause before restoring.
    /// </summary>
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

    /// <summary>
    /// Begin a bounce splat hold. Zeros velocity, stores the reflected vector, and sets the expiration time.
    /// </summary>
    private void BeginBounceSplat(Vector3 reflectedVelocity)
    {
        splatHeldVelocity = reflectedVelocity;
        splatHoldEndTime = Time.time + splatHoldDuration;
        isSplatHolding = true;
        rb.linearVelocity = Vector3.zero;
    }

    /// <summary>
    /// End a bounce splat hold and restore the held velocity to the rigidbody.
    /// </summary>
    private void EndBounceSplat()
    {
        rb.linearVelocity = splatHeldVelocity;
        splatHeldVelocity = Vector3.zero;
        splatHoldEndTime = 0f;
        isSplatHolding = false;
    }

    /// <summary>
    /// While splat-holding, check whether the hold has expired. If so, end the hold and restore velocity.
    /// </summary>
    private void UpdateBounceSplat()
    {
        if (Time.time >= splatHoldEndTime)
            EndBounceSplat();
    }


    /// <summary>
    /// Find the surface normals the sceneObject should bounce off of.
    /// First checks for direct contact with surfaces opposing velocity; if none, runs a predictive raycast lookahead.
    /// Only surfaces that velocity is moving into (negative dot product) are included.
    /// </summary>
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

    /// <summary>
    /// Reflect the current velocity off the averaged surface normal and apply bounce degrade.
    /// Returns Vector3.zero if the reflected magnitude falls below the bounce threshold.
    /// </summary>
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
