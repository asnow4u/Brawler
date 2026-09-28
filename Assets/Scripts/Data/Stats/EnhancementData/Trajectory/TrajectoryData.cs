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
