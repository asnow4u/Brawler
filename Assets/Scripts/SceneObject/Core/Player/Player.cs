using System;
using UnityEngine;
using UnityEngine.InputSystem;

internal class Player : SceneObject, IMovementInput, IAttackInput, IInteractionInput, IEquipmentInput, IInputBuffer, IMovementInputEditor, IAttackInputEditor
{
    private PlayerInputHandler inputHandler;

    [Header("Input Buffer")]
    [Tooltip("How long a press stays live while the object cannot act on it.")]
    [SerializeField] private float inputBufferWindow = 0.2f;
    private InputBuffer inputBuffer;

    [Header("Input Deadzone")]
    [Range(0, 1)]
    [SerializeField] private float horizontalDeadzone = 0.2f;
    [Range(0, 1)]
    [SerializeField] private float verticalDeadzone = 0.2f;
    [Range(0, 1)]
    [SerializeField] private float jumpDeadzone = 0.1f;
    private Vector2 rawMovement;
    private float rawJump;

    public float HorizontalInfluence => Mathf.Abs(rawMovement.x) > horizontalDeadzone ? Mathf.Clamp(rawMovement.x, -1f, 1f) : 0f;
    public float VerticalInfluence => Mathf.Abs(rawMovement.y) > verticalDeadzone ? Mathf.Clamp(rawMovement.y, -1f, 1f) : 0f;
    public float JumpInfluence => rawJump > jumpDeadzone ? Mathf.Clamp01(rawJump) : 0f;

    //Attack
    private const float ATTACK_INPUT_THRESHOLD = 0.7f;
    private const float ATTACK_INPUT_RESET_THRESHOLD = 0.2f;
    private bool attackInputTriggered = false; // NOTE: Prevent multi buffering off of a single attack.


    #region Initialize

    protected override void Awake()
    {
        base.Awake();
        InitializeInput();
    }

    private void InitializeInput()
    {
        inputHandler = new PlayerInputHandler();
        inputBuffer = new InputBuffer(inputBufferWindow);

        inputHandler.input.PlayerActions.Movement.performed += MovementInput;
        inputHandler.input.PlayerActions.Movement.canceled += MovementCanceled;
        inputHandler.input.PlayerActions.Jump.performed += JumpInput;
        inputHandler.input.PlayerActions.Jump.canceled += JumpCanceled;
        inputHandler.input.PlayerActions.Dash.performed += DashInput;

        inputHandler.input.PlayerActions.Attack.performed += AttackInput;
        inputHandler.input.PlayerActions.Attack.canceled += AttackCanceled;

        inputHandler.input.PlayerActions.Interaction.performed += InteractInput;

        inputHandler.input.PlayerActions.WeaponSwitch.performed += ToggleWeapon;
    }

    private void OnDisable()
    {
        inputHandler.DisableInputEvents();
    }

    #endregion


    #region Movement Inputs

    private void MovementInput(InputAction.CallbackContext obj)
    {
        rawMovement = obj.ReadValue<Vector2>();
    }

    private void MovementCanceled(InputAction.CallbackContext obj)
    {
        rawMovement = Vector2.zero;
    }

    private void JumpInput(InputAction.CallbackContext obj)
    {
        rawJump = obj.ReadValue<float>();

        if (rawJump > jumpDeadzone)
            inputBuffer.Buffer(BufferedInput.Jump, rawJump);
    }

    private void JumpCanceled(InputAction.CallbackContext obj)
    {
        rawJump = 0f;
    }

    private void DashInput(InputAction.CallbackContext obj)
    {
        inputBuffer.Buffer(BufferedInput.Dash);
    }

    #endregion


    #region Attack Input

    private void AttackInput(InputAction.CallbackContext obj)
    {
        var direction = obj.ReadValue<Vector2>();

        if (direction.magnitude < ATTACK_INPUT_RESET_THRESHOLD)
        {
            attackInputTriggered = false;
            return;
        }

        if (!attackInputTriggered && direction.magnitude > ATTACK_INPUT_THRESHOLD)
        {
            attackInputTriggered = true;
            inputBuffer.Buffer(BufferedInput.Attack, direction.magnitude, direction);
        }
    }

    private void AttackCanceled(InputAction.CallbackContext obj)
    {
        attackInputTriggered = false;
    }

    #endregion


    #region Interaction Input Events

    private void InteractInput(InputAction.CallbackContext obj)
    {
    }

    #endregion


    #region Equipment Input

    private void ToggleWeapon(InputAction.CallbackContext obj)
    {
        inputBuffer.Buffer(BufferedInput.SwapWeapon);
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

    public void DebugMovementInput(Vector2 movementInput)
    {
        rawMovement = movementInput;
    }

    public void DebugJumpInput(float jumpInput)
    {
        rawJump = jumpInput;

        if (rawJump > jumpDeadzone)
            inputBuffer.Buffer(BufferedInput.Jump, rawJump);
    }

    public void DebugAttackInput(Vector2 direction)
    {            
        if (direction.magnitude > ATTACK_INPUT_THRESHOLD)
            inputBuffer.Buffer(BufferedInput.Attack, direction.magnitude, direction);
    }

    #endregion
}
