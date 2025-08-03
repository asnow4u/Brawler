using Game.SceneObjects.Animation;
using Game.SceneObjects.Attack;
using Game.SceneObjects.Equipment;
using Game.SceneObjects.Movement;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.InputSystem;

namespace Game.SceneObjects 
{

    [RequireComponent(typeof(EquipmentHandler))]
    [RequireComponent(typeof(InteractionHandler))]
    [RequireComponent(typeof(MovementInputHandler))]
    [RequireComponent(typeof(AttackInputHandler))]
    [RequireComponent(typeof(AnimationHandler))]
    public class Player : SceneObject
    {
        private float numLives;
        private float numDeaths;

        private PlayerInputHandler inputHandler;

        protected override void Initialize()
        {
            base.Initialize();

            ObjectType = SceneObjectType.Player;

            InitializeInput();
        }

        protected override void GetHandlers()
        {
            base.GetHandlers();

            EquipmentHandler = GetComponent<EquipmentHandler>();
            InteractionHandler = GetComponent<InteractionHandler>();
            MovementInputHandler = GetComponent<MovementInputHandler>();
            AttackInputHandler = GetComponent<AttackInputHandler>();
            AnimationHandler = GetComponent<AnimationHandler>();
        }

        private void InitializeInput()
        {
            inputHandler = new PlayerInputHandler();

            inputHandler.input.PlayerActions.Movement.performed += MovementInput;
            inputHandler.input.PlayerActions.Movement.canceled += MovementCanceled;

            inputHandler.input.PlayerActions.Jump.performed += JumpInput;
            inputHandler.input.PlayerActions.Jump.canceled += JumpCanceled;

            //inputHandler.input.PlayerActions.Couch.performed += Couch_performed;
            //inputHandler.input.PlayerActions.Couch.canceled += Couch_canceled;

            //Attack
            inputHandler.input.PlayerActions.RightAttack.performed += AttackRightInput;
            inputHandler.input.PlayerActions.LeftAttack.performed += AttackLeftInput;
            inputHandler.input.PlayerActions.DownAttack.performed += AttackDownwardInput;
            inputHandler.input.PlayerActions.UpAttack.performed += AttackUpwardInput;

            //Interact
            inputHandler.input.PlayerActions.Interaction.performed += InteractInput;

            //Weapon Switch
            inputHandler.input.PlayerActions.WeaponSwitch.performed += ToggleWeapon;
        }


        private void OnDisable()
        {
            inputHandler.DisableInputEvents();
        }


        #region Movement Inputs

        //Horizontal Movement

        private void MovementInput(InputAction.CallbackContext obj)
        {
            PerformMovement(obj.ReadValue<Vector2>());
        }

        /// <inheritdoc/>
        public override void PerformMovement(Vector2 movement)
        {
            MovementInputHandler.SetMovementInfluence(movement);
        }

        private void MovementCanceled(InputAction.CallbackContext obj)
        {
            StopHorizontalMovement();
        }

        /// <inheritdoc/>
        public override void StopHorizontalMovement()
        {
            MovementInputHandler.SetMovementInfluence(Vector2.zero);
        }

        //Vertical Jump
        
        private void JumpInput(InputAction.CallbackContext obj)
        {
            PerformVerticalJump(obj.ReadValue<float>());
        }

        /// <inheritdoc/>
        public override void PerformVerticalJump(float jumpInfluence)
        {
            MovementInputHandler.SetJumpInfluence(jumpInfluence);
        }

        private void JumpCanceled(InputAction.CallbackContext obj)
        {
            StopJumpMovement();
        }

        /// <inheritdoc/>
        public override void StopJumpMovement()
        {
            MovementInputHandler.SetJumpInfluence(0);
        }

        //Active Movement Inputs
        /// <inheritdoc/>
        public override bool IsHorizontalMovementActive()
        {
            return inputHandler.input.PlayerActions.Movement.IsPressed();
        }

        /// <inheritdoc/>
        public override bool IsVerticalJumpActive()
        {
            return inputHandler.input.PlayerActions.Jump.IsPressed();
        }

        #endregion

        #region Attack Input

        //Upward Attack
        private void AttackUpwardInput(InputAction.CallbackContext obj)
        {
            PerformUpAttack();
        }

        /// <inheritdoc/>
        public override void PerformUpAttack()
        {
            AttackInputHandler.PerformUpAttack();
        }

        //Downward Attack
        private void AttackDownwardInput(InputAction.CallbackContext obj)
        {
            PerformDownAttack();
        }

        /// <inheritdoc/>
        public override void PerformDownAttack()
        {
            AttackInputHandler.PerformDownAttack();
        }

        //Left Attack
        private void AttackLeftInput(InputAction.CallbackContext obj)
        {
            PerformLeftAttack();
        }

        /// <inheritdoc/>
        public override void PerformLeftAttack()
        {
            AttackInputHandler.PerformLeftAttack();
        }

        //Right Attack
        private void AttackRightInput(InputAction.CallbackContext obj)
        {
            PerformRightAttack();           
        }

        /// <inheritdoc/>
        public override void PerformRightAttack()
        {
            AttackInputHandler.PerformForwardAttack();
        }

        //Active Attack Inputs
        /// <inheritdoc/>
        public override bool IsUpAttackActive()
        {
            return inputHandler.input.PlayerActions.UpAttack.IsPressed();
        }

        /// <inheritdoc/>
        public override bool IsDownAttackActive()
        {
            return inputHandler.input.PlayerActions.DownAttack.IsPressed();
        }

        /// <inheritdoc/>
        public override bool IsLeftAttackActive()
        {
            return inputHandler.input.PlayerActions.LeftAttack.IsPressed();
        }

        /// <inheritdoc/>
        public override bool IsRightAttackActive()
        {
            return inputHandler.input.PlayerActions.RightAttack.IsPressed();
        }

        #endregion

        #region Interaction Input Events

        private void InteractInput(InputAction.CallbackContext obj)
        {
            PerformInteraction();
        }

        /// <inheritdoc/>
        public override void PerformInteraction()
        {
            if (ActionStateHandler.CurActionState == ActionStates.ActionState.Idle || ActionStateHandler.CurActionState == ActionStates.ActionState.Moving)
                InteractionHandler.InitiateInteraction();
        }

        #endregion

        #region Weapon Selection Input

        private void ToggleWeapon(InputAction.CallbackContext obj)
        {
            if (ActionStateHandler.CurActionState == ActionStates.ActionState.Idle || ActionStateHandler.CurActionState == ActionStates.ActionState.Moving)
                EquipmentHandler.WeaponHandler.ToggleEquippedWeapon();
        }

        #endregion
    }
}

