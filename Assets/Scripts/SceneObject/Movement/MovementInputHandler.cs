using Game.Navigation;
using Game.SceneObjects.ActionStates;
using System;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

namespace Game.SceneObjects.Movement 
{
    public enum MovementType { Null, Move, WallLean, LedgeGrab, Jump, HeavyLanding}

    public class MovementInputHandler : SceneObjectHandler
    {
        const ActionState MOVESTATE = ActionState.Moving;

        private IMovementInput movementInput;

        private Rigidbody rb => sceneObject.Rb;
        private MovementHandler movementHandler => sceneObject.MovementHandler;

        //Movement State Data
        [Header("State")]
        [SerializeField] private MovementType curMoveInputState = MovementType.Null;
        [SerializeField] private MovementInputData curMovementInputData = null;

        //Movement Collection (NOTE: BaseMovementCollection is Required for all sceneObjects. Base handles the case where a sceneObject dosent use a weapon but moves)    
        [Header("Collection")]
        [SerializeField] private MovementInputCollection baseMovementCollection;
        [SerializeField] private MovementInputCollection currentMovementCollection;

        [Header("Influence")]
        [Range(-1, 1)]
        [SerializeField] private float horizontalInfluence;

        [Range(-1, 1)]
        [SerializeField] private float verticalInfluence;

        [Range(0, 1)]
        [SerializeField] private float jumpInfluence;

        [Range(0, 1)]
        [Tooltip("Target percentage of maxVelocity on X Axis")]
        [SerializeField] private const float hitStunVelocityTargetMultiplier = 0.25f;

        //Jump Properties
        private bool jumpInputAvailable = true; //Jump available is only true after the user has released the jump button 
        private int airJumpsPerformed = 0;

        //Events
        public event Action<MovementInputCollection> MovementCollectionChangedEvent;
        public event Action<MovementInputData> InputDataChangedEvent;


        #region Getters
        
        public MovementInputData CurMovementInputData => curMovementInputData;
        public MovementInputCollection CurrentMovementCollection => currentMovementCollection;        
        public float VerticalInfluence => verticalInfluence;   

        #endregion


        #region Initialize

        public override void RegisterToEvents()
        {
            sceneObject.GroundedStateChangedEvent += OnGroundedStateChanged;
            sceneObject.ClimbStateChangedEvent += OnClimbStateChanged;
            sceneObject.AnimationHandler.AnimationEndedEvent += OnAnimationEnded;
        }

        public override void UnregisterToEvents()
        {
            sceneObject.GroundedStateChangedEvent -= OnGroundedStateChanged;
            sceneObject.ClimbStateChangedEvent -= OnClimbStateChanged;
            sceneObject.AnimationHandler.AnimationEndedEvent -= OnAnimationEnded;
        }

        public override void Setup()
        {
            //Input
            if (sceneObject is IMovementInput input)
                movementInput = input;
            else
                throw new Exception($"SceneObject {sceneObject.name} does not implement IMovementInput interface");

            //Movement Collection //NOTE: This will later be removed to just get the movement data from the equipped weapon
            if (baseMovementCollection != null) 
                SetUpCollection(baseMovementCollection);
            else
                throw new MissingReferenceException("BaseMovementCollection is not set for MovementInputHandler");
        }


        public void SetUpCollection(MovementInputCollection collection)
        {
            currentMovementCollection = baseMovementCollection;
            currentMovementCollection.IndexData();

            //MovementCollectionChangedEvent?.Invoke(currentMovementCollection);
        }


        #endregion


        #region Events

        /// <summary>
        /// Handle Ground state changed event
        /// </summary>
        private void OnGroundedStateChanged(GroundedState groundedState)
        {
            if (groundedState == GroundedState.Grounded)
            {                     
                //Reset jumps
                airJumpsPerformed = 0;
                SetCurrentMoveState(null);
            }
        }

        /// <summary>
        /// Handle Climb state changed event
        /// </summary>
        private void OnClimbStateChanged(ClimbState prevClimbState, ClimbState climbState)
        {
            if (prevClimbState == ClimbState.Climbing && climbState == ClimbState.Unavailable)
                SetCurrentMoveState(null);

            //Reset jumps
            if (climbState == ClimbState.Climbing)
                airJumpsPerformed = 0;    
        }

        /// <summary>
        /// Move Animation Ended reset current moveState
        /// </summary>
        /// <param name="clip"></param>
        private void OnAnimationEnded(AnimationClip clip)
        {
            if (currentMovementCollection.TryGetMovementFromAnimation(clip, out MovementInputData inputData) &&
                inputData.Type == curMoveInputState)
            {
                SetCurrentMoveState(null);
            }
        }

        #endregion

        #region State 

        /// <summary>
        /// Set the current movement state to <paramref name="inputData"/> if possible
        /// </summary>
        private void SetCurrentMoveState(MovementInputData inputData)
        {            
            if (inputData == null)
            {
                bool sendEvent = curMovementInputData != null;
                curMovementInputData = null;
                curMoveInputState = MovementType.Null;

                if (sendEvent)
                    InputDataChangedEvent?.Invoke(null);

                return;
            }

            if (inputData != curMovementInputData && 
                sceneObject.ActionStateHandler.TryChangeState(MOVESTATE))
            {                             
                curMovementInputData = inputData;
                curMoveInputState = inputData.Type;
                InputDataChangedEvent?.Invoke(inputData);
            }
        }

        /// <summary>
        /// Update the current grounded movement state based on input influence and sceneObject state
        /// </summary>
        private void UpdateGroundedMovementState()
        {
            //Idle
            if (horizontalInfluence == 0 && jumpInfluence == 0)
                SetCurrentMoveState(null);

            //Jump
            else if (jumpInfluence > 0 && jumpInputAvailable)
            {
                SetCurrentMoveState(currentMovementCollection.GetMovementData<JumpInputData>());
                StartJump();
            }

            //Wall Lean
            else if (horizontalInfluence != 0 && IsRunningAgainstWall())
                SetCurrentMoveState(currentMovementCollection.GetMovementData<WallLeanInputData>());

            //Accelerate
            else if (horizontalInfluence != 0)
                SetCurrentMoveState(currentMovementCollection.GetMovementData<MoveInputData>());
        }

        /// <summary>
        /// Update the current aerial movement state based on input influence and sceneObject state
        /// </summary>
        private void UpdateAerialMovementState()
        {
            //Idle
            if (horizontalInfluence == 0 && verticalInfluence == 0 && jumpInfluence == 0)
                SetCurrentMoveState(null);

            //Jump
            else if (IsAerialJumpMovementAllowed())
            {
                SetCurrentMoveState(currentMovementCollection.GetMovementData<AirJumpInputData>());
                StartJump();
            }

            //Accelerate
            else if (curMoveInputState != MovementType.Jump && (horizontalInfluence != 0 || verticalInfluence != 0))
                SetCurrentMoveState(currentMovementCollection.GetMovementData<AirMoveInputData>());
        }

        /// <summary>
        /// Determine if aerial jump movement is allowed based on current movement state
        /// </summary>
        private bool IsAerialJumpMovementAllowed()
        {
            if (jumpInfluence == 0 || !jumpInputAvailable)
                return false;

            //Cant air jump if in the middle of jumping
            if (curMoveInputState == MovementType.Jump)
                return false;

            AirJumpInputData jumpData = currentMovementCollection.GetMovementData<AirJumpInputData>();
            if (airJumpsPerformed > jumpData.JumpsAvailable)
                return false;

            return true;
        }

        /// <summary>
        /// Update the current climb movement state based on input influence and sceneObject state
        /// </summary>
        private void UpdateClimbMovementState()
        {
            //Idle
            if (horizontalInfluence == 0 && verticalInfluence == 0 && jumpInfluence == 0)
                SetCurrentMoveState(null);

            //Jump
            else if (jumpInfluence > 0)
                SetCurrentMoveState(currentMovementCollection.GetMovementData<JumpInputData>());

            //Accelerate
            else if (horizontalInfluence != 0 || verticalInfluence != 0)
                SetCurrentMoveState(currentMovementCollection.GetMovementData<ClimbMoveInputData>());
        }

        #endregion


        #region Turn Around

        private void CheckTurnAround()
        {
            if (movementHandler.IsFacingRightDirection && horizontalInfluence < 0)
            {
                movementHandler.TurnAround();
                rb.linearVelocity = new Vector3(rb.linearVelocity.x * -1, rb.linearVelocity.y, 0);
            }

            else if (!movementHandler.IsFacingRightDirection && horizontalInfluence > 0)
            {
                movementHandler.TurnAround();
                rb.linearVelocity = new Vector3(rb.linearVelocity.x * -1, rb.linearVelocity.y, 0);
            }
        }

        #endregion


        #region Wall Check

        /// <summary>
        /// Determine if the sceneObject is against a wall
        /// </summary>
        private bool IsRunningAgainstWall()
        {            
            return IsAgainstRightWall() || IsAgainstLeftWall();
        }

        /// <summary>
        /// Determine if the sceneObject is against a wall on the right side
        /// </summary>
        private bool IsAgainstRightWall()
        {
            if (movementHandler.IsFacingRightDirection)
            {
                if (horizontalInfluence >= 0 && sceneObject.TryDetectCollision(Direction.Right, 0.5f, LayerMask.GetMask("Environment"), out _))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Determine if the sceneObject is against a wall on the left side
        /// </summary>
        private bool IsAgainstLeftWall()
        {
            if (!movementHandler.IsFacingRightDirection)
            {
                if (horizontalInfluence <= 0 && sceneObject.TryDetectCollision(Direction.Left, 0.5f, LayerMask.GetMask("Environment"), out _))
                    return true;
            }

            return false;
        }

        #endregion


        #region Movement Influence

        /// <summary>
        /// Movement based on input value <br/>
        /// Input value ranges between (-1, 1) <br/>
        /// Input Value determines how much of the curCollection moveSpeed should be applied
        /// </summary>
        public void SetMovementInfluence(Vector2 inputInfluence)
        {            
            horizontalInfluence = Mathf.Clamp(inputInfluence.x, -1, 1);
            verticalInfluence = Mathf.Clamp(inputInfluence.y, -1, 1);
        }

        /// <summary>
        /// Jump movement based on input value <br/>
        /// Input value is set to 0 or 1 <br/>
        /// Input value determines how much of the curCollection jumpSpeed should be applied
        /// </summary>
        public void SetJumpInfluence(float inputInfluence)
        {
            if (!jumpInputAvailable && inputInfluence == 0)
                jumpInputAvailable = true;

            jumpInfluence = Mathf.Clamp(inputInfluence, 0, 1);
        }

        #endregion


        #region Grounded Movement

        /// <summary>
        /// Update movement on the ground based on current movement state
        /// </summary>
        public void UpdateGroundedMovement(Action DeccerationCallback)
        {
            UpdateGroundedMovementState();

            switch (curMoveInputState)
            {
                case MovementType.Null:
                    DeccerationCallback();
                    break;

                case MovementType.WallLean:
                    DeccerationCallback();
                    //TODO: Handle better?
                    break;

                case MovementType.Move:
                    UpdateGroundedAcceleration();
                    break;

                case MovementType.Jump:
                    UpdateGroundedAcceleration();
                    UpdateJumpVelocity();
                    break;
            }
        }        

        /// <summary>
        /// Accelerate on the ground by an acceleration value to a max velocity from <see cref="currentMovementCollection"/>
        /// </summary>
        private void UpdateGroundedAcceleration()
        {
            float acceleration = currentMovementCollection.GetGroundedXAcceleration(sceneObject.MassRatio);
            float maxVelocity = movementHandler.GroundedMaxVelocity;

            CheckTurnAround();

            float targetXVelocity = maxVelocity * Mathf.Abs(horizontalInfluence);

            rb.linearVelocity = new Vector3(rb.linearVelocity.x + (horizontalInfluence * acceleration * Time.fixedDeltaTime), rb.linearVelocity.y, 0);

            if (rb.linearVelocity.x > targetXVelocity)
                rb.linearVelocity = new Vector3(targetXVelocity, rb.linearVelocity.y, 0);

            else if (rb.linearVelocity.x < -targetXVelocity)
                rb.linearVelocity = new Vector3(-targetXVelocity, rb.linearVelocity.y, 0);
        }     

        #endregion


        #region Aerial Movement

        /// <summary>
        /// Update velocity in the air while in hitstun
        /// </summary>
        //private void UpdateAerialHitStunMovement(AirMoveInputData airMoveData)
        //{
        //    //X Deceleration
        //    float targetXVelocity;

        //    //Right (Pos)
        //    if (sceneObject.Rb.linearVelocity.x > 0)
        //    {
        //        targetXVelocity = airMoveData.AerialMaxXVelocity * hitStunVelocityTargetMultiplier;

        //        if (sceneObject.Rb.linearVelocity.x > targetXVelocity)
        //            sceneObject.Rb.linearVelocity = new Vector3(sceneObject.Rb.linearVelocity.x - (sceneObject.DamageHandler.HitStunDeceleration * Time.fixedDeltaTime), sceneObject.Rb.linearVelocity.y, 0);

        //        if (sceneObject.Rb.linearVelocity.x < targetXVelocity)
        //            sceneObject.Rb.linearVelocity = new Vector3(targetXVelocity, sceneObject.Rb.linearVelocity.y, 0);
        //    }

        //    //Left (Neg)
        //    else
        //    {
        //        targetXVelocity = -airMoveData.AerialMaxXVelocity * hitStunVelocityTargetMultiplier;

        //        if (sceneObject.Rb.linearVelocity.x < targetXVelocity)
        //            sceneObject.Rb.linearVelocity = new Vector3(sceneObject.Rb.linearVelocity.x + (sceneObject.DamageHandler.HitStunDeceleration * Time.fixedDeltaTime), sceneObject.Rb.linearVelocity.y, 0);

        //        if (sceneObject.Rb.linearVelocity.x > targetXVelocity)
        //            sceneObject.Rb.linearVelocity = new Vector3(targetXVelocity, sceneObject.Rb.linearVelocity.y, 0);
        //    }


        //    //Y Deceleration
        //    float targetYVelocity = -airMoveData.AerialMaxXVelocity * hitStunVelocityTargetMultiplier;

        //    if (sceneObject.Rb.linearVelocity.y > targetYVelocity)
        //        sceneObject.Rb.linearVelocity = new Vector3(sceneObject.Rb.linearVelocity.x, sceneObject.Rb.linearVelocity.y - (sceneObject.DamageHandler.HitStunDeceleration * Time.fixedDeltaTime), 0);

        //    if (sceneObject.Rb.linearVelocity.y < targetYVelocity)
        //        sceneObject.Rb.linearVelocity = new Vector3(sceneObject.Rb.linearVelocity.x, targetYVelocity, 0);
        //}
        

        /// <summary>
        /// Update horizontal movement in the air
        /// </summary>
        public void UpdateAerialMovement(Action DeccelerationXCallback, Action DeccelerationYCallback)
        {
            UpdateAerialMovementState();

            switch (curMoveInputState)
            {
                case MovementType.Null:
                    DeccelerationXCallback();
                    DeccelerationYCallback();
                    break;

                case MovementType.Move:
                    
                    //Horizontal Movement
                    if (horizontalInfluence != 0)
                        AerialXAccelerate();
                    else
                        DeccelerationXCallback();

                    //Vertical Movement
                    if (verticalInfluence != 0)
                        AerialYAccelerate();
                    else
                        DeccelerationYCallback();
                    break;

                case MovementType.Jump:

                    //Horizontal Movement
                    if (horizontalInfluence != 0)
                        AerialXAccelerate();
                    else
                        DeccelerationXCallback();

                    UpdateJumpVelocity();
                    break;
            }
        }

        /// <summary>
        /// Accelerate in the air on the X axis
        /// </summary>
        private void AerialXAccelerate()
        {
            float maxXVelocity = movementHandler.AerialMaxXVelocity;

            AirMoveInputData airMoveData = currentMovementCollection.GetMovementData<AirMoveInputData>();
            float acceleration = airMoveData.AerialXMaxAcceleration;

            //Positive Acceleration
            if (horizontalInfluence > 0)
            {
                float acceleratedXValue = rb.linearVelocity.x + (acceleration * Time.fixedDeltaTime);

                if (acceleratedXValue > maxXVelocity)
                    acceleratedXValue = maxXVelocity;

                rb.linearVelocity = new Vector3(acceleratedXValue, rb.linearVelocity.y, 0);
            }

            //Negative Acceleration
            else if (horizontalInfluence < 0)
            {
                float acceleratedXValue = rb.linearVelocity.x - (acceleration * Time.fixedDeltaTime);

                if (acceleratedXValue < -maxXVelocity)
                    acceleratedXValue = -maxXVelocity;    

                rb.linearVelocity = new Vector3(acceleratedXValue, rb.linearVelocity.y, 0);
            }            
        }

        /// <summary>
        /// Accelerate in the air on the Y axis
        /// </summary>
        private void AerialYAccelerate()
        {
            float maxYVelocity = movementHandler.AerialMaxYVelocity;

            AirMoveInputData airMoveData = currentMovementCollection.GetMovementData<AirMoveInputData>();
            float acceleration = airMoveData.AerialYMaxAcceleration;

            if (verticalInfluence < 0)
            {
                float acceleratedYValue = rb.linearVelocity.y - (acceleration * Time.fixedDeltaTime);

                if (acceleratedYValue < -maxYVelocity)
                    acceleratedYValue = -maxYVelocity;

                rb.linearVelocity = new Vector3(rb.linearVelocity.x, acceleratedYValue, 0);
            }
        }

        #endregion


        #region Climb Movement

        /// <summary>
        /// Update movement while climbing
        /// </summary>        
        public void UpdateClimbMovement()
        {
            UpdateClimbMovementState();

            switch (curMoveInputState)
            {
                case MovementType.Null:
                    rb.linearVelocity = new Vector3(0, 0, 0);
                    break;
            
                case MovementType.Move:
                    UpdateClimbAcceleration();
                    break;
                
                case MovementType.Jump:
                    UpdateJumpVelocity();
                    break;
            }            
        }

        /// <summary>
        /// Accelerate while on a climbable surface
        /// </summary>
        private void UpdateClimbAcceleration()
        {
            CheckTurnAround();

            ClimbMoveInputData climbData = currentMovementCollection.GetMovementData<ClimbMoveInputData>();

            float climbXVelocity = horizontalInfluence * climbData.ClimbXVelocity;
            float climbYVelocity = 0;

            //Climb Up
            if (verticalInfluence > 0)
                climbYVelocity = verticalInfluence * climbData.ClimbUpYVelocity;

            //Climb Down
            else if (verticalInfluence < 0)
                climbYVelocity = verticalInfluence * climbData.ClimbDownYVelocity;

            rb.linearVelocity = new Vector3(climbXVelocity, climbYVelocity, 0);
        }

        #endregion


        #region Jump

        private void StartJump()
        {
            if (curMovementInputData is JumpInputData jumpInputData)
            {
                float jumpVelocity = jumpInputData.GetInitialVelocity(sceneObject.MassRatio);
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpVelocity, 0);
            }

            if (curMovementInputData is AirJumpInputData)
                airJumpsPerformed++;

            jumpInputAvailable = false;
        }

        /// <summary>
        /// Velocity applied to rb based on how long the jump button has been held
        /// </summary>
        private void UpdateJumpVelocity()
        {
            if (curMovementInputData is JumpInputData jumpInputData)
            {                
                float acceleration = jumpInputData.GetJumpAcceleration(sceneObject.MassRatio) * jumpInfluence;
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y + (acceleration * Time.fixedDeltaTime), 0);
            }
        }

        #endregion
    }
}
