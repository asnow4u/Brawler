using UnityEngine;

/// <summary>
/// Follows a path from the LevelNavGraph and returns a NavIntent each tick.
///   Walk, Climb - hold direction to the segment end.
///   Jump        - approach the launch point, hold jump through the arc, steer in the air.
///   Fall        - walk off the ledge, steer in the air once below it.
/// </summary>
public class NavAgent : MonoBehaviour
{
    [Header("Graph")]
    [SerializeField] private LevelNavGraph navGraph;

    [Header("Arrival")]
    [Tooltip("Horizontal distance from a segment end that counts as reaching it.")]
    [SerializeField] private float segmentTolerance = 0.35f;
    [Tooltip("Vertical distance from a segment's surface that counts as being on it.")]
    [SerializeField] private float verticalTolerance = 0.6f;
    [Tooltip("Horizontal distance from the final destination that counts as arrived.")]
    [SerializeField] private float destinationTolerance = 1f;

    [Header("Re-planning")]
    [Tooltip("Distance the destination must move before the path is rebuilt.")]
    [SerializeField] private float destinationMoveThreshold = 1.5f;
    [Tooltip("Minimum time between searches, in seconds.")]
    [SerializeField] private float replanCooldown = 0.25f;
    [Tooltip("Delay after hitstun before planning again, in seconds.")]
    [SerializeField] private float reengageDelay = 0.4f;
    [Tooltip("Consecutive failed attempts before the agent gives up.")]
    [SerializeField] private int maxConsecutiveFailures = 3;

    [Header("Stuck Detection")]
    [Tooltip("Time without progress before the path is abandoned, in seconds.")]
    [SerializeField] private float stuckTimeout = 1.5f;
    [Tooltip("Distance that counts as progress.")]
    [SerializeField] private float stuckProgressThreshold = 0.05f;

    [Header("Jumping")]
    [Tooltip("Fraction of standing jump reach below which a standing jump is used instead of a running one.")]
    [SerializeField] private float standingJumpSafety = 0.8f;
    [Tooltip("Distance from the launch point that counts as being on it.")]
    [SerializeField] private float launchTolerance = 0.2f;
    [Tooltip("Speed below which the agent counts as stopped for a standing jump.")]
    [SerializeField] private float standingSpeedThreshold = 0.5f;

    [Header("Debug")]
    [Tooltip("Log every plan, and the agent's traversal limits when a plan fails.")]
    [SerializeField] private bool logNavigation = false;

    [Header("Gizmos")]
    [SerializeField] private bool drawPath = true;
    [Tooltip("Draw the reachable jump boundary from the agent's position.")]
    [SerializeField] private bool drawJumpEnvelope = false;
    [Tooltip("Depth below the agent the jump boundary is drawn to, in world units.")]
    [SerializeField] private float envelopeDropDepth = 8f;

    // Components
    private IActionState actionState;
    private IStats statHandler;
    private Collider ownCollider;
    private Rigidbody body;

    private MovementStatData curMovementData = null;
    private NavAgentProfile profile;

    // Destination and path
    private Vector3 destination;
    private bool hasDestination;
    private readonly NavPath path = new NavPath();
    private int segmentIndex;

    // Set when the final segment completes. Cleared when the destination changes, on hitstun, or when pushed away.
    private bool arrived;

    // Planning
    private float nextPlanAllowedTime;
    private int consecutiveFailures;
    private bool givenUp;

    // Jump and fall execution
    private enum TraversalPhase { Approach, Committed, Airborne }
    private TraversalPhase phase;
    private bool runningLaunch;

    // Stuck detection
    private float lastProgressTime;
    private Vector3 lastProgressPosition;

    // Output, rebuilt every tick
    private NavIntent intent;


    #region Public API

    public bool HasPath => path.IsValid && segmentIndex < path.Count;
    public bool HasArrived => arrived;
    public bool GaveUp => givenUp;

    /// <summary>Sets the destination. Moves shorter than destinationMoveThreshold keep the current path.</summary>
    public void SetDestination(Vector3 worldPosition)
    {
        if (hasDestination && (worldPosition - destination).sqrMagnitude < destinationMoveThreshold * destinationMoveThreshold)
        {
            destination = worldPosition;
            return;
        }

        destination = worldPosition;
        hasDestination = true;
        givenUp = false;
        arrived = false;
        consecutiveFailures = 0;

        ClearPath();
    }

    /// <summary>Clears the destination and path.</summary>
    public void Stop()
    {
        hasDestination = false;
        arrived = false;
        ClearPath();
    }

    /// <summary>Advances path following and returns what to hold this tick.</summary>
    public NavIntent Tick()
    {
        intent = NavIntent.None;

        // Hitstun clears the path and delays re-planning.
        if (IsInHitStun())
        {
            ClearPath();
            arrived = false;
            nextPlanAllowedTime = Time.time + reengageDelay;
            return intent;
        }

        if (navGraph == null || navGraph.Navigator == null || curMovementData == null || !hasDestination || givenUp)
            return intent;

        // Once arrived, stays put unless moved more than twice the destination tolerance away.
        if (arrived)
        {
            if (Mathf.Abs(destination.x - transform.position.x) <= destinationTolerance * 2f)
                return intent;

            arrived = false;
        }

        if (!HasPath && !TryPlan())
            return intent;

        FollowCurrentSegment();

        return intent;
    }

    #endregion


    #region Initialize

    private void Awake()
    {
        actionState = GetComponent<IActionState>();
        statHandler = GetComponent<IStats>();
        ownCollider = GetComponent<Collider>();
        body = GetComponent<Rigidbody>();

        if (navGraph == null)
            Debug.LogError("NavAgent has no LevelNavGraph assigned.", this);

        RegisterToEvents();
    }

    private void Start()
    {
        ResetProgress();
    }

    private void OnDestroy()
    {
        UnregisterFromEvents();
    }

    private void RegisterToEvents()
    {
        statHandler.MovementStatsChangedEvent += OnMovementStatsChanged;
    }

    private void UnregisterFromEvents()
    {
        statHandler.MovementStatsChangedEvent -= OnMovementStatsChanged;
    }

    private void OnMovementStatsChanged(MovementStatData movementData)
    {
        curMovementData = movementData;
        BuildProfile();
    }

    /// <summary>Rebuilds the profile from the current movement stats.</summary>
    private void BuildProfile()
    {
        profile = NavAgentProfile.FromHeight(ownCollider != null ? ownCollider.bounds.size.y : 2f);

        if (curMovementData == null)
        {
            profile.CanWalk = false;
            profile.CanClimb = false;
            return;
        }

        profile.CanWalk = curMovementData.GroundedMovementValid;
        profile.CanClimb = curMovementData.LedgeClimbValid;
        profile.HasGroundJump = curMovementData.GroundedJumpValid;

        profile.Airborne = new AirborneEnvelope
        {
            Valid = true,

            LaunchVelocity = curMovementData.JumpVelocity,
            MaxRisingVelocity = curMovementData.MaxAerialRisingVelocity,
            RisingDecceleration = curMovementData.AerialRisingDecceleration,
            GravityRaising = curMovementData.GravityRaising,
            GravityFalling = curMovementData.GravityFalling,
            MaxFallVelocity = curMovementData.MaxFallVelocity,

            LaunchXVelocity = curMovementData.MaxGroundedVelocity,
            MaxAerialXVelocity = curMovementData.MaxAerialXVelocity,
            AerialXAcceleration = curMovementData.AerialXAcceleration,
            AerialXDecceleration = curMovementData.AerialXDecceleration,
        };
    }

    #endregion


    #region Planning

    private bool TryPlan()
    {
        if (Time.time < nextPlanAllowedTime)
            return false;

        nextPlanAllowedTime = Time.time + replanCooldown;

        if (navGraph.Navigator.TryFindPath(transform.position, destination, profile, path))
        {
            segmentIndex = 0;
            consecutiveFailures = 0;
            ResetProgress();

            if (logNavigation)
                Debug.Log($"{name}: planned {path.Count} segments ({path.CountOfType(NavLinkType.Climb)} climbs, " +
                          $"{path.CountOfType(NavLinkType.Jump)} jumps, {path.CountOfType(NavLinkType.Fall)} falls).", this);

            return true;
        }

        path.Clear();

        if (logNavigation)
            Debug.Log($"{name}: no path from {transform.position} to {destination}. " +
                      $"Height {profile.Height:F2}, walk {profile.CanWalk}, climb {profile.CanClimb} (limit {profile.ClimbLimit:F2}), " +
                      $"jump {profile.CanJump}, fall {profile.CanFall}, apex {profile.Airborne.ApexHeight:F2}, " +
                      $"reach at level {profile.Airborne.MaxJumpReach(0f):F2}.", this);

        RegisterFailure("no path found");
        return false;
    }

    private void RegisterFailure(string reason)
    {
        consecutiveFailures++;

        if (consecutiveFailures < maxConsecutiveFailures)
            return;

        givenUp = true;
        Debug.Log($"{name}: NavAgent gave up after {consecutiveFailures} attempts ({reason}).", this);
    }

    private void ClearPath()
    {
        path.Clear();
        segmentIndex = 0;
        phase = TraversalPhase.Approach;
        ResetProgress();
    }

    #endregion


    #region Following

    private void FollowCurrentSegment()
    {
        NavSegment segment = path[segmentIndex];

        switch (segment.Type)
        {
            case NavLinkType.Jump:
                FollowJumpSegment(segment);
                break;

            case NavLinkType.Fall:
                FollowFallSegment(segment);
                break;

            default:
                FollowGroundSegment(segment);
                break;
        }
    }

    /// <summary>Holds direction until the end of a walk or climb segment is reached.</summary>
    private void FollowGroundSegment(NavSegment segment)
    {
        if (HasReachedSegmentEnd(segment))
        {
            AdvanceSegment();
            return;
        }

        if (IsStuck())
        {
            RegisterFailure("no progress toward the current segment");
            ClearPath();
            return;
        }

        intent.Horizontal = GroundSteering(segment);
    }

    private float GroundSteering(NavSegment segment)
    {
        // Climbs keep pressing into the ledge until the segment completes.
        if (segment.Type == NavLinkType.Climb && Mathf.Abs(segment.ToPosition.x - transform.position.x) <= segmentTolerance)
            return Mathf.Sign(segment.ToPosition.x - segment.FromPosition.x);

        return SteerToward(segment.ToPosition.x);
    }

    private bool HasReachedSegmentEnd(NavSegment segment)
    {
        bool isFinalSegment = segmentIndex == path.Count - 1;
        float horizontalTolerance = isFinalSegment ? destinationTolerance : segmentTolerance;

        if (Mathf.Abs(segment.ToPosition.x - transform.position.x) > horizontalTolerance)
            return false;

        return IsOnSegmentSurface(segment);
    }

    /// <summary>Whether the agent's feet are on the segment's destination surface.</summary>
    private bool IsOnSegmentSurface(NavSegment segment)
    {
        return Mathf.Abs(FeetY - navGraph.Grid.SurfaceY(segment.ToNode)) <= verticalTolerance;
    }

    private void AdvanceSegment()
    {
        segmentIndex++;
        phase = TraversalPhase.Approach;
        ResetProgress();

        if (segmentIndex >= path.Count)
        {
            ClearPath();
            arrived = true;
        }
    }

    #endregion


    #region Shared Helpers

    private bool IsInHitStun()
    {
        return actionState != null && actionState.CurActionState == ActionState.HitStun;
    }

    private bool IsGrounded()
    {
        return actionState == null || actionState.CurGroundedState == GroundedState.Grounded;
    }

    private float FeetY => ownCollider != null ? ownCollider.bounds.min.y : transform.position.y;

    private float SteerToward(float targetX)
    {
        float delta = targetX - transform.position.x;

        if (Mathf.Abs(delta) <= 0.01f)
            return 0f;

        return Mathf.Sign(delta);
    }

    /// <summary>X to steer toward in the air: the end of the following walk segment, or the landing cell if there is none.</summary>
    private float AirborneTargetX(NavSegment segment)
    {
        int next = segmentIndex + 1;

        if (next < path.Count && path[next].Type == NavLinkType.Walk)
            return path[next].ToPosition.x;

        return segment.ToPosition.x;
    }

    #endregion


    #region Jump Execution

    /// <summary>Flies a jump segment through its approach, committed and airborne phases.</summary>
    private void FollowJumpSegment(NavSegment segment)
    {
        bool grounded = IsGrounded();

        if (phase == TraversalPhase.Airborne)
        {
            if (grounded)
            {
                ResolveJumpLanding(segment);
                return;
            }

            // Jump is held for the whole arc.
            intent.HoldJump = true;
            intent.Horizontal = SteerToward(AirborneTargetX(segment));
            return;
        }

        if (!grounded)
        {
            phase = TraversalPhase.Airborne;
            intent.HoldJump = true;
            intent.Horizontal = SteerToward(AirborneTargetX(segment));
            return;
        }

        // Jump squat: hold jump and the run direction.
        if (phase == TraversalPhase.Committed)
        {
            intent.HoldJump = true;
            intent.Horizontal = runningLaunch ? Mathf.Sign(segment.ToPosition.x - segment.FromPosition.x) : 0f;
            return;
        }

        ApproachLaunch(segment);
    }

    /// <summary>Chooses a standing or running launch and approaches the launch point.</summary>
    private void ApproachLaunch(NavSegment segment)
    {
        if (IsStuck())
        {
            RegisterFailure("no progress approaching the launch point");
            ClearPath();
            return;
        }

        float dx = segment.ToPosition.x - segment.FromPosition.x;
        float dy = segment.ToPosition.y - segment.FromPosition.y;
        float directionX = Mathf.Sign(dx);
        float speed = body != null ? Mathf.Abs(body.linearVelocity.x) : 0f;

        runningLaunch = Mathf.Abs(dx) > profile.Airborne.StandingJumpReach(dy) * standingJumpSafety;

        if (runningLaunch)
            RunningApproach(segment, directionX, speed);
        else
            StandingApproach(segment, directionX, speed);
    }

    /// <summary>Runs at the launch point, pressing jump early by the distance covered during jump squat.</summary>
    private void RunningApproach(NavSegment segment, float directionX, float speed)
    {
        float lead = speed * curMovementData.JumpSquatDuration;

        if (Mathf.Abs(segment.FromPosition.x - transform.position.x) <= lead + launchTolerance)
        {
            Commit(directionX);
            return;
        }

        intent.Horizontal = directionX;
    }

    /// <summary>
    /// Moves toward the launch point without turning back, coasts to a stop, then jumps. Stopping
    /// past the launch point on the landing side is accepted.
    /// </summary>
    private void StandingApproach(NavSegment segment, float directionX, float speed)
    {
        // Remaining distance to the launch point along the jump direction.
        float ahead = (segment.FromPosition.x - transform.position.x) * directionX;

        if (ahead > launchTolerance)
        {
            if (ahead > StoppingDistance(speed) + launchTolerance)
            {
                intent.Horizontal = directionX;
                return;
            }

            // Coasting.
            if (speed > standingSpeedThreshold)
                return;

            // Stopped short: creep forward.
            intent.Horizontal = directionX;
            return;
        }

        if (speed > standingSpeedThreshold)
            return;

        Commit(0f);
    }

    /// <summary>Distance to coast to a stop from a given ground speed.</summary>
    private float StoppingDistance(float speed)
    {
        float decceleration = curMovementData.GroundedDecceleration;

        if (decceleration <= 0f)
            return 0f;

        return (speed * speed) / (2f * decceleration);
    }

    private void Commit(float directionX)
    {
        phase = TraversalPhase.Committed;
        intent.HoldJump = true;
        intent.Horizontal = directionX;
        ResetProgress();
    }

    /// <summary>Advances when the jump landed on target, otherwise re-plans.</summary>
    private void ResolveJumpLanding(NavSegment segment)
    {
        phase = TraversalPhase.Approach;

        if (LandedPastJumpTarget(segment))
        {
            AdvanceSegment();
            return;
        }

        RegisterFailure("landed short of the jump target");
        ClearPath();
    }

    /// <summary>Whether the agent is on the landing surface at or past the landing cell.</summary>
    private bool LandedPastJumpTarget(NavSegment segment)
    {
        if (!IsOnSegmentSurface(segment))
            return false;

        float jumpDirection = Mathf.Sign(segment.ToPosition.x - segment.FromPosition.x);
        float pastLanding = (transform.position.x - segment.ToPosition.x) * jumpDirection;

        return pastLanding >= -segmentTolerance;
    }

    #endregion


    #region Fall Execution

    /// <summary>Walks off the ledge, holds outward until below it, then steers toward the next waypoint.</summary>
    private void FollowFallSegment(NavSegment segment)
    {
        float fallDirection = Mathf.Sign(segment.ToPosition.x - segment.FromPosition.x);

        if (phase == TraversalPhase.Airborne)
        {
            if (IsGrounded())
            {
                ResolveFallLanding(segment);
                return;
            }

            intent.Horizontal = IsBelowLedge(segment) ? SteerToward(AirborneTargetX(segment)) : fallDirection;
            return;
        }

        if (!IsGrounded())
        {
            phase = TraversalPhase.Airborne;
            intent.Horizontal = fallDirection;
            return;
        }

        if (IsStuck())
        {
            RegisterFailure("no progress walking off the ledge");
            ClearPath();
            return;
        }

        intent.Horizontal = fallDirection;
    }

    /// <summary>Whether the agent's feet are below the ledge it left.</summary>
    private bool IsBelowLedge(NavSegment segment)
    {
        return FeetY < navGraph.Grid.SurfaceY(segment.FromNode) - navGraph.Grid.Spacing;
    }

    /// <summary>Advances when the agent landed on the expected surface, otherwise re-plans.</summary>
    private void ResolveFallLanding(NavSegment segment)
    {
        phase = TraversalPhase.Approach;

        if (IsOnSegmentSurface(segment))
        {
            AdvanceSegment();
            return;
        }

        RegisterFailure("fell somewhere other than the fall target");
        ClearPath();
    }

    #endregion


    #region Stuck Detection

    private bool IsStuck()
    {
        if ((transform.position - lastProgressPosition).sqrMagnitude > stuckProgressThreshold * stuckProgressThreshold)
        {
            ResetProgress();
            return false;
        }

        return Time.time - lastProgressTime > stuckTimeout;
    }

    private void ResetProgress()
    {
        lastProgressTime = Time.time;
        lastProgressPosition = transform.position;
    }

    #endregion


    #region Gizmos

    private void OnDrawGizmosSelected()
    {
        DrawEnvelope();
        DrawPath();
    }

    private void DrawPath()
    {
        if (!drawPath || !path.IsValid)
            return;

        for (int i = 0; i < path.Count; i++)
        {
            NavSegment segment = path[i];

            Gizmos.color = NavLinkColors.For(segment.Type);
            Gizmos.DrawLine(segment.FromPosition, segment.ToPosition);

            if (i == segmentIndex)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawWireSphere(segment.ToPosition, 0.3f);
            }
        }

        if (hasDestination)
        {
            Gizmos.color = givenUp ? Color.red : Color.green;
            Gizmos.DrawWireSphere(destination, 0.35f);
        }
    }

    /// <summary>Draws the reachable jump boundary from the agent's position, with the apex marked in white.</summary>
    private void DrawEnvelope()
    {
        if (!drawJumpEnvelope || !profile.CanJump)
            return;

        Vector3 feet = new Vector3(transform.position.x, FeetY, 0f);

        const int steps = 24;
        float apex = profile.Airborne.ApexHeight;
        float top = apex - 0.25f;
        float bottom = -Mathf.Abs(envelopeDropDepth);

        Vector3 previousRight = Vector3.zero;
        Vector3 previousLeft = Vector3.zero;

        for (int i = 0; i <= steps; i++)
        {
            float dy = Mathf.Lerp(bottom, top, i / (float)steps);
            float reach = profile.Airborne.MaxJumpReach(dy);

            Vector3 right = feet + new Vector3(reach, dy, 0f);
            Vector3 left = feet + new Vector3(-reach, dy, 0f);

            if (i > 0)
            {
                Gizmos.color = NavLinkColors.For(NavLinkType.Jump);
                Gizmos.DrawLine(previousRight, right);
                Gizmos.DrawLine(previousLeft, left);
            }

            previousRight = right;
            previousLeft = left;
        }

        // Standing jump reach at launch height.
        float standing = profile.Airborne.StandingJumpReach(0f);
        Gizmos.color = new Color(1f, 0.85f, 0.2f);
        Gizmos.DrawLine(feet, feet + new Vector3(standing, 0f, 0f));
        Gizmos.DrawLine(feet, feet + new Vector3(-standing, 0f, 0f));

        Gizmos.color = Color.white;
        Gizmos.DrawLine(feet + new Vector3(-0.4f, apex, 0f), feet + new Vector3(0.4f, apex, 0f));
    }

    #endregion
}
