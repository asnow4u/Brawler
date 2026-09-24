using UnityEngine;

/// <summary>
/// Base enemy brain. Runs awareness and the state machine, and moves through a NavAgent when one is
/// present. Subclasses decide what the enemy does once engaged.
///
/// Engages the player on sight within range of itself, searches the last known position when sight
/// is lost or when hit, then returns home. Holds a chase until the player leaves the disengage range
/// of its home. With Home Mode None both radii are measured from the enemy and it stops where it is.
/// </summary>
internal abstract partial class Enemy : SceneObject
{
    protected enum EnemyState { Idle, Engaged, Searching, Returning }

    private static readonly Color EngageColor = Color.yellow;
    private static readonly Color DisengageColor = new Color(1f, 0.5f, 0f);
    private static readonly Color SearchColor = new Color(1f, 0.6f, 0f);

    [Header("Home")]
    [SerializeField] private EnemyHomeMode homeMode = EnemyHomeMode.Post;
    [Tooltip("Used when Home Mode is Patrol.")]
    [SerializeField] private PatrolSettings patrol = new PatrolSettings();

    [Header("Awareness")]
    [Tooltip("Distance from the enemy at which the player is engaged, in world units.")]
    [SerializeField] private float engageRadius = 12f;
    [Tooltip("Distance from home at which a chase is released, in world units.")]
    [SerializeField] private float disengageRadius = 18f;
    [Tooltip("How long the enemy waits at the player's last known position before returning, in seconds.")]
    [SerializeField] private float searchWaitDuration = 1f;
    [Tooltip("Layers that block sight.")]
    [SerializeField] private LayerMask sightBlockingMask;

    [Header("Debug")]
    [Tooltip("When set, the enemy paths to this instead of running its behavior.")]
    [SerializeField] private Transform debugTarget;
    [SerializeField] private bool drawAwareness = false;

    // Optional Components
    private NavAgent navAgent;
    private IEnemyHome home;
    private IActionState actionState;
    private IAttack attack;

    private Player player;
    private EnemyState state = EnemyState.Idle;

    private Vector3 lastKnownPlayerPosition;
    private bool waitingAtLastKnown;
    private float searchWaitStartTime;

    private bool wasInHitStun;

    protected EnemyState State => state;
    protected Vector3 LastKnownPlayerPosition => lastKnownPlayerPosition;
    protected bool CanNavigate => navAgent != null;
    protected bool IsAttacking => attack != null && attack.CurAttackState != AttackState.Null;
    protected bool IsGrounded => actionState == null || actionState.CurGroundedState == GroundedState.Grounded;


    #region Initialize

    protected override void Awake()
    {
        base.Awake();

        InitializeInput();

        // Optional Components
        navAgent = GetComponent<NavAgent>();
        actionState = GetComponent<IActionState>();
        attack = GetComponent<IAttack>();

        home = CreateHome();
    }

    private void Reset()
    {
        sightBlockingMask = LayerMask.GetMask("Environment");
    }

    protected virtual void Start()
    {
        home?.Initialize(transform.position);
    }

    private IEnemyHome CreateHome()
    {
        switch (homeMode)
        {
            case EnemyHomeMode.Post:
                return new EnemyPost(transform);

            case EnemyHomeMode.Patrol:
                return new EnemyPatrol(transform, patrol);

            default:
                return null;
        }
    }

    #endregion


    #region Brain

    protected virtual void FixedUpdate()
    {
        Think();
    }

    /// <summary>Runs the behavior, then applies the navigation intent to movement input.</summary>
    protected virtual void Think()
    {
        if (debugTarget != null && CanNavigate)
        {
            navAgent.SetDestination(debugTarget.position);
        }
        else
        {
            CheckForHit();
            UpdateState();
            ApplyState();
        }

        if (CanNavigate)
            ApplyNavIntent(navAgent.Tick());
    }

    /// <summary>What the enemy does each tick while engaged with the player.</summary>
    protected abstract void OnEngaged(Vector3 playerCenter);

    /// <summary>Marks where the enemy was hit as the last known position and starts searching there.</summary>
    private void CheckForHit()
    {
        bool inHitStun = actionState != null && actionState.CurActionState == ActionState.HitStun;

        if (inHitStun && !wasInHitStun)
            BeginSearch(transform.position);

        wasInHitStun = inHitStun;
    }

    /// <summary>Engages on sight, drops to searching when sight is lost, then runs the search.</summary>
    private void UpdateState()
    {
        if (CanEngagePlayer())
        {
            lastKnownPlayerPosition = player.Bounds.center;
            waitingAtLastKnown = false;
            state = EnemyState.Engaged;
            return;
        }

        if (state == EnemyState.Engaged)
        {
            BeginSearch(lastKnownPlayerPosition);
            return;
        }

        if (state == EnemyState.Searching)
            UpdateSearch();
    }

    private void BeginSearch(Vector3 position)
    {
        lastKnownPlayerPosition = position;
        waitingAtLastKnown = false;
        state = EnemyState.Searching;
    }

    /// <summary>Waits at the last known position once it is reached, then returns home.</summary>
    private void UpdateSearch()
    {
        if (!waitingAtLastKnown)
        {
            if (!HasFinishedMoving())
                return;

            waitingAtLastKnown = true;
            searchWaitStartTime = Time.time;
            return;
        }

        if (Time.time - searchWaitStartTime > searchWaitDuration)
            state = EnemyState.Returning;
    }

    private void ApplyState()
    {
        switch (state)
        {
            case EnemyState.Engaged:
                ApplyEngaged();
                break;

            case EnemyState.Searching:
                ApplySearching();
                break;

            case EnemyState.Returning:
                ApplyReturning();
                break;

            default:
                ApplyIdle();
                break;
        }
    }

    private void ApplyEngaged()
    {
        if (player == null)
        {
            StopMoving();
            return;
        }

        OnEngaged(player.Bounds.center);
    }

    private void ApplySearching()
    {
        if (waitingAtLastKnown)
            StopMoving();
        else
            MoveTo(lastKnownPlayerPosition);
    }

    /// <summary>Paths home, and adopts the current position as home when it cannot be reached.</summary>
    private void ApplyReturning()
    {
        if (home == null || !CanNavigate)
        {
            state = EnemyState.Idle;
            return;
        }

        if (navAgent.GaveUp)
        {
            home.ReHome(transform.position);
            state = EnemyState.Idle;
            return;
        }

        navAgent.SetDestination(home.GetReturnDestination(transform.position));

        if (navAgent.HasArrived)
            state = EnemyState.Idle;
    }

    private void ApplyIdle()
    {
        if (!CanNavigate)
            return;

        if (home != null && home.TryGetIdleDestination(transform.position, navAgent.HasArrived, navAgent.GaveUp, out Vector3 destination))
        {
            navAgent.SetDestination(destination);
            return;
        }

        navAgent.Stop();
    }

    #endregion


    #region Movement Helpers

    protected void MoveTo(Vector3 destination)
    {
        if (CanNavigate)
            navAgent.SetDestination(destination);
    }

    protected void StopMoving()
    {
        if (CanNavigate)
            navAgent.Stop();
    }

    /// <summary>True once the agent has arrived, given up, or when there is no agent at all.</summary>
    private bool HasFinishedMoving()
    {
        return !CanNavigate || navAgent.HasArrived || navAgent.GaveUp;
    }

    /// <summary>Applies a navigation intent to movement input, pressing or releasing jump when HoldJump changes.</summary>
    private void ApplyNavIntent(NavIntent intent)
    {
        SetMovement(new Vector2(intent.Horizontal, intent.Vertical));

        if (intent.HoldJump == IsJumpHeld)
            return;

        if (intent.HoldJump)
            PressJump();
        else
            ReleaseJump();
    }

    #endregion


    #region Combat Helpers

    /// <summary>Up, down or forward toward a target, from its position relative to the enemy.</summary>
    protected Vector2 AttackDirectionTo(Vector3 targetCenter, float verticalThreshold)
    {
        float verticalOffset = targetCenter.y - Bounds.center.y;

        if (verticalOffset > verticalThreshold)
            return Vector2.up;

        if (verticalOffset < -verticalThreshold)
            return Vector2.down;

        return targetCenter.x >= Bounds.center.x ? Vector2.right : Vector2.left;
    }

    protected bool IsWithinOfSelf(Vector3 position, float radius)
    {
        return Vector3.Distance(Bounds.center, position) <= radius;
    }

    #endregion


    #region Awareness

    /// <summary>
    /// Whether the player is in range and visible. Range is measured from the enemy, and also from
    /// home while already pursuing.
    /// </summary>
    private bool CanEngagePlayer()
    {
        if (!TryGetPlayer())
            return false;

        Vector3 playerCenter = player.Bounds.center;
        bool pursuing = state == EnemyState.Engaged || state == EnemyState.Searching;

        bool inRange = IsWithinOfSelf(playerCenter, engageRadius)
                    || (pursuing && IsWithinOfHome(playerCenter, disengageRadius));

        return inRange && HasSightTo(playerCenter);
    }

    private bool TryGetPlayer()
    {
        if (player != null)
            return true;

        player = FindFirstObjectByType<Player>();
        return player != null;
    }

    /// <summary>Distance is measured from home, or from the enemy when it has none.</summary>
    private bool IsWithinOfHome(Vector3 position, float radius)
    {
        float distance = home != null
            ? home.DistanceFrom(position)
            : Vector3.Distance(Bounds.center, position);

        return distance <= radius;
    }

    private bool HasSightTo(Vector3 position)
    {
        return !Physics.Linecast(Bounds.center, position, sightBlockingMask, QueryTriggerInteraction.Ignore);
    }

    #endregion


    #region Debug

    private void OnDrawGizmosSelected()
    {
        if (debugTarget != null)
        {
            Gizmos.color = Color.grey;
            Gizmos.DrawLine(transform.position, debugTarget.position);
        }

        if (!drawAwareness)
            return;

        DrawAwarenessRadii();
        DrawPlayerLink();
        DrawArchetypeGizmos();
    }

    /// <summary>Gizmos specific to a subclass, drawn when awareness gizmos are on.</summary>
    protected virtual void DrawArchetypeGizmos() { }

    private void DrawAwarenessRadii()
    {
        Gizmos.color = EngageColor;
        Gizmos.DrawWireSphere(transform.position, engageRadius);

        IEnemyHome gizmoHome = home ?? CreateHome();

        if (gizmoHome != null)
        {
            gizmoHome.DrawHomeGizmos(disengageRadius);
            return;
        }

        Gizmos.color = DisengageColor;
        Gizmos.DrawWireSphere(transform.position, disengageRadius);
    }

    private void DrawPlayerLink()
    {
        if (!Application.isPlaying || player == null)
            return;

        Gizmos.color = state == EnemyState.Engaged ? Color.red
                     : state == EnemyState.Searching ? SearchColor
                     : Color.grey;
        Gizmos.DrawLine(Bounds.center, player.Bounds.center);

        if (state != EnemyState.Searching)
            return;

        Gizmos.color = SearchColor;
        Gizmos.DrawWireSphere(lastKnownPlayerPosition, 0.4f);
    }

    #endregion
}
