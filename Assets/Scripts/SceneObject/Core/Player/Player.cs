using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.SceneObjects 
{    
    internal class Player : SceneObject, IMovementInput, IAttackInput, IInteractionInput, IEquipmentInput
    {
        private PlayerInputHandler inputHandler;

        public event Action<Vector2> MovementPerformedEvent;
        public event Action MovementStoppedEvent;
        public event Action<float> JumpPerformedEvent;
        public event Action JumpStoppedEvent;

        public event Action UpAttackPerformedEvent;
        public event Action DownAttackPerformedEvent;
        public event Action LeftAttackPerformedEvent;
        public event Action RightAttackPerformedEvent;

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

            inputHandler.input.PlayerActions.RightAttack.performed += AttackRightInput;
            inputHandler.input.PlayerActions.LeftAttack.performed += AttackLeftInput;
            inputHandler.input.PlayerActions.DownAttack.performed += AttackDownwardInput;
            inputHandler.input.PlayerActions.UpAttack.performed += AttackUpwardInput;

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

        //Active Movement Inputs
        /// <inheritdoc/>
        public bool IsHorizontalMovementActive()
        {
            return inputHandler.input.PlayerActions.Movement.IsPressed();
        }

        /// <inheritdoc/>
        public bool IsVerticalJumpActive()
        {
            return inputHandler.input.PlayerActions.Jump.IsPressed();
        }

        #endregion


        #region Attack Input

        //Upward Attack
        private void AttackUpwardInput(InputAction.CallbackContext obj)
        {
            UpAttackPerformedEvent?.Invoke();
        }

        //Downward Attack
        private void AttackDownwardInput(InputAction.CallbackContext obj)
        {
            DownAttackPerformedEvent?.Invoke();
        }

        //Left Attack
        private void AttackLeftInput(InputAction.CallbackContext obj)
        {
            LeftAttackPerformedEvent?.Invoke();
        }

        //Right Attack
        private void AttackRightInput(InputAction.CallbackContext obj)
        {
            RightAttackPerformedEvent?.Invoke();
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
    }
}

