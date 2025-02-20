using Game.SceneObjects.ActionStates;
using Game.SceneObjects.Attack;
using Game.SceneObjects.Damage;
using System;
using UnityEngine;

namespace Game.SceneObjects.Movement 
{
    public enum MovementType { Null, Move, AirMove, Jump, AirJump, WallLean, WallSlide, WallJump, Landing }

    public class MovementInputHandler : SceneObjectHandler
    {
        const ActionState MOVESTATE = ActionState.Moving;

        //Movement State Data
        [Header("State")]
        [SerializeField] private MovementType curMoveState;
        private MovementData curMoveData;

        //Jump Properties
        private int airJumpsPerformed;

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

        [SerializeField] private float jumpInfluence;

        

        [Range(0, 1)]
        [Tooltip("Target percentage of maxVelocity on X Axis")]
        [SerializeField] private const float hitStunVelocityTargetMultiplier = 0.25f;

        //Events
        public event Action<MovementCollection> MovementCollectionChangedEvent;
        public event Action<MovementType> MoveStateChangedEvent;


        #region Getters

        public MovementType CurMoveState => curMoveState;
        public float HorizontalInfluence => horizontalInfluence;


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
            sceneObject.GroundedStateChangeEvent += OnGroundedStateChanged;
            sceneObject.AnimationHandler.AnimationEndedEvent += OnAnimationEnded;
        }

        public override void UnregisterToEvents()
        {
            sceneObject.GroundedStateChangeEvent -= OnGroundedStateChanged;
            sceneObject.AnimationHandler.AnimationEndedEvent -= OnAnimationEnded;
        }


        #endregion


        #region Events

        /// <summary>
        /// Ground state changed
        /// </summary>
        /// <param name="curGroundState"></param>
        private void OnGroundedStateChanged(GroundedState curGroundState)
        {
            switch (curGroundState)
            {
                case GroundedState.Grounded:
                    PerformLanding();
                    break;

                case GroundedState.Airborn:
                    TrySetCurrentMoveState(MovementType.Null);
                    break;

            }
        }


        /// <summary>
        /// Move Animation Ended reset current moveState
        /// </summary>
        /// <param name="clip"></param>
        private void OnAnimationEnded(AnimationClip clip)
        {
            if (TryGetCurrentMovementCollection(out MovementCollection curMovementCollection))
            {
                if (curMovementCollection.TryGetMovementFromAnimation(clip, out MovementData moveData))
                {
                    if (moveData.Type != MovementType.Move && moveData.Type != MovementType.AirMove)
                        TrySetCurrentMoveState(MovementType.Null);
                }

                //End movement after attack
                if (deceleratingForAttack && sceneObject.AttackInputHandler.TryGetCurrentAttackCollection(out AttackCollection curAttackCollection))
                {
                    if (curAttackCollection.TryGetAttackByAnimation(clip, out AttackData attackData))
                        TrySetCurrentMoveState(MovementType.Null);

                    deceleratingForAttack = false;
                }
            }
        }

        #endregion


        #region State 

        private bool TrySetCurrentMoveState(MovementType moveState)
        {
            if (moveState != MovementType.Null)
            {
                if (moveState != MovementType.Landing)
                {
                    //Change State
                    if (sceneObject.ActionStateHandler.TryChangeState(MOVESTATE))
                    {
                        curMoveState = moveState;
                        MoveStateChangedEvent?.Invoke(moveState);
                        return true;
                    }
                }

                //Landing
                else
                {
                    //TODO: Check for hitstun?
                    sceneObject.ActionStateHandler.ChangeState(MOVESTATE);
                    curMoveState = moveState;
                    MoveStateChangedEvent?.Invoke(moveState);
                    return true;
                }
            }

            else
            {
                curMoveState = MovementType.Null;
                curMoveData = null;
                MoveStateChangedEvent?.Invoke(moveState);
                return true;
            }

            return false;
        }

        #endregion


        #region Turn Around

        private void CheckTurnAround()
        {
            if (sceneObject.IsFacingRightDirection() && horizontalInfluence < 0)
            {
                sceneObject.TurnAround();

                if (sceneObject.Rb.linearVelocity.x > 0)
                {
                    switch (sceneObject.CurGroundedState)
                    {
                        case GroundedState.Grounded:
                            if (sceneObject.TryGetSlopeAngle(out Vector3 slope))
                                sceneObject.Rb.linearVelocity = slope * sceneObject.Rb.linearVelocity.magnitude;
                            break;

                        case GroundedState.Airborn:
                            sceneObject.Rb.linearVelocity = new Vector3(sceneObject.Rb.linearVelocity.x * -1, sceneObject.Rb.linearVelocity.y, sceneObject.Rb.linearVelocity.z);
                            break;
                    }
                }
            }

            else if (!sceneObject.IsFacingRightDirection() && horizontalInfluence > 0)
            {
                sceneObject.TurnAround();

                switch (sceneObject.CurGroundedState)
                {
                    case GroundedState.Grounded:
                        if (sceneObject.TryGetSlopeAngle(out Vector3 slope))
                            sceneObject.Rb.linearVelocity = slope * sceneObject.Rb.linearVelocity.magnitude;
                        break;

                    case GroundedState.Airborn:
                        sceneObject.Rb.linearVelocity = new Vector3(sceneObject.Rb.linearVelocity.x * -1, sceneObject.Rb.linearVelocity.y, sceneObject.Rb.linearVelocity.z);
                        break;
                }
            }
        }

        #endregion


        #region Wall Check

        public bool IsAgainstGroundedWall()
        {
            //Left
            if (!sceneObject.IsFacingRightDirection())
            {
                if (horizontalInfluence <= 0 && sceneObject.TryDetectCollision(Direction.Left, 0.5f, LayerMask.GetMask("Environment"), out _))
                    return true;
            }

            //Right
            else if (sceneObject.IsFacingRightDirection())
            {
                if (horizontalInfluence >= 0 && sceneObject.TryDetectCollision(Direction.Right, 0.5f, LayerMask.GetMask("Environment"), out _))
                    return true;
            }

            return false;
        }


        public bool IsAgainstArialWall()
        {
            //Left
            if (sceneObject.Rb.linearVelocity.x <= 0 && sceneObject.TryDetectCollision(Direction.Left, 0.5f, LayerMask.GetMask("Environment"), out _))
            {
                if (sceneObject.IsFacingRightDirection())
                    sceneObject.TurnAround();

                return true;
            }

            //Right
            if (sceneObject.Rb.linearVelocity.x >= 0 && sceneObject.TryDetectCollision(Direction.Right, 0.5f, LayerMask.GetMask("Environment"), out _))
            {
                if (!sceneObject.IsFacingRightDirection())
                    sceneObject.TurnAround();

                return true;
            }

            return false;
        }

        #endregion


        #region Perform Move

        /// <summary>
        /// Horizontal movement based on input value
        /// Input value ranges between (-1, 1) 
        /// Input Value determines how much of the curCollection moveSpeed should be applied
        /// </summary>
        /// <param name="inputInfluence"></param>
        public void PerformMovement(Vector2 inputInfluence)
        {
            horizontalInfluence = Mathf.Clamp(inputInfluence.x, -1, 1);
            verticalInfluence = Mathf.Clamp(inputInfluence.y, -1, 1);
        }


        /// <summary>
        /// Update movement based on grounded status
        /// </summary>
        public void UpdateMovement()
        {
            if (TryGetCurrentMovementCollection(out MovementCollection curMovementCollection))
            {
                //HitStun Movement
                if (sceneObject.ActionStateHandler.CurActionState == ActionState.HitStun)
                    UpdateAerialHitStunMovement(curMovementCollection.AirMoveData);

                //Grounded Movement
                else if (sceneObject.CurGroundedState == GroundedState.Grounded)
                {
                    UpdateGroundedMovement(curMovementCollection);

                    if (IsAgainstGroundedWall())
                        TrySetCurrentMoveState(MovementType.WallLean);
                }

                //Air Movement
                else
                {
                    UpdateAerialMovement(curMovementCollection.AirMoveData);

                    if (IsAgainstArialWall())
                        TrySetCurrentMoveState(MovementType.WallSlide);                    
                    
                    //Gravity
                    ApplyGravityScaler(curMovementCollection.AirMoveData);
                }

            }
        }


        #region Grounded Movement

        /// <summary>
        /// Update movement on the ground
        /// </summary>
        private void UpdateGroundedMovement(MovementCollection curMovementCollection)
        {
            if (sceneObject.ActionStateHandler.CurActionState != ActionState.HitStun)
            {
                if (horizontalInfluence != 0)
                {
                    //Calculate decceleration for attacks
                    if (sceneObject.ActionStateHandler.CurActionState == ActionState.Attacking)
                    {
                        AttackData attackData = sceneObject.AttackInputHandler.CurAttackData;
                        UpdateGroundDecceleration(curMovementCollection.GetGroundedAttackDeclerationn());

                        deceleratingForAttack = true;
                    }

                    if (curMoveState == MovementType.Null || curMoveState == MovementType.WallLean)
                        TrySetCurrentMoveState(MovementType.Move);

                    else if (curMoveState == MovementType.Move)
                    {
                        CheckTurnAround();
                        UpdateGroundAcceleration(curMovementCollection.GetGroundedXAcceleration(), curMovementCollection.GetGroundedMaxXVelocity());
                    }
                }

                else
                    UpdateGroundDecceleration(curMovementCollection.GetGroundedXDeceleration());
            } 
        }


        /// <summary>
        /// Accelerate on the ground by <paramref name="acceleration"/> value to a max <paramref name="maxVelocity"/>
        /// </summary>
        private void UpdateGroundAcceleration(float acceleration, float maxVelocity)
        {
            //Ground slope
            if (sceneObject.TryGetSlopeAngle(out Vector3 slope))
            {
                //Drag
                sceneObject.Rb.linearDamping = 0;

                //Cap Velocity based on horizontal influence
                float targetXVelocity = maxVelocity * Mathf.Abs(horizontalInfluence);

                //Update velocity based on slope
                sceneObject.Rb.linearVelocity += slope * Mathf.Abs(horizontalInfluence) * acceleration * Time.fixedDeltaTime;

                //Cant exceed target velocity
                if (sceneObject.Rb.linearVelocity.magnitude > targetXVelocity)
                    sceneObject.Rb.linearVelocity = slope * targetXVelocity;
            }
        }


        /// <summary>
        /// Update velocity while on the ground to slow down by <paramref name="deccelerationValue"/>
        /// </summary>
        private void UpdateGroundDecceleration(float deccelerationValue)
        {
            //Prevet deccelerate when jumping from ground
            if (curMoveState != MovementType.Jump)
            {
                if (sceneObject.Rb.linearVelocity.x != 0)
                {
                    Vector3 dragForce = sceneObject.Rb.linearVelocity.normalized * deccelerationValue;
                    sceneObject.Rb.linearVelocity -= dragForce * Time.fixedDeltaTime;

                    if ((sceneObject.IsFacingRightDirection() && sceneObject.Rb.linearVelocity.x <= 0) ||
                        (!sceneObject.IsFacingRightDirection() && sceneObject.Rb.linearVelocity.x >= 0))
                    {
                        sceneObject.Rb.linearVelocity = Vector3.zero;

                        if (curMoveData != null)
                            sceneObject.AnimationHandler.EndAnimation(curMoveData.Animation);

                        TrySetCurrentMoveState(MovementType.Null);
                    }
                }
            }
        }

        #endregion


        #region Aerial Movement

        /// <summary>
        /// Update movement in the air
        /// </summary>
        private void UpdateAerialMovement(AirMoveData airMoveData)
        {
            UpdateAerialXMovement(airMoveData);
            UpdateAerialYMovement(airMoveData);
        }


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
        /// Update velocity in the air on the X axis
        /// </summary>
        private void UpdateAerialXMovement(AirMoveData airMoveData)
        {
            if (horizontalInfluence != 0)
            {
                if (curMoveState == MovementType.Null || curMoveState == MovementType.WallSlide)
                    TrySetCurrentMoveState(MovementType.AirMove);

                if (CurMoveState == MovementType.AirMove)
                    AerialXAccelerate();
            }

            if (Mathf.Abs(sceneObject.Rb.linearVelocity.x) > airMoveData.AerialMaxXVelocity)
                AerialXDeccelerate();
        }


        /// <summary>
        /// Accelerate in the air on the X axis
        /// </summary>
        private void AerialXAccelerate()
        {
            if (TryGetCurrentMovementCollection(out MovementCollection collection))
            {
                //Positive Acceleration
                if (horizontalInfluence > 0)
                {
                    float acceleratedXValue = sceneObject.Rb.linearVelocity.x + (collection.GetAerialXAcceleration() * Time.fixedDeltaTime);

                    if (acceleratedXValue > collection.GetAerialMaxXVelocity())
                        acceleratedXValue = collection.GetAerialMaxXVelocity();

                    sceneObject.Rb.linearVelocity = new Vector3(acceleratedXValue, sceneObject.Rb.linearVelocity.y, sceneObject.Rb.linearVelocity.z);
                }

                //Negative Acceleration
                else if (horizontalInfluence < 0)
                {
                    float acceleratedXValue = sceneObject.Rb.linearVelocity.x - (collection.GetAerialXAcceleration() * Time.fixedDeltaTime);

                    if (acceleratedXValue < -collection.GetAerialMaxXVelocity())
                        acceleratedXValue = -collection.GetAerialMaxXVelocity();    

                    sceneObject.Rb.linearVelocity = new Vector3(acceleratedXValue, sceneObject.Rb.linearVelocity.y, sceneObject.Rb.linearVelocity.z);
                }
            }
        }


        /// <summary>
        /// Deccelerate in the air on the X axis 
        /// </summary>
        private void AerialXDeccelerate()
        {
            if (TryGetCurrentMovementCollection(out MovementCollection collection))
            {
                //Positive Decceleration
                if (sceneObject.Rb.linearVelocity.x > 0)
                {
                    float decceleratedXValue = sceneObject.Rb.linearVelocity.x - (collection.GetAerialXDeceleration() * Time.fixedDeltaTime);

                    if (decceleratedXValue < 0)
                        decceleratedXValue = 0;

                    sceneObject.Rb.linearVelocity = new Vector3(decceleratedXValue, sceneObject.Rb.linearVelocity.y, sceneObject.Rb.linearVelocity.z);
                }

                //Negative Decceleration
                else if (sceneObject.Rb.linearVelocity.x < 0)
                {
                    float decceleratedXValue = sceneObject.Rb.linearVelocity.x + (collection.GetAerialXDeceleration() * Time.fixedDeltaTime);

                    if (decceleratedXValue > 0)
                        decceleratedXValue = 0;

                    sceneObject.Rb.linearVelocity = new Vector3(decceleratedXValue, sceneObject.Rb.linearVelocity.y, sceneObject.Rb.linearVelocity.z);
                }
            }
        }


        /// <summary>
        /// Update velocity in the air on the Y axis
        /// </summary>
        private void UpdateAerialYMovement(AirMoveData airMoveData)
        {
            if (verticalInfluence != 0)
            {
                //TODO: Implement Fast Fall
                //AerialYAccelerate();
            }

            if (Mathf.Abs(sceneObject.Rb.linearVelocity.y) > airMoveData.AerialMaxYVelocity)
                AerialYDeccelerate();
        }



        /// <returns>
        /// Return the Y axis decceleration rate with gravity applied <br/>
        /// The decceleration rate from the collection is what the sceneObject should deccelerate by so adjustments have to be made to account for gravity
        /// </returns>
        /// <exception cref="NullReferenceException"></exception>
        private float GetDeccelerationRateWithGravity()
        {
            if (TryGetCurrentMovementCollection(out MovementCollection collection))
            {
                if (sceneObject.Rb.linearVelocity.y > 0)
                    return collection.AirMoveData.AerialYDeceleration - Physics.gravity.y;

                else if (sceneObject.Rb.linearVelocity.y < 0)
                    return collection.AirMoveData.AerialYDeceleration + Physics.gravity.y;
            }

            throw new NullReferenceException("Movement Collection not found");
        }


        /// <summary>
        /// Accelerate in the air on the Y axis 
        /// </summary>
        private void AerialYAccelerate()
        {
            if (TryGetCurrentMovementCollection(out MovementCollection collection))
            {
                if (verticalInfluence < 0)
                {
                    //TODO: Implement with fast fall
                }
            }
        }


        /// <summary>
        /// Deccelerate in the air on the Y axis 
        /// </summary>
        private void AerialYDeccelerate()
        {
            if (TryGetCurrentMovementCollection(out MovementCollection collection))
            {
                //Positive Decceleration
                if (sceneObject.Rb.linearVelocity.y > 0)
                {
                    float decceleratedYValue = sceneObject.Rb.linearVelocity.y - (GetDeccelerationRateWithGravity() * Time.fixedDeltaTime);

                    if (decceleratedYValue < 0)
                        decceleratedYValue = 0;

                    sceneObject.Rb.linearVelocity = new Vector3(sceneObject.Rb.linearVelocity.x, decceleratedYValue, sceneObject.Rb.linearVelocity.z);
                }

                //Negative Decceleration
                else if (sceneObject.Rb.linearVelocity.y < 0)
                {
                    float decceleratedYValue = sceneObject.Rb.linearVelocity.y + (GetDeccelerationRateWithGravity() * Time.fixedDeltaTime);

                    if (decceleratedYValue > 0)
                        decceleratedYValue = 0;

                    sceneObject.Rb.linearVelocity = new Vector3(sceneObject.Rb.linearVelocity.x, decceleratedYValue, sceneObject.Rb.linearVelocity.z);
                }
            }
        }

        #endregion

        #endregion


        #region Gravity Scaler

        /// <summary>
        /// Apply additional downward velocity ontop of gravity based on <paramref name="airMoveData"/>
        /// </summary>
        private void ApplyGravityScaler(AirMoveData airMoveData)
        {
            if (sceneObject.CurGroundedState == GroundedState.Airborn)
                sceneObject.Rb.linearVelocity = new Vector3(sceneObject.Rb.linearVelocity.x, sceneObject.Rb.linearVelocity.y + (Physics.gravity.y * airMoveData.AdditionalGravityScaler * Time.fixedDeltaTime), sceneObject.Rb.linearVelocity.z);
        }

        #endregion


        #region Perform Jump

        /// <summary>
        /// Jump action based on input value <br/>
        /// Cant jump while jumping or landing <br/>
        /// Cant jump while attacking / in hitstun
        /// Input value ranges between (0, 1) 
        /// </summary>
        /// <param name="inputInfluence"></param>
        public void PerformJump(float inputInfluence)
        {
            if (TryGetCurrentMovementCollection(out MovementCollection curMovementCollection))
            {
                if (curMoveState != MovementType.Jump && curMoveState != MovementType.Landing)
                {
                    jumpInfluence = Mathf.Clamp01(inputInfluence);

                    switch (sceneObject.CurGroundedState)
                    {
                        case GroundedState.Grounded:
                            if (curMovementCollection.TryGetMovementByType(MovementType.Jump, out MovementData groundJumpData))
                                PerformGroundedJump((JumpData)groundJumpData);

                            break;

                        case GroundedState.Airborn:

                            if (curMoveState == MovementType.WallSlide)
                            {
                                if (curMovementCollection.TryGetMovementByType(MovementType.WallJump, out MovementData wallJumpData))
                                    PerformWallJump((WallJumpData)wallJumpData);
                            }

                            else
                            {
                                if (curMovementCollection.TryGetMovementByType(MovementType.AirJump, out MovementData airJumpData))
                                {
                                    if (airJumpsPerformed < ((AirJumpData)airJumpData).JumpsAvailable)
                                    {
                                        PerformAirJump((AirJumpData)airJumpData);
                                        airJumpsPerformed++;
                                    }
                                }
                            }

                            break;
                    }
                }
            }
        }


        /// <summary>
        /// Velocity applied to rb based on jumpInfluence
        /// </summary>
        private void PerformGroundedJump(JumpData jumpData)
        {
            if (TrySetCurrentMoveState(MovementType.Jump))
            {
                sceneObject.Rb.linearVelocity = new Vector3(sceneObject.Rb.linearVelocity.x, jumpData.JumpVelocity * jumpInfluence, sceneObject.Rb.linearVelocity.z);
            }
        }


        /// <summary>
        /// Velocity applied to rb based on jumpInfluence
        /// </summary>
        private void PerformAirJump(AirJumpData airJumpData)
        {
            if (TrySetCurrentMoveState(MovementType.AirJump))
            {
                CheckTurnAround();
                sceneObject.Rb.linearVelocity = new Vector3(sceneObject.Rb.linearVelocity.x, airJumpData.AirJumpVelocity * jumpInfluence, sceneObject.Rb.linearVelocity.z);
            }
        }


        /// <summary>
        /// Velociy to rb to perform wall jump
        /// </summary>
        private void PerformWallJump(WallJumpData wallJumpData)
        {
            if (TrySetCurrentMoveState(MovementType.AirJump))
            {
                if (sceneObject.IsFacingRightDirection())
                    sceneObject.Rb.linearVelocity = new Vector3((-1) * Mathf.Cos(wallJumpData.JumpAngle * Mathf.Deg2Rad) * wallJumpData.JumpVelocity, Mathf.Sin(wallJumpData.JumpAngle * Mathf.Deg2Rad) * wallJumpData.JumpVelocity, sceneObject.Rb.linearVelocity.z);
                else
                    sceneObject.Rb.linearVelocity = new Vector3(Mathf.Cos(wallJumpData.JumpAngle * Mathf.Deg2Rad) * wallJumpData.JumpVelocity, Mathf.Sin(wallJumpData.JumpAngle * Mathf.Deg2Rad) * wallJumpData.JumpVelocity, sceneObject.Rb.linearVelocity.z);

                sceneObject.TurnAround();
            }
        }

        #endregion


        #region Perform Landing

        /// <summary>
        /// Performs any actions associated with landing
        /// </summary>
        private void PerformLanding()
        {
            TrySetCurrentMoveState(MovementType.Landing);

            //Reset jumps
            airJumpsPerformed = 0;
        }

        #endregion
    }
}
