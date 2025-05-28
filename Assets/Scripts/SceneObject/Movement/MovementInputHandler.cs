using Game.SceneObjects.ActionStates;
using Game.SceneObjects.Attack;
using Game.SceneObjects.Damage;
using System;
using System.ComponentModel;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Game.SceneObjects.Movement 
{
    public enum MovementType { Null, Move, WallLean, AirMove, Climb, Jump, AirJump }

    public class MovementInputHandler : SceneObjectHandler
    {
        const ActionState MOVESTATE = ActionState.Moving;

        //Movement State Data
        [Header("State")]
        [SerializeField] private MovementType curMoveState;

        //Jump Properties
        //NOTE: Based on how long the user holds the jump button will determin how high the player jumps
        [Header("Jump Properties")]        
        public int MAXJUMPFRAMECOUNT = 10;
        [SerializeField] private float curJumpFrameCount;
        [SerializeField] private int airJumpsPerformed;

        //Attack Properties
        private bool deceleratingForAttack;

        //Movement Collection (NOTE: BaseMovementCollection is Required for all sceneObjects)    
        [Header("Collection")]
        [SerializeField] private MovementCollection baseMovementCollection;

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
        public event Action<MovementCollection> MovementCollectionChangedEvent;
        public event Action<MovementType> MoveStateChangedEvent;


        #region Getters

        public MovementType CurMoveState => curMoveState;

        public float VerticalInfluence => verticalInfluence;

        /// <summary>
        /// Attempt to get the <paramref name="currentMoveCollection"/> <br/>
        /// This will prioritize an equpped <see cref="Weapon"/> movement collection over <see cref="baseMovementCollection"/>
        /// </summary>
        public bool TryGetCurrentMovementCollection(out MovementCollection currentMoveCollection)
        {
            currentMoveCollection = null;

            if (baseMovementCollection != null)
            {
                if (sceneObject.EquipmentHandler.CurWeapon != null)
                    currentMoveCollection = sceneObject.EquipmentHandler.CurWeapon.MovementCollection;
                else
                    currentMoveCollection = baseMovementCollection;
            }

            return currentMoveCollection != null;
        }


        #endregion


        #region Initialize

        public override void Setup()
        {
            base.Setup();
        }


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
        private void OnClimbStateChanged(ClimbState climbState)
        {
            if (climbState == ClimbState.Unavailable)
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
            if (TryGetCurrentMovementCollection(out MovementCollection curMovementCollection))
            {
                if (curMovementCollection.TryGetMovementFromAnimation(clip, out _))
                    SetCurrentMoveState(MovementType.Null);

                //End movement after attack
                if (deceleratingForAttack && sceneObject.AttackInputHandler.TryGetCurrentAttackCollection(out AttackCollection curAttackCollection))
                {
                    if (curAttackCollection.TryGetAttackByAnimation(clip, out _))
                        SetCurrentMoveState(MovementType.Null);

                    deceleratingForAttack = false;
                }
            }
        }

        #endregion


        #region Update        

        /// <summary>
        /// Update movement based on grounded status
        /// </summary>
        public void UpdateMovement()
        {            
            if (TryGetCurrentMovementCollection(out MovementCollection curMovementCollection))
            {                 
                //Climb Movement
                if (sceneObject.CurClimbState == ClimbState.Climbing)
                {
                    //NOTE: Hitstun will force sceneObject ClimbState.UnAvailable
                    UpdateClimbMovement(curMovementCollection);                   
                }

                //Grounded Movement
                else if (sceneObject.CurGroundedState == GroundedState.Grounded)
                {
                    if (sceneObject.ActionStateHandler.CurActionState == ActionState.HitStun)
                        UpdateGroundedHitStunMovement(curMovementCollection);
                    else if (sceneObject.ActionStateHandler.CurActionState == ActionState.Attacking)
                        UpdateGroundedAttackingMovement(curMovementCollection);
                    else
                        UpdateGroundedMovement(curMovementCollection);
                }                

                //Aerial
                else if (sceneObject.CurGroundedState == GroundedState.Airborn && sceneObject.CurClimbState != ClimbState.Climbing)
                {
                    //Check if the player is in hitstun and update movement accordingly
                    if (sceneObject.ActionStateHandler.CurActionState == ActionState.HitStun)
                        UpdateAerialHitStunMovement(curMovementCollection.AirMoveData);
                    else
                        UpdateAerialMovement(curMovementCollection);
                }

                //Jump
                if (sceneObject.ActionStateHandler.CurActionState != ActionState.HitStun)
                    UpdateJumpMovement(curMovementCollection);
            }
        }        

        #endregion


        #region State 

        /// <summary>
        /// Attempt to set the <see cref="ActionState"/> to <see cref="MOVESTATE"/> <br/>
        /// Then set the <see cref="curMoveState"/> to the <paramref name="moveState"/>        
        private bool TrySetCurrentMoveState(MovementType moveState)
        {                                    
            if (sceneObject.ActionStateHandler.TryChangeState(MOVESTATE))
            {
                if (moveState != curMoveState && moveState > curMoveState)
                {
                    SetCurrentMoveState(moveState);                    
                }

                return true;
            }

            return false;
        }


        /// <summary>
        /// Set the <see cref="curMoveState"/> to <paramref name="moveState"/>
        /// </summary>
        private void SetCurrentMoveState(MovementType moveState)
        {
            curMoveState = moveState;
            MoveStateChangedEvent?.Invoke(moveState);
        }

        #endregion


        #region Turn Around

        private void CheckTurnAround()
        {
            if (sceneObject.IsFacingRightDirection && horizontalInfluence < 0)
            {
                sceneObject.TurnAround();
                sceneObject.Rb.linearVelocity = new Vector3(sceneObject.Rb.linearVelocity.x * -1, sceneObject.Rb.linearVelocity.y, sceneObject.Rb.linearVelocity.z);                
            }

            else if (!sceneObject.IsFacingRightDirection && horizontalInfluence > 0)
            {
                sceneObject.TurnAround();
                sceneObject.Rb.linearVelocity = new Vector3(sceneObject.Rb.linearVelocity.x * -1, sceneObject.Rb.linearVelocity.y, sceneObject.Rb.linearVelocity.z);
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
            if (sceneObject.IsFacingRightDirection)
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
            if (!sceneObject.IsFacingRightDirection)
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
        private bool IsAerialJumpMovementAllowed(AirJumpData airJumpData)
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
        private bool IsClimbMovementAllowed(MovementCollection curMovementCollection)
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
        /// Movement to be performed while in hitstun on the ground
        /// </summary>
        private void UpdateGroundedHitStunMovement(MovementCollection curMovementCollection)
        {
            //throw new NotImplementedException();
        }


        /// <summary>
        /// Movement to be performed while attacking on the ground
        /// </summary>
        private void UpdateGroundedAttackingMovement(MovementCollection curMovementCollection)
        {            
            UpdateGroundedDecceleration(curMovementCollection.GetGroundedAttackDecleration());
            deceleratingForAttack = true;
        }       


        /// <summary>
        /// Update movement on the ground based on <see cref="horizontalInfluence"/>, <see cref="verticalInfluence"/> and <see cref="jumpInfluence"/>
        /// </summary>
        private void UpdateGroundedMovement(MovementCollection curMovementCollection)
        {
            //Check if moving into a wall
            if ((horizontalInfluence > 0 && IsAgainstRightWall()) || (horizontalInfluence < 0 && IsAgainstLeftWall()))
                TrySetCurrentMoveState(MovementType.WallLean);

            //Horizontal Movement
            else if (IsHorizontalMovementAllowed() && TrySetCurrentMoveState(MovementType.Move))
                UpdateGroundedAcceleration(curMovementCollection.GetGroundedXAcceleration(), curMovementCollection.GetGroundedMaxXVelocity());
            
            else
                UpdateGroundedDecceleration(curMovementCollection.GetGroundedXDeceleration());

        }


        /// <summary>
        /// Accelerate on the ground by <paramref name="acceleration"/> value to a max <paramref name="maxVelocity"/>
        /// </summary>
        private void UpdateGroundedAcceleration(float acceleration, float maxVelocity)
        {
            CheckTurnAround();

            float targetXVelocity = maxVelocity * Mathf.Abs(horizontalInfluence);

            sceneObject.Rb.linearVelocity = new Vector3(sceneObject.Rb.linearVelocity.x + (horizontalInfluence * acceleration * Time.fixedDeltaTime), sceneObject.Rb.linearVelocity.y, sceneObject.Rb.linearVelocity.z);

            if (sceneObject.Rb.linearVelocity.x > targetXVelocity)
                sceneObject.Rb.linearVelocity = new Vector3(targetXVelocity, sceneObject.Rb.linearVelocity.y, sceneObject.Rb.linearVelocity.z);

            else if (sceneObject.Rb.linearVelocity.x < -targetXVelocity)
                sceneObject.Rb.linearVelocity = new Vector3(-targetXVelocity, sceneObject.Rb.linearVelocity.y, sceneObject.Rb.linearVelocity.z);
        }


        /// <summary>
        /// Update velocity while on the ground to slow down by <paramref name="deccelerationValue"/>
        /// </summary>
        private void UpdateGroundedDecceleration(float deccelerationValue)
        {
            //Prevet deccelerate when jumping from ground
            if (curMoveState != MovementType.Jump)
            {
                if (sceneObject.Rb.linearVelocity.x != 0)
                {
                    Vector3 dragForce = sceneObject.Rb.linearVelocity.normalized * deccelerationValue;
                    sceneObject.Rb.linearVelocity -= dragForce * Time.fixedDeltaTime;
                }

                if ((sceneObject.IsFacingRightDirection && sceneObject.Rb.linearVelocity.x <= 0) ||
                    (!sceneObject.IsFacingRightDirection && sceneObject.Rb.linearVelocity.x >= 0))
                {
                    sceneObject.Rb.linearVelocity = new Vector3(0, sceneObject.Rb.linearVelocity.y, sceneObject.Rb.linearVelocity.z);

                    if (curMoveState != MovementType.Null)
                        SetCurrentMoveState(MovementType.Null);

                }
            }
        }

        #endregion


        #region Aerial Movement

        /// <summary>
        /// Update velocity in the air while in hitstun
        /// </summary>
        private void UpdateAerialHitStunMovement(AirMoveData airMoveData)
        {
            //X Deceleration
            float targetXVelocity;

            //Right (Pos)
            if (sceneObject.Rb.linearVelocity.x > 0)
            {
                targetXVelocity = airMoveData.AerialMaxXVelocity * hitStunVelocityTargetMultiplier;

                if (sceneObject.Rb.linearVelocity.x > targetXVelocity)
                    sceneObject.Rb.linearVelocity = new Vector3(sceneObject.Rb.linearVelocity.x - (sceneObject.DamageHandler.HitStunDeceleration * Time.fixedDeltaTime), sceneObject.Rb.linearVelocity.y, sceneObject.Rb.linearVelocity.z);

                if (sceneObject.Rb.linearVelocity.x < targetXVelocity)
                    sceneObject.Rb.linearVelocity = new Vector3(targetXVelocity, sceneObject.Rb.linearVelocity.y, sceneObject.Rb.linearVelocity.z);
            }

            //Left (Neg)
            else
            {
                targetXVelocity = -airMoveData.AerialMaxXVelocity * hitStunVelocityTargetMultiplier;

                if (sceneObject.Rb.linearVelocity.x < targetXVelocity)
                    sceneObject.Rb.linearVelocity = new Vector3(sceneObject.Rb.linearVelocity.x + (sceneObject.DamageHandler.HitStunDeceleration * Time.fixedDeltaTime), sceneObject.Rb.linearVelocity.y, sceneObject.Rb.linearVelocity.z);

                if (sceneObject.Rb.linearVelocity.x > targetXVelocity)
                    sceneObject.Rb.linearVelocity = new Vector3(targetXVelocity, sceneObject.Rb.linearVelocity.y, sceneObject.Rb.linearVelocity.z);
            }


            //Y Deceleration
            float targetYVelocity = -airMoveData.AerialMaxXVelocity * hitStunVelocityTargetMultiplier;

            if (sceneObject.Rb.linearVelocity.y > targetYVelocity)
                sceneObject.Rb.linearVelocity = new Vector3(sceneObject.Rb.linearVelocity.x, sceneObject.Rb.linearVelocity.y - (sceneObject.DamageHandler.HitStunDeceleration * Time.fixedDeltaTime), sceneObject.Rb.linearVelocity.z);

            if (sceneObject.Rb.linearVelocity.y < targetYVelocity)
                sceneObject.Rb.linearVelocity = new Vector3(sceneObject.Rb.linearVelocity.x, targetYVelocity, sceneObject.Rb.linearVelocity.z);
        }


        /// <summary>
        /// Update movement in the air
        /// </summary>
        private void UpdateAerialMovement(MovementCollection curMovementCollection)
        {
            //Horizontal Movement
            if (IsHorizontalMovementAllowed() && TrySetCurrentMoveState(MovementType.AirMove))
                AerialXAccelerate(curMovementCollection.AirMoveData);
            else
                AerialXDeccelerate(curMovementCollection.AirMoveData);

            //Vertical Movement  
            if (IsVerticalMovementAllowed() && TrySetCurrentMoveState(MovementType.AirMove))
                AerialYAccelerate(curMovementCollection.AirMoveData);
            else
                AerialYDeccelerate(curMovementCollection.AirMoveData);               

            //Gravity
            ApplyGravityScaler(curMovementCollection.AirMoveData);
        }


        /// <summary>
        /// Accelerate in the air on the X axis
        /// </summary>
        private void AerialXAccelerate(AirMoveData airMoveData)
        {
            
            //Positive Acceleration
            if (horizontalInfluence > 0)
            {
                float acceleratedXValue = sceneObject.Rb.linearVelocity.x + (airMoveData.AerialXAcceleration * Time.fixedDeltaTime);

                if (acceleratedXValue > airMoveData.AerialMaxXVelocity)
                    acceleratedXValue = airMoveData.AerialMaxXVelocity;

                sceneObject.Rb.linearVelocity = new Vector3(acceleratedXValue, sceneObject.Rb.linearVelocity.y, sceneObject.Rb.linearVelocity.z);
            }

            //Negative Acceleration
            else if (horizontalInfluence < 0)
            {
                float acceleratedXValue = sceneObject.Rb.linearVelocity.x - (airMoveData.AerialXAcceleration * Time.fixedDeltaTime);

                if (acceleratedXValue < -airMoveData.AerialMaxXVelocity)
                    acceleratedXValue = -airMoveData.AerialMaxXVelocity;    

                sceneObject.Rb.linearVelocity = new Vector3(acceleratedXValue, sceneObject.Rb.linearVelocity.y, sceneObject.Rb.linearVelocity.z);
            }            
        }


        /// <summary>
        /// Accelerate in the air on the Y axis
        /// </summary>
        private void AerialYAccelerate(AirMoveData airMoveData)
        {
            if (verticalInfluence < 0)
            {
                float acceleratedYValue = sceneObject.Rb.linearVelocity.y - (airMoveData.AerialYAcceleration * Time.fixedDeltaTime);

                if (acceleratedYValue < -airMoveData.AerialMaxYVelocity)
                    acceleratedYValue = -airMoveData.AerialMaxYVelocity;

                sceneObject.Rb.linearVelocity = new Vector3(sceneObject.Rb.linearVelocity.x, acceleratedYValue, sceneObject.Rb.linearVelocity.z);
            }
        }


        /// <summary>
        /// Deccelerate in the air on the X axis 
        /// </summary>
        private void AerialXDeccelerate(AirMoveData airMoveData)
        {
            //Only deccelerate if velocity is greater than max velocity
            if (Mathf.Abs(sceneObject.Rb.linearVelocity.x) > airMoveData.AerialMaxXVelocity)
            {                
                //Positive Decceleration
                if (sceneObject.Rb.linearVelocity.x > 0)
                {
                    float decceleratedXValue = sceneObject.Rb.linearVelocity.x - (airMoveData.AerialXDeceleration * Time.fixedDeltaTime);

                    if (decceleratedXValue < 0)
                        decceleratedXValue = 0;

                    sceneObject.Rb.linearVelocity = new Vector3(decceleratedXValue, sceneObject.Rb.linearVelocity.y, sceneObject.Rb.linearVelocity.z);
                }

                //Negative Decceleration
                else if (sceneObject.Rb.linearVelocity.x < 0)
                {
                    float decceleratedXValue = sceneObject.Rb.linearVelocity.x + (airMoveData.AerialXDeceleration * Time.fixedDeltaTime);

                    if (decceleratedXValue > 0)
                        decceleratedXValue = 0;

                    sceneObject.Rb.linearVelocity = new Vector3(decceleratedXValue, sceneObject.Rb.linearVelocity.y, sceneObject.Rb.linearVelocity.z);
                }
            }
        }


        /// <summary>
        /// Deccelerate in the air on the Y axis 
        /// </summary>
        private void AerialYDeccelerate(AirMoveData airMoveData)
        {
            if (Mathf.Abs(sceneObject.Rb.linearVelocity.y) > airMoveData.AerialMaxYVelocity)                
            {
                //Positive Decceleration
                if (sceneObject.Rb.linearVelocity.y > 0)
                {
                    float decceleratedYValue = sceneObject.Rb.linearVelocity.y - (GetDeccelerationRateWithGravity(airMoveData) * Time.fixedDeltaTime);

                    if (decceleratedYValue < 0)
                        decceleratedYValue = 0;

                    sceneObject.Rb.linearVelocity = new Vector3(sceneObject.Rb.linearVelocity.x, decceleratedYValue, sceneObject.Rb.linearVelocity.z);
                }

                //Negative Decceleration
                else if (sceneObject.Rb.linearVelocity.y < 0)
                {
                    float decceleratedYValue = sceneObject.Rb.linearVelocity.y + (GetDeccelerationRateWithGravity(airMoveData) * Time.fixedDeltaTime);

                    if (decceleratedYValue > 0)
                        decceleratedYValue = 0;

                    sceneObject.Rb.linearVelocity = new Vector3(sceneObject.Rb.linearVelocity.x, decceleratedYValue, sceneObject.Rb.linearVelocity.z);
                }
            }
        }


        /// <returns>
        /// Return the Y axis decceleration rate with gravity applied <br/>
        /// The decceleration rate from the collection is what the sceneObject should deccelerate by so adjustments have to be made to account for gravity
        /// </returns>
        /// <exception cref="NullReferenceException"></exception>
        private float GetDeccelerationRateWithGravity(AirMoveData airMoveData)
        {            
            if (sceneObject.Rb.linearVelocity.y > 0)
                return airMoveData.AerialYDeceleration - Physics.gravity.y;
            else
                return airMoveData.AerialYDeceleration + Physics.gravity.y;        
        }


        /// <summary>
        /// Apply additional downward velocity ontop of gravity based on <paramref name="airMoveData"/>
        /// </summary>
        private void ApplyGravityScaler(AirMoveData airMoveData)
        {
            sceneObject.Rb.linearVelocity = new Vector3(sceneObject.Rb.linearVelocity.x, sceneObject.Rb.linearVelocity.y + (Physics.gravity.y * airMoveData.AdditionalGravityScaler * Time.fixedDeltaTime), sceneObject.Rb.linearVelocity.z);
        }

        #endregion


        #region Jump Movement

        private void UpdateJumpMovement(MovementCollection curMovementCollection)
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
        private void UpdateGroundedJumpVelocity(JumpData jumpData)
        {
            float jumpVelocity = Mathf.Lerp(jumpData.MinJumpVelocity, jumpData.MaxJumpVelocity, curJumpFrameCount / MAXJUMPFRAMECOUNT);
            sceneObject.Rb.linearVelocity = new Vector3(sceneObject.Rb.linearVelocity.x, jumpVelocity, sceneObject.Rb.linearVelocity.z);

            curJumpFrameCount++;
        }



        /// <summary>
        /// Velocity applied to rb based on jumpInfluence
        /// </summary>
        private void UpdateAerialJumpVelocity(AirJumpData airJumpData)
        {
            CheckTurnAround();

            float jumpVelocity = Mathf.Lerp(airJumpData.MinAirJumpVelocity, airJumpData.MaxAirJumpVelocity, curJumpFrameCount / MAXJUMPFRAMECOUNT);
            sceneObject.Rb.linearVelocity = new Vector3(sceneObject.Rb.linearVelocity.x, jumpVelocity, sceneObject.Rb.linearVelocity.z);

            curJumpFrameCount++;
        }

        #endregion


        #region Climb Movement

        /// <summary>
        /// Update movement while climbing
        /// </summary>        
        private void UpdateClimbMovement(MovementCollection curMovementCollection)
        {
            if (IsClimbMovementAllowed(curMovementCollection) && (curMoveState == MovementType.Climb || TrySetCurrentMoveState(MovementType.Climb)))
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

                sceneObject.Rb.linearVelocity = new Vector3(climbXVelocity, climbYVelocity, sceneObject.Rb.linearVelocity.z);
            }

            else
            {
                sceneObject.Rb.linearVelocity = new Vector3(0, 0, sceneObject.Rb.linearVelocity.z);

                if (curMoveState != MovementType.Null)
                    SetCurrentMoveState(MovementType.Null);
            }
        }

        #endregion
    }
}
