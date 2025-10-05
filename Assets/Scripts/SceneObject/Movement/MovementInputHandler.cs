using Game.SceneObjects.ActionStates;
using System;
using UnityEngine;

namespace Game.SceneObjects.Movement 
{
    public enum MovementType { Null, Move, WallLean, AirMove, Jump, AirJump, Climb }

    public class MovementInputHandler : SceneObjectHandler
    {
        const ActionState MOVESTATE = ActionState.Moving;

        private IMovementInput movementInput;

        private Rigidbody rb => sceneObject.Rb;
        private MovementHandler movementHandler => sceneObject.MovementHandler;

        //Movement State Data
        [Header("State")]
        [SerializeField] private MovementType curMoveInputState;

        //Movement Collection (NOTE: BaseMovementCollection is Required for all sceneObjects. Base handles the case where a sceneObject dosent use a weapon but moves)    
        [Header("Collection")]
        [SerializeField] private MovementInputCollection baseMovementCollection;
        [SerializeField] private MovementInputCollection currentMovementCollection;

        [Header("Influence")]
        [Range(-1, 1)]
        [SerializeField] private float horizontalInfluence;

        [Range(-1, 1)]
        [SerializeField] private float verticalInfluence;

        [SerializeField] private float groundedJumpInfluence;
        [SerializeField] private float aerialJumpInfluence;

        [Range(0, 1)]
        [Tooltip("Target percentage of maxVelocity on X Axis")]
        [SerializeField] private const float hitStunVelocityTargetMultiplier = 0.25f;

        //Jump Properties
        //NOTE: Based on how long the user holds the jump button will determin how high the player jumps
        [Header("Jump Properties")]
        public int MAXJUMPFRAMECOUNT = 10;
        [SerializeField] private float curJumpFrameCount;
        [SerializeField] private int airJumpsPerformed;

        //Events
        public event Action<MovementInputCollection> MovementCollectionChangedEvent;
        public event Action<MovementType> MoveStateChangedEvent;


        #region Getters

        public MovementType CurMoveInputState => curMoveInputState;
        public MovementInputCollection CurrentMovementCollection => currentMovementCollection;
        public float VerticalInfluence => verticalInfluence;

        ///// <summary>
        ///// Attempt to get the <paramref name="currentMoveCollection"/> <br/>
        ///// This will prioritize an equpped <see cref="Weapon"/> movement collection over <see cref="baseMovementCollection"/>
        ///// </summary>
        //public bool TryGetCurrentMovementCollection(out MovementCollection currentMoveCollection)
        //{
        //    currentMoveCollection = null;
            
        //    if (baseMovementCollection == null)
        //        return false;            

        //    if (sceneObject.EquipmentHandler.WeaponHandler?.EquippedWeapon != null)
        //        currentMoveCollection = sceneObject.EquipmentHandler.WeaponHandler.EquippedWeapon.MovementCollection;
        //    else
        //        currentMoveCollection = baseMovementCollection;

        //    return currentMoveCollection != null;
        //}


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
                currentMovementCollection = baseMovementCollection;
            else
                throw new MissingReferenceException("BaseMovementCollection is not set for MovementInputHandler");
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
                SetCurrentMoveState(MovementType.Null);
            }
        }

        /// <summary>
        /// Handle Climb state changed event
        /// </summary>
        private void OnClimbStateChanged(ClimbState prevClimbState, ClimbState climbState)
        {
            if (prevClimbState == ClimbState.Climbing && climbState == ClimbState.Unavailable)
                SetCurrentMoveState(MovementType.Null);

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
            if (currentMovementCollection.TryGetMovementFromAnimation(clip, out _))
                SetCurrentMoveState(MovementType.Null);
        }

        #endregion

        #region State 

        /// <summary>
        /// Attempt to set the <see cref="ActionState"/> to <see cref="MOVESTATE"/> <br/>
        /// Then set the <see cref="curMoveInputState"/> to the <paramref name="moveState"/>        
        private bool TrySetCurrentMoveState(MovementType moveState)
        {                                    
            if (sceneObject.ActionStateHandler.TryChangeState(MOVESTATE))
            {
                if (moveState != curMoveInputState)
                {
                    //NOTE: Jumping can override any moveState
                    if (moveState == MovementType.Jump || moveState == MovementType.AirJump)
                        SetCurrentMoveState(moveState);

                    else if (moveState > curMoveInputState)
                        SetCurrentMoveState(moveState);
                }

                return true;
            }

            return false;
        }


        /// <summary>
        /// Set the <see cref="curMoveInputState"/> to <paramref name="moveState"/>
        /// </summary>
        private void SetCurrentMoveState(MovementType moveState)
        {
            curMoveInputState = moveState;
            MoveStateChangedEvent?.Invoke(moveState);
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
        public bool IsAgainstWall()
        {            
            return IsAgainstRightWall() || IsAgainstLeftWall();
        }

        /// <summary>
        /// Determine if the sceneObject is against a wall on the right side
        /// </summary>
        public bool IsAgainstRightWall()
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
        public bool IsAgainstLeftWall()
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
            if (inputInfluence > 0)
            {
                if (sceneObject.CurGroundedState == GroundedState.Grounded || sceneObject.CurClimbState == ClimbState.Climbing)
                    groundedJumpInfluence = inputInfluence;
                else
                {
                    aerialJumpInfluence = inputInfluence;
                    airJumpsPerformed++;
                }
            }

            else
            {
                groundedJumpInfluence = 0;
                aerialJumpInfluence = 0;
                curJumpFrameCount = 0;
            }
        }

        #endregion


        #region Movement Permission

        /// <summary>
        /// Determin if horizontal movement is allowed based on current movement state
        /// </summary>
        private bool IsHorizontalMovementAllowed()
        {
            //Cant move without influence
            if (horizontalInfluence == 0)
                return false;
            
            if (IsAgainstWall())
                return false;

            return true;
        }


        /// <summary>
        /// Determine if vertical movement is allowed based on current movement state
        /// </summary>
        private bool IsVerticalMovementAllowed()
        {
            //Cant move without influence
            if (verticalInfluence == 0)
                return false;

            return true;
        }


        /// <summary>
        /// Determine if jump movement is allowed based on current movement state
        /// </summary>
        private bool IsGroundedJumpMovementAllowed()
        {            
            if (groundedJumpInfluence == 0)
                return false;

            if (curJumpFrameCount >= MAXJUMPFRAMECOUNT)
                return false;

            return true;
        }


        /// <summary>
        /// Determine if aerial jump movement is allowed based on current movement state
        /// </summary>
        private bool IsAerialJumpMovementAllowed()
        {
            if (aerialJumpInfluence == 0)
                return false;

            if (curJumpFrameCount >= MAXJUMPFRAMECOUNT)
                return false;

            if (airJumpsPerformed > currentMovementCollection.AirJumpData.JumpsAvailable)
                return false;

            return true;
        }


        /// <summary>
        /// Determine if climb movement is allowed to transition to based on movement state
        /// </summary>
        private bool IsClimbMovementAllowed()
        {
            if (currentMovementCollection.ClimbData == null)
                return false;

            if (curMoveInputState == MovementType.Jump || curMoveInputState == MovementType.AirJump)
                return false;

            if (verticalInfluence == 0 && horizontalInfluence == 0)
                return false;            

            return true;
        }

        #endregion


        #region Grounded Movement

        /// <summary>
        /// Update movement on the ground based on <see cref="horizontalInfluence"/>, <see cref="verticalInfluence"/> and <see cref="jumpInfluence"/>
        /// </summary>
        public void UpdateGroundedMovement(Action DeccerationCallback)
        {            
            //Wall Lean
            if ((horizontalInfluence > 0 && IsAgainstRightWall()) || (horizontalInfluence < 0 && IsAgainstLeftWall()))
                TrySetCurrentMoveState(MovementType.WallLean);

            //Accelerate
            else if (IsHorizontalMovementAllowed() && TrySetCurrentMoveState(MovementType.Move))
                UpdateGroundedAcceleration();

            //Deccelerate
            else
                DeccerationCallback();

            //Jump
            if (IsGroundedJumpMovementAllowed() && TrySetCurrentMoveState(MovementType.Jump))
                UpdateGroundedJumpVelocity();

            //Stop
            if (horizontalInfluence == 0 && curMoveInputState != MovementType.Jump && curMoveInputState != MovementType.Null)
                SetCurrentMoveState(MovementType.Null);
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

        /// <summary>
        /// Velocity applied to rb based on how long the jump button has been held
        /// </summary>
        private void UpdateGroundedJumpVelocity()
        {
            float minVelocity = currentMovementCollection.JumpData.MinJumpVelocity;
            float maxVelocity = currentMovementCollection.JumpData.MaxJumpVelocity;

            float jumpVelocity = Mathf.Lerp(minVelocity, maxVelocity, curJumpFrameCount / MAXJUMPFRAMECOUNT);
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpVelocity, 0);

            curJumpFrameCount++;
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
        public void UpdateAerialXMovement(Action DeccelerationCallback)
        {
            if (IsHorizontalMovementAllowed() || TrySetCurrentMoveState(MovementType.AirMove))
                AerialXAccelerate();
            else
                DeccelerationCallback();
        }

        /// <summary>
        /// Update vertical movement in the air
        /// </summary>
        public void UpdateAerialYMovement(Action DeccelerationCallback)
        {
            if (IsVerticalMovementAllowed() || TrySetCurrentMoveState(MovementType.AirMove))
                AerialYAccelerate();
            else
                DeccelerationCallback();

            if (IsAerialJumpMovementAllowed() && TrySetCurrentMoveState(MovementType.AirJump))
                UpdateAerialJumpVelocity();

        }


        /// <summary>
        /// Accelerate in the air on the X axis
        /// </summary>
        private void AerialXAccelerate()
        {
            float maxXVelocity = movementHandler.AerialMaxXVelocity;
            float acceleration = currentMovementCollection.AirMoveData.AerialXMaxAcceleration;

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
            float acceleration = currentMovementCollection.AirMoveData.AerialYMaxAcceleration;

            if (verticalInfluence < 0)
            {
                float acceleratedYValue = rb.linearVelocity.y - (acceleration * Time.fixedDeltaTime);

                if (acceleratedYValue < -maxYVelocity)
                    acceleratedYValue = -maxYVelocity;

                rb.linearVelocity = new Vector3(rb.linearVelocity.x, acceleratedYValue, 0);
            }
        }

        /// <summary>
        /// Velocity applied to rb based on jumpInfluence
        /// </summary>
        private void UpdateAerialJumpVelocity()
        {
            float minVelocity = currentMovementCollection.AirJumpData.MinAirJumpVelocity;
            float maxVelocity = currentMovementCollection.AirJumpData.MaxAirJumpVelocity;

            CheckTurnAround();

            float jumpVelocity = Mathf.Lerp(minVelocity, maxVelocity, curJumpFrameCount / MAXJUMPFRAMECOUNT);
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpVelocity, 0);

            curJumpFrameCount++;
        }

        #endregion


        #region Climb Movement

        /// <summary>
        /// Update movement while climbing
        /// </summary>        
        public void UpdateClimbMovement()
        {
            if (IsClimbMovementAllowed() && TrySetCurrentMoveState(MovementType.Climb))
            {
                CheckTurnAround();

                float climbXVelocity = horizontalInfluence * currentMovementCollection.ClimbData.ClimbXVelocity;
                float climbYVelocity = 0;

                //Climb Up
                if (verticalInfluence > 0)
                    climbYVelocity = verticalInfluence * currentMovementCollection.ClimbData.ClimbUpYVelocity;

                //Climb Down
                else if (verticalInfluence < 0)
                    climbYVelocity = verticalInfluence * currentMovementCollection.ClimbData.ClimbDownYVelocity;

                rb.linearVelocity = new Vector3(climbXVelocity, climbYVelocity, 0);
            }

            else
            {
                rb.linearVelocity = new Vector3(0, 0, 0);

                if (curMoveInputState != MovementType.Null)
                    SetCurrentMoveState(MovementType.Null);
            }

            //Jump
            if (IsGroundedJumpMovementAllowed() && TrySetCurrentMoveState(MovementType.Jump))
                UpdateGroundedJumpVelocity();
        }       

        #endregion
    }
}
