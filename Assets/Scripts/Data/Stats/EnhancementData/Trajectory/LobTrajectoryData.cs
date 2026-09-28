using UnityEngine;

/// <summary>
/// Flies in an arc under its own gravity. When a target is in range, the launch angle is adjusted so
/// the arc passes through it.
/// </summary>
[CreateAssetMenu(fileName = "LobTrajectory", menuName = "ScriptableObjects/SceneObject/Enhancement/Trajectory/Lob")]
public class LobTrajectoryData : TrajectoryData
{
    [Header("Arc")]
    [Tooltip("Downward acceleration in world units per second squared.")]
    public float Gravity = 30f;

    [Header("Targeting Assist")]
    [Tooltip("Distance in world units within which a target is used to adjust the launch angle.")]
    public float AssistRange = 10f;

    [Tooltip("Largest adjustment in degrees the assist can make to the launch angle.")]
    public float AssistMaxCorrection = 15f;
}
