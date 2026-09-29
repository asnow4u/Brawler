using System;
using UnityEngine;

/// <summary>
/// How a spawned object flies. Holds a launch angle per attack slot, authored facing right, plus the
/// values every flight uses. Subclasses add the values specific to their flight type.
/// </summary>
public abstract class TrajectoryData : ScriptableObject
{
    [Header("Launch Angles")]
    [Tooltip("Launch angles in degrees, authored facing right." +
        "\n 0 is forward," +
        "\n 90 is up," +
        "\n 270 is down")]
    public float UpTiltAngle = 90f;
    public float DownTiltAngle = 0f;
    public float ForwardTiltAngle = 0f;
    public float UpAirAngle = 90f;
    public float DownAirAngle = 270f;
    public float ForwardAirAngle = 0f;

    [Header("Flight")]
    [Tooltip("Launch speed in world units per second.")]
    public float Speed = 12f;

    [Tooltip("Time in seconds before the spawned object is removed.")]
    public float Lifetime = 3f;

    [Header("Targeting Assist")]
    [Tooltip("Distance in world units within which a target is used to aim the spawned object.")]
    public float AssistRange = 10f;

    [Tooltip("Largest adjustment in degrees the assist can make to a launch angle.")]
    public float AssistMaxCorrection = 15f;


    /// <summary>Launch angle for an attack state, passed as its AttackState value. Zero for no attack.</summary>
    public float GetAngle(int attackState)
    {
        switch (attackState)
        {
            case 0: return UpTiltAngle;
            case 1: return DownTiltAngle;
            case 2: return ForwardTiltAngle;
            case 3: return UpAirAngle;
            case 4: return DownAirAngle;
            case 5: return ForwardAirAngle;
            default: return 0f;
        }
    }

    /// <summary>
    /// Launch angles in degrees, 0 being right and 90 up, that reach an offset from the launch point at
    /// a speed. The same angle twice when only one exists. False when the offset is out of reach.
    /// </summary>
    public abstract bool TrySolveAngles(Vector3 offset, float speed, out float lowAngle, out float highAngle);

    /// <summary>Position relative to the launch point after a time, launched at an angle in degrees and a speed.</summary>
    public abstract Vector3 OffsetAt(float angle, float speed, float time);

    /// <summary>
    /// Whether a launch from the origin at an angle and speed passes nothing on the mask before reaching
    /// the target's horizontal position. Traced as a sphere of the given radius in the given number of
    /// segments.
    /// </summary>
    public bool IsFlightClear(Vector3 origin, Vector3 target, float angle, float speed, LayerMask blockingMask, float radius, int segments = 12)
    {
        float radians = angle * Mathf.Deg2Rad;
        float horizontalSpeed = Mathf.Abs(Mathf.Cos(radians)) * speed;

        float duration = horizontalSpeed > 0.01f
            ? Mathf.Abs(target.x - origin.x) / horizontalSpeed
            : Mathf.Abs(target.y - origin.y) / Mathf.Max(0.01f, speed);

        Vector3 previous = origin;

        for (int i = 1; i <= segments; i++)
        {
            Vector3 point = origin + OffsetAt(angle, speed, duration * i / segments);
            Vector3 segment = point - previous;

            if (segment.sqrMagnitude > 0f &&
                Physics.SphereCast(previous, radius, segment.normalized, out _, segment.magnitude, blockingMask, QueryTriggerInteraction.Ignore))
                return false;

            previous = point;
        }

        return true;
    }


    #region Editor Updating

    public event Action OnChangedEvent;

    #if UNITY_EDITOR

        private void OnValidate()
        {
            if (Application.isPlaying)
                OnChangedEvent?.Invoke();
        }

    #endif

    #endregion
}
