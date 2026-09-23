using UnityEngine;

/// <summary>
/// Base enemy. Owns the input buffer, decides where the enemy goes, and performs the result.
/// Movement, attack and home behavior come from sibling components.
///
/// Engages the player on sight within range of itself, chases, searches the last known position,
/// then returns home. It holds the chase until the player leaves the disengage range of its home.
/// With no home component both radii are measured from the enemy and it stops where it is.
/// </summary>
internal class Enemy : SceneObject, IInputBuffer
{
    private enum EnemyState { Idle, Engaged, Searching, Returning }

    private static readonly Color EngageColor = Color.yellow;
    private static readonly Color DisengageColor = new Color(1f, 0.5f, 0f);
    private static readonly Color SearchColor = new Color(1f, 0.6f, 0f);

    [Header("Input Buffer")]
    [Tooltip("How long a press stays live in the buffer, in seconds.")]
    [SerializeField] private float inputBufferWindow = 0.2f;
    private InputBuffer inputBuffer;

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
    private EnemyMovementInput movementInput;
    private NavAgent navAgent;
    private IEnemyHome home;
    private IActionState actionState;

    private Player player;
    private EnemyState state = EnemyState.Idle;

    private Vector3 lastKnownPlayerPosition;
    private bool waitingAtLastKnown;
    private float searchWaitStartTime;

    private bool wasInHitStun;
    private bool jumpHeld;

    protected bool CanMove => movementInput != null;
    protected bool CanNavigate => navAgent != null;


    #region Initialize

    protected override void Awake()
    {
        base.Awake();

        inputBuffer = new InputBuffer(inputBufferWindow);

        // Optional Components
        movementInput = GetComponent<EnemyMovementInput>();
        navAgent = GetComponent<NavAgent>();
        home = GetComponent<IEnemyHome>();
        actionState = GetComponent<IActionState>();
    }

    private void Reset()
    {
        sightBlockingMask = LayerMask.GetMask("Environment");
    }

    protected virtual void Start()
    {
        home?.Initialize(transform.position);
    }

    #endregion


    #region Brain

    protected virtual void FixedUpdate()
    {
        Think();
    }

    /// <summary>Chooses a destination, then performs the intent the agent returns.</summary>
    protected virtual void Think()
    {
        if (!CanMove)
            return;

        if (!CanNavigate)
        {
            movementInput.SetHorizontal(0f);
            return;
        }

        if (debugTarget != null)
            navAgent.SetDestination(debugTarget.position);
        else
            RunBehavior();

        PerformIntent(navAgent.Tick());
    }

    private void RunBehavior()
    {
        CheckForHit();
        UpdateState();
        ApplyState();
    }

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
            if (!navAgent.HasArrived && !navAgent.GaveUp)
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
                navAgent.SetDestination(lastKnownPlayerPosition);
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

    private void ApplySearching()
    {
        if (waitingAtLastKnown)
            navAgent.Stop();
        else
            navAgent.SetDestination(lastKnownPlayerPosition);
    }

    /// <summary>Paths home, and adopts the current position as home when it cannot be reached.</summary>
    private void ApplyReturning()
    {
        if (home == null)
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
        if (home != null && home.TryGetIdleDestination(transform.position, navAgent.HasArrived, navAgent.GaveUp, out Vector3 destination))
        {
            navAgent.SetDestination(destination);
            return;
        }

        navAgent.Stop();
    }

    /// <summary>Applies an intent to the movement input, pressing or releasing jump when HoldJump changes.</summary>
    private void PerformIntent(NavIntent intent)
    {
        movementInput.SetMovement(new Vector2(intent.Horizontal, intent.Vertical));

        if (intent.HoldJump == jumpHeld)
            return;

        if (intent.HoldJump)
            movementInput.PressJump();
        else
            movementInput.ReleaseJump();

        jumpHeld = intent.HoldJump;
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

    private bool IsWithinOfSelf(Vector3 position, float radius)
    {
        return Vector3.Distance(Bounds.center, position) <= radius;
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


    #region Input Buffer

    public float BufferWindow => inputBuffer.BufferWindow;
    public void Buffer(BufferedInput input, float value = 0f, Vector2 direction = default) => inputBuffer.Buffer(input, value, direction);
    public bool Peek(BufferedInput input) => inputBuffer.Peek(input);
    public bool TryConsume(BufferedInput input) => inputBuffer.TryConsume(input);
    public bool TryConsume(BufferedInput input, out InputRecord record) => inputBuffer.TryConsume(input, out record);
    public float LastPressTime(BufferedInput input) => inputBuffer.LastPressTime(input);
    public bool TryGetNewestLive(BufferedInput[] inputs, out BufferedInput newest) => inputBuffer.TryGetNewestLive(inputs, out newest);
    public void Clear(BufferedInput input) => inputBuffer.Clear(input);
    public void ClearAll() => inputBuffer.ClearAll();

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
    }

    private void DrawAwarenessRadii()
    {
        Gizmos.color = EngageColor;
        Gizmos.DrawWireSphere(transform.position, engageRadius);

        IEnemyHome gizmoHome = home ?? GetComponent<IEnemyHome>();

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
