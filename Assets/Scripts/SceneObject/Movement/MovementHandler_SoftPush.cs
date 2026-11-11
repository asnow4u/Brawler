using UnityEngine;

public partial class MovementHandler
{
    [Header("Soft Push")]
    [Tooltip("Horizontal speed applied when fully overlapping another idle grounded sceneObject.")]
    [SerializeField] private float softPushSpeed = 3f;
    [Tooltip("Lowest horizontal speed applied while any overlap remains.")]
    [SerializeField] private float softPushMinSpeed = 0.5f;
    [Tooltip("Overlap depth at which softPushSpeed is reached.")]
    [SerializeField] private float softPushFullDepth = 0.5f;
    [Tooltip("Distance below the pushed edge checked for ground.")]
    [SerializeField] private float softPushLedgeCheckDepth = 0.3f;
    [Tooltip("Distance beside the body checked for a wall on the push side.")]
    [SerializeField] private float softPushWallCheckDistance = 0.05f;

    private const int SoftPushBufferSize = 16;
    private readonly Collider[] softPushBuffer = new Collider[SoftPushBufferSize];
    private LayerMask softPushMask;


    private void InitSoftPush()
    {
        softPushMask = LayerMask.GetMask("SceneObject");
    }

    /// <summary>Separates this sceneObject horizontally from overlapping idle grounded sceneObjects. The player neither pushes nor is pushed.</summary>
    private void ApplySoftPush()
    {
        if (!IsSoftPushEligible(sceneObject, actionState))
            return;

        Bounds bounds = sceneObject.Bounds;
        float push = 0f;

        int count = Physics.OverlapBoxNonAlloc(bounds.center, bounds.extents, softPushBuffer, Quaternion.identity, softPushMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            Collider other = softPushBuffer[i];
            if (other == col)
                continue;

            if (!other.TryGetComponent(out ISceneObject otherSceneObject) || !other.TryGetComponent(out IActionState otherActionState))
                continue;

            if (!IsSoftPushEligible(otherSceneObject, otherActionState))
                continue;

            Bounds otherBounds = otherSceneObject.Bounds;
            float offsetX = bounds.center.x - otherBounds.center.x;
            float overlap = bounds.extents.x + otherBounds.extents.x - Mathf.Abs(offsetX);
            if (overlap <= 0f)
                continue;

            float direction;
            if (Mathf.Abs(offsetX) > 1e-4f)
                direction = Mathf.Sign(offsetX);
            else
                direction = sceneObject.UniqueID.CompareTo(otherSceneObject.UniqueID) > 0 ? 1f : -1f;

            push += direction * overlap;
        }

        if (Mathf.Abs(push) < 1e-4f)
            return;

        float pushDirection = Mathf.Sign(push);
        float depthT = softPushFullDepth > 0f ? Mathf.Clamp01(Mathf.Abs(push) / softPushFullDepth) : 1f;
        float speed = Mathf.Max(softPushSpeed * depthT, softPushMinSpeed);

        if (IsSoftPushBlocked(bounds, pushDirection, speed * Time.fixedDeltaTime))
            return;

        rb.WakeUp();
        rb.linearVelocity = new Vector3(pushDirection * speed, rb.linearVelocity.y, 0);
    }

    private static bool IsSoftPushEligible(ISceneObject target, IActionState targetState)
    {
        return target.ObjectType != SceneObjectType.Player &&
               targetState.CurGroundedState == GroundedState.Grounded &&
               targetState.CurActionState == ActionState.Idle;
    }

    /// <summary>True when a wall is on the push side or the pushed edge would leave the ground.</summary>
    private bool IsSoftPushBlocked(Bounds bounds, float pushDirection, float step)
    {
        Direction side = pushDirection > 0f ? Direction.Right : Direction.Left;
        if (sceneObject.TryDetectCollision(side, softPushWallCheckDistance, environmentLayerMask, out _))
            return true;

        float edgeX = pushDirection > 0f ? bounds.max.x + step : bounds.min.x - step;
        Vector3 origin = new Vector3(edgeX, bounds.min.y + 0.1f, 0f);

        return !Physics.Raycast(origin, Vector3.down, softPushLedgeCheckDepth, environmentLayerMask, QueryTriggerInteraction.Ignore);
    }
}
