using UnityEngine;

/// <summary>
/// Base enemy. Owns the input buffer and decides where the enemy goes. Movement and attack
/// capability come from sibling components.
/// </summary>
internal class Enemy : SceneObject, IInputBuffer
{
    [Header("Input Buffer")]
    [Tooltip("How long a press stays live in the buffer, in seconds.")]
    [SerializeField] private float inputBufferWindow = 0.2f;
    private InputBuffer inputBuffer;

    [Header("Debug Target")]
    [Tooltip("Transform the enemy paths to.")]
    [SerializeField] private Transform debugTarget;

    // Optional Components
    private EnemyMovementInput movementInput;
    private NavAgent navAgent;

    private bool jumpHeld = false;

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
    }

    #endregion


    #region Brain

    protected virtual void FixedUpdate()
    {
        Think();
    }

    /// <summary>Paths to the debug target and performs the returned intent.</summary>
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
            navAgent.Stop();

        PerformIntent(navAgent.Tick());
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
        if (debugTarget == null)
            return;

        Gizmos.color = Color.grey;
        Gizmos.DrawLine(transform.position, debugTarget.position);
    }

    #endregion
}
