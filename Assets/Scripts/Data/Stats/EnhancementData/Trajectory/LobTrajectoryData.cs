using UnityEngine;

/// <summary>Flies in an arc under its own gravity.</summary>
[CreateAssetMenu(fileName = "LobTrajectory", menuName = "ScriptableObjects/SceneObject/Enhancement/Trajectory/Lob")]
public class LobTrajectoryData : TrajectoryData
{
    [Header("Arc")]
    [Tooltip("Downward acceleration in world units per second squared.")]
    public float Gravity = 30f;


    /// <summary>The low and high arcs that pass through the offset at the speed under this gravity.</summary>
    public override bool TrySolveAngles(Vector3 offset, float speed, out float lowAngle, out float highAngle)
    {
        float dx = Mathf.Abs(offset.x);

        if (Gravity <= 0f || dx < 0.01f)
        {
            lowAngle = highAngle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
            return true;
        }

        float speedSquared = speed * speed;
        float discriminant = speedSquared * speedSquared - Gravity * (Gravity * dx * dx + 2f * offset.y * speedSquared);

        if (discriminant < 0f)
        {
            lowAngle = highAngle = 0f;
            return false;
        }

        float root = Mathf.Sqrt(discriminant);
        float low = Mathf.Atan((speedSquared - root) / (Gravity * dx)) * Mathf.Rad2Deg;
        float high = Mathf.Atan((speedSquared + root) / (Gravity * dx)) * Mathf.Rad2Deg;

        // Angles above are for a target to the right; mirrored for a target to the left.
        bool toRight = offset.x >= 0f;
        lowAngle = toRight ? low : 180f - low;
        highAngle = toRight ? high : 180f - high;
        return true;
    }

    public override Vector3 OffsetAt(float angle, float speed, float time)
    {
        float radians = angle * Mathf.Deg2Rad;
        Vector3 launch = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f) * speed * time;

        return launch + Vector3.down * (0.5f * Gravity * time * time);
    }
}
