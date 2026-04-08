using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.SceneObjects 
{    
    internal class Player : SceneObject, IMovementInput, IAttackInput, IInteractionInput, IEquipmentInput, IMovementInputEditor, IAttackInputEditor
    {
        private PlayerInputHandler inputHandler;

        //Movement
        public event Action<Vector2> MovementPerformedEvent;
        public event Action MovementStoppedEvent;
        public event Action<float> JumpPerformedEvent;
        public event Action JumpStoppedEvent;

        //Attack
        private const float ATTACK_INPUT_THRESHOLD = 0.7f;
        private const float ATTACK_INPUT_RESET_THRESHOLD = 0.2f;
        private bool attackInputTriggered = false;
        public event Action<Vector2> AttackPerformedEvent;        

        public event Action InteractionPerformedEvent;

        public event Action ToggleEquippedWeaponEvent;

        #region Initialize

        protected override void Awake()
        {
            base.Awake();
            InitializeInput();
        }

        private void InitializeInput()
        {
            inputHandler = new PlayerInputHandler();

            inputHandler.input.PlayerActions.Movement.performed += MovementInput;
            inputHandler.input.PlayerActions.Movement.canceled += MovementCanceled;
            inputHandler.input.PlayerActions.Jump.performed += JumpInput;
            inputHandler.input.PlayerActions.Jump.canceled += JumpCanceled;

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
            MovementPerformedEvent?.Invoke(obj.ReadValue<Vector2>());
        }

        private void MovementCanceled(InputAction.CallbackContext obj)
        {
            MovementStoppedEvent?.Invoke();
        }

        //Vertical Jump
        
        private void JumpInput(InputAction.CallbackContext obj)
        {
            JumpPerformedEvent?.Invoke(obj.ReadValue<float>());
        }

        private void JumpCanceled(InputAction.CallbackContext obj)
        {
            JumpStoppedEvent?.Invoke();
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
                AttackPerformedEvent?.Invoke(direction);
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
            InteractionPerformedEvent?.Invoke();
        }

        #endregion


        #region Equipment Input Input

        private void ToggleWeapon(InputAction.CallbackContext obj)
        {
            ToggleEquippedWeaponEvent?.Invoke();
        }


        #endregion


        #region Debug

        public void DebugMovementInput(Vector2 movementInput)
        {
            if (movementInput.magnitude > 0)
                MovementPerformedEvent?.Invoke(movementInput);

            else
                MovementStoppedEvent?.Invoke();
        }

        public void DebugJumpInput(float jumpInput)
        {
            if (jumpInput > 0)
                JumpPerformedEvent?.Invoke(jumpInput);
            else
                JumpStoppedEvent?.Invoke();
        }

        public void DebugAttackInput(Vector2 direction)
        {            
            if (direction.magnitude > ATTACK_INPUT_THRESHOLD)
                AttackPerformedEvent?.Invoke(direction);
        }

        #endregion
    }
}

