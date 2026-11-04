using UnityEngine;

public partial class MovementHandler : IRootMotion
{
    [Header("Root Motion")]
    [Tooltip("Root motion speed on an axis above this value takes ownership of that axis.")]
    [SerializeField] private float rootMotionThreshold = 0.05f;

    private Vector3 rootMotionVelocity = Vector3.zero;
    private bool isRootMotionPaused = false;
    private bool isRootMotionOwningX = false;
    private bool isRootMotionOwningY = false;

    public void SetRootMotion(Vector3 velocity, bool isPaused)
    {
        rootMotionVelocity = velocity;
        isRootMotionPaused = isPaused;
    }

    /// <summary>Sets velocity on each axis owned by root motion. Ownership is held while paused, with owned axes held at zero.</summary>
    private void ApplyRootMotion()
    {
        if (!isRootMotionPaused)
        {
            isRootMotionOwningX = Mathf.Abs(rootMotionVelocity.x) > rootMotionThreshold;
            isRootMotionOwningY = Mathf.Abs(rootMotionVelocity.y) > rootMotionThreshold;
        }

        if (!isRootMotionOwningX && !isRootMotionOwningY)
            return;

        Vector3 velocity = rb.linearVelocity;

        if (isRootMotionOwningX)
            velocity.x = isRootMotionPaused ? 0f : rootMotionVelocity.x;

        if (isRootMotionOwningY)
            velocity.y = isRootMotionPaused ? 0f : rootMotionVelocity.y;

        velocity.z = 0f;
        rb.linearVelocity = velocity;
    }

    private void ClearRootMotionOwnership()
    {
        isRootMotionOwningX = false;
        isRootMotionOwningY = false;
    }
}
