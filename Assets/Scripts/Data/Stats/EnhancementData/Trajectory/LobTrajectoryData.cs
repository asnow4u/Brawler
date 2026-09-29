using UnityEngine;

/// <summary>Flies in an arc under its own gravity.</summary>
[CreateAssetMenu(fileName = "LobTrajectory", menuName = "ScriptableObjects/SceneObject/Enhancement/Trajectory/Lob")]
public class LobTrajectoryData : TrajectoryData
{
    [Header("Arc")]
    [Tooltip("Downward acceleration in world units per second squared.")]
    public float Gravity = 30f;
}
