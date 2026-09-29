using UnityEngine;

/// <summary>Flies in a straight line at the launch angle with no gravity.</summary>
[CreateAssetMenu(fileName = "StraightTrajectory", menuName = "ScriptableObjects/SceneObject/Enhancement/Trajectory/Straight")]
public class StraightTrajectoryData : TrajectoryData
{
    /// <summary>The direct angle to the offset. Always reachable.</summary>
    public override bool TrySolveAngles(Vector3 offset, float speed, out float lowAngle, out float highAngle)
    {
        lowAngle = highAngle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
        return true;
    }

    public override Vector3 OffsetAt(float angle, float speed, float time)
    {
        float radians = angle * Mathf.Deg2Rad;

        return new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f) * speed * time;
    }
}
