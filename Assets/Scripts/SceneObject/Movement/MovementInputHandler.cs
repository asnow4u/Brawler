using Game.SceneObject.Movement;
using Game.SceneObjects.ActionStates;
using Game.SceneObjects.Attack;
using Game.SceneObjects.Damage;
using System;
using System.ComponentModel;
using System.Text.RegularExpressions;
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

        //Jump Properties
        //NOTE: Based on how long the user holds the jump button will determin how high the player jumps
        [Header("Jump Properties")]        
        public int MAXJUMPFRAMECOUNT = 10;
        [SerializeField] private float curJumpFrameCount;
        [SerializeField] private int airJumpsPerformed;

        //Attack Properties
        private bool deceleratingForAttack;

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

            //Movement Collection
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

            //End movement after attack
            if (deceleratingForAttack && sceneObject.AttackInputHandler.TryGetCurrentAttackCollection(out AttackCollection curAttackCollection))
            {
                if (curAttackCollection.TryGetAttackByAnimation(clip, out _))
                    SetCurrentMoveState(MovementType.Null);

                deceleratingForAttack = false;
            }
        }

        #endregion


        #region Update        

        /// <summary>
        /// Update movement based on grounded status
        /// </summary>
        //public void UpdateMovement()
        //{                     
        //        //Climb Movement
        //        if (sceneObject.CurClimbState == ClimbState.Climbing)
        //        {
        //            //NOTE: Hitstun will force sceneObject ClimbState.UnAvailable
        //            UpdateClimbMovement(curMovementCollection);                   
        //        }

        //        //Grounded Movement
        //        else if (sceneObject.CurGroundedState == GroundedState.Grounded)
        //        {
        //            if (sceneObject.ActionStateHandler.CurActionState == ActionState.HitStun)
        //                UpdateGroundedHitStunMovement(curMovementCollection);
        //            else if (sceneObject.ActionStateHandler.CurActionState == ActionState.Attacking)
        //                UpdateGroundedAttackingMovement(curMovementCollection);
        //            else
        //                UpdateGroundedMovement(curMovementCollection);
        //        }                

        //        //Aerial
        //        else if (sceneObject.CurGroundedState == GroundedState.Airborn && sceneObject.CurClimbState != ClimbState.Climbing)
        //        {
        //            //Check if the player is in hitstun and update movement accordingly
        //            if (sceneObject.ActionStateHandler.CurActionState == ActionState.HitStun)
        //                UpdateAerialHitStunMovement(curMovementCollection.AirMoveData);
        //            else
        //                UpdateAerialMovement(curMovementCollection);
        //        }

        //        //Jump
        //        if (sceneObject.ActionStateHandler.CurActionState != ActionState.HitStun)
        //            UpdateJumpMovement(curMovementCollection);
        //}

        
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
        private bool IsJumpMovementAllowed()
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
        private bool IsAerialJumpMovementAllowed(AirJumpInputData airJumpData)
        {
            if (aerialJumpInfluence == 0)
                return false;

            if (curJumpFrameCount >= MAXJUMPFRAMECOUNT)
                return false;

            if (airJumpsPerformed > airJumpData.JumpsAvailable)
                return false;

            return true;
        }


        /// <summary>
        /// Determine if climb movement is allowed to transition to based on movement state
        /// </summary>
        private bool IsClimbMovementAllowed(MovementInputCollection curMovementCollection)
        {
            if (curMovementCollection.ClimbData == null)
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
        public void UpdateGroundedMovement()
        {
            ////Attacking Movement
            //if (sceneObject.ActionStateHandler.CurActionState == ActionState.Attacking)
            //    UpdateGroundedAttackingMovement();

            //Check if moving into a wall
            if ((horizontalInfluence > 0 && IsAgainstRightWall()) || (horizontalInfluence < 0 && IsAgainstLeftWall()))
                TrySetCurrentMoveState(MovementType.WallLean);

            //Horizontal Movement
            else if (IsHorizontalMovementAllowed() && TrySetCurrentMoveState(MovementType.Move))
                UpdateGroundedAcceleration();
        }

        //NOTE: This method should be removed.
        // When an attack state is set the move state should be set to null
        // MovementHandler should handle the decceleration for the attack
        /// <summary>
        /// Movement to be performed while attacking on the ground
        /// </summary>
        //private void UpdateGroundedAttackingMovement()
        //{            
        //    UpdateGroundedDecceleration(currentMovementCollection.GetGroundedAttackDecleration());
        //    deceleratingForAttack = true;
        //}       

        /// <summary>
        /// Accelerate on the ground by an acceleration value to a max velocity from <see cref="currentMovementCollection"/>
        /// </summary>
        private void UpdateGroundedAcceleration()
        {
            float acceleration = currentMovementCollection.GetGroundedXAcceleration();
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
        public bool UpdateAerialXMovement()
        {
            if (!IsHorizontalMovementAllowed() || !TrySetCurrentMoveState(MovementType.AirMove))
                return false;
            
            AerialXAccelerate();            
            return true;
        }

        /// <summary>
        /// Update vertical movement in the air
        /// </summary>
        public bool UpdateAerialYMovement()
        {
            if (!IsVerticalMovementAllowed() || !TrySetCurrentMoveState(MovementType.AirMove))
                return false;

            AerialYAccelerate();
            return true;
        }


        /// <summary>
        /// Accelerate in the air on the X axis
        /// </summary>
        private void AerialXAccelerate()
        {
            float maxXVelocity = movementHandler.AerialMaxXVelocity;
            float acceleration = currentMovementCollection.AirMoveData.AerialXAcceleration;

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
            float acceleration = currentMovementCollection.AirMoveData.AerialYAcceleration;

            if (verticalInfluence < 0)
            {
                float acceleratedYValue = rb.linearVelocity.y - (acceleration * Time.fixedDeltaTime);

                if (acceleratedYValue < -maxYVelocity)
                    acceleratedYValue = -maxYVelocity;

                rb.linearVelocity = new Vector3(rb.linearVelocity.x, acceleratedYValue, 0);
            }
        }

        #endregion


        #region Jump Movement

        private void UpdateJumpMovement(MovementInputCollection curMovementCollection)
        {
            //Grounded Jump
            if (IsJumpMovementAllowed() && TrySetCurrentMoveState(MovementType.Jump))
                UpdateGroundedJumpVelocity(curMovementCollection.JumpData);

            //Aerial Jump
            else if (IsAerialJumpMovementAllowed(curMovementCollection.AirJumpData) && TrySetCurrentMoveState(MovementType.AirJump))
                UpdateAerialJumpVelocity(curMovementCollection.AirJumpData);

        }



        /// <summary>
        /// Velocity applied to rb based on how long the jump button has been held
        /// </summary>
        private void UpdateGroundedJumpVelocity(JumpInputData jumpData)
        {
            float jumpVelocity = Mathf.Lerp(jumpData.MinJumpVelocity, jumpData.MaxJumpVelocity, curJumpFrameCount / MAXJUMPFRAMECOUNT);
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpVelocity, 0);

            curJumpFrameCount++;
        }



        /// <summary>
        /// Velocity applied to rb based on jumpInfluence
        /// </summary>
        private void UpdateAerialJumpVelocity(AirJumpInputData airJumpData)
        {
            CheckTurnAround();

            float jumpVelocity = Mathf.Lerp(airJumpData.MinAirJumpVelocity, airJumpData.MaxAirJumpVelocity, curJumpFrameCount / MAXJUMPFRAMECOUNT);
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpVelocity, 0);

            curJumpFrameCount++;
        }

        #endregion


        #region Climb Movement

        /// <summary>
        /// Update movement while climbing
        /// </summary>        
        private void UpdateClimbMovement(MovementInputCollection curMovementCollection)
        {
            if (IsClimbMovementAllowed(curMovementCollection) && (curMoveInputState == MovementType.Climb || TrySetCurrentMoveState(MovementType.Climb)))
            {
                CheckTurnAround();

                float climbXVelocity = horizontalInfluence * curMovementCollection.ClimbData.ClimbXVelocity;
                float climbYVelocity = 0;

                //Climb Up
                if (verticalInfluence > 0)
                    climbYVelocity = verticalInfluence * curMovementCollection.ClimbData.ClimbUpYVelocity;

                //Climb Down
                else if (verticalInfluence < 0)
                    climbYVelocity = verticalInfluence * curMovementCollection.ClimbData.ClimbDownYVelocity;

                rb.linearVelocity = new Vector3(climbXVelocity, climbYVelocity, 0);
            }

            else
            {
                rb.linearVelocity = new Vector3(0, 0, 0);

                if (curMoveInputState != MovementType.Null)
                    SetCurrentMoveState(MovementType.Null);
            }
        }

        #endregion
    }
}
