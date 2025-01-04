using Game.SceneObjects.ActionStates;
using Game.SceneObjects.Attack;
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

        //Fast Fall Properties
        private const float fastFallAcceleration = 20f;
        private const float MaxYVelocity = 15f;

        //Movement Collection (NOTE: BaseMovementCollection is Required for all sceneObjects)    
        [Header("Collection")]
        [SerializeField] private MovementCollection baseMovementCollection;

        [Header("Influence")]
        [Range(-1, 1)]
        [SerializeField] private float horizontalInfluence;

        [Range(-1, 1)]
        [SerializeField] private float verticalInfluence;

        [SerializeField] private float jumpInfluence;

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

                //End movement after dash attack
                if (sceneObject.AttackInputHandler.TryGetCurAttackCollection(out AttackCollection curAttackCollection))
                {
                    if (curAttackCollection.TryGetAttackByAnimation(clip, out AttackData attackData))
                    {
                        if (attackData.Type == AttackType.Dash)
                            TrySetCurrentMoveState(MovementType.Null);
                    }
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

                if (sceneObject.CoreRigidBody.linearVelocity.x > 0)
                {
                    switch (sceneObject.CurGroundedState)
                    {
                        case GroundedState.Grounded:
                            if (sceneObject.TryGetSlopeAngle(out Vector3 slope))
                                sceneObject.CoreRigidBody.linearVelocity = slope * sceneObject.CoreRigidBody.linearVelocity.magnitude;
                            break;

                        case GroundedState.Airborn:
                            sceneObject.CoreRigidBody.linearVelocity = new Vector3(sceneObject.CoreRigidBody.linearVelocity.x * -1, sceneObject.CoreRigidBody.linearVelocity.y, sceneObject.CoreRigidBody.linearVelocity.z);
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
                            sceneObject.CoreRigidBody.linearVelocity = slope * sceneObject.CoreRigidBody.linearVelocity.magnitude;
                        break;

                    case GroundedState.Airborn:
                        sceneObject.CoreRigidBody.linearVelocity = new Vector3(sceneObject.CoreRigidBody.linearVelocity.x * -1, sceneObject.CoreRigidBody.linearVelocity.y, sceneObject.CoreRigidBody.linearVelocity.z);
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
            if (horizontalInfluence <= 0 && sceneObject.TryDetectCollision(Direction.Left, 0.5f, LayerMask.GetMask("Environment"), out _))
            {
                if (sceneObject.CoreRigidBody.linearVelocity.x <= 0 && sceneObject.IsFacingRightDirection())
                    sceneObject.TurnAround();

                return true;
            }

            //Right
            if (horizontalInfluence >= 0 && sceneObject.TryDetectCollision(Direction.Right, 0.5f, LayerMask.GetMask("Environment"), out _))
            {
                if (sceneObject.CoreRigidBody.linearVelocity.x >= 0 && !sceneObject.IsFacingRightDirection())
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
                //Grounded Movement
                if (sceneObject.CurGroundedState == GroundedState.Grounded)
                {
                    UpdateGroundedMovement(curMovementCollection);

                    if (IsAgainstGroundedWall())
                        TrySetCurrentMoveState(MovementType.WallLean);
                }

                //Air Movement
                else
                {
                    UpdateAirialMovement(curMovementCollection);

                    if (IsAgainstArialWall())
                        TrySetCurrentMoveState(MovementType.WallSlide);
                }
            }
        }


        /// <summary>
        /// Update movement on the ground
        /// </summary>
        private void UpdateGroundedMovement(MovementCollection curMovementCollection)
        {
            if (horizontalInfluence != 0)
            {
                //Calculate decceleration for dash attack
                if (sceneObject.ActionStateHandler.CurActionState == ActionState.Attacking &&
                    sceneObject.AttackInputHandler.TryGetCurAttackCollection(out AttackCollection curAttackCollection) &&
                    curAttackCollection.TryGetAttackByType(AttackType.Dash, out AttackData attack))
                {
                    float deceleration = sceneObject.CoreRigidBody.linearVelocity.magnitude / attack.AttackAnimation.length;
                    UpdateGroundDecceleration(deceleration);
                }

                else if (curMoveState == MovementType.Null || curMoveState == MovementType.WallLean)
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


        /// <summary>
        /// Update movement in the air
        /// </summary>
        private void UpdateAirialMovement(MovementCollection curMovementCollection)
        {
            if (horizontalInfluence != 0)
            {
                if (curMoveState == MovementType.Null || curMoveState == MovementType.WallSlide)
                    TrySetCurrentMoveState(MovementType.AirMove);

                else if (curMoveState == MovementType.AirMove ||
                    curMoveState == MovementType.AirJump)
                {
                    UpdateAirAcceleration(curMovementCollection.GetAerialXAcceleration(), curMovementCollection.GetAerialMaxVelocity());
                }
            }

            else
                UpdateAirDeceleration(curMovementCollection.GetAerialXDeceleration(), curMovementCollection.GetAerialMaxVelocity());
        }


        #region Acceleration   

        /// <summary>
        /// Accelerate on the ground by <paramref name="acceleration"/> value to a max <paramref name="maxVelocity"/>
        /// </summary>
        private void UpdateGroundAcceleration(float acceleration, float maxVelocity)
        {
            //Ground slope
            if (sceneObject.TryGetSlopeAngle(out Vector3 slope))
            {
                //Drag
                sceneObject.CoreRigidBody.linearDamping = 0;

                //Cap Velocity based on horizontal influence
                float targetXVelocity = maxVelocity * Mathf.Abs(horizontalInfluence);

                //Update velocity based on slope
                sceneObject.CoreRigidBody.linearVelocity += slope * Mathf.Abs(horizontalInfluence) * acceleration * Time.fixedDeltaTime;

                //Cant exceed target velocity
                if (sceneObject.CoreRigidBody.linearVelocity.magnitude > targetXVelocity)
                    sceneObject.CoreRigidBody.linearVelocity = slope * targetXVelocity;
            }
        }


        /// <summary>
        /// Accelerate throught the air by <paramref name="acceleration"/> value to a max <paramref name="maxVelocity"/> 
        /// </summary>
        private void UpdateAirAcceleration(float acceleration, float maxVelocity)
        {
            //Cap Velocity based on horizontal influence
            float targetXVelocity = maxVelocity * horizontalInfluence;

            sceneObject.CoreRigidBody.linearVelocity += Vector3.right * horizontalInfluence * acceleration * Time.fixedDeltaTime;

            //Cant exceed target velocity
            if ((horizontalInfluence > 0 && sceneObject.CoreRigidBody.linearVelocity.x > targetXVelocity) ||
                (horizontalInfluence < 0 && sceneObject.CoreRigidBody.linearVelocity.x < targetXVelocity))
            {
                sceneObject.CoreRigidBody.linearVelocity = new Vector3(targetXVelocity, sceneObject.CoreRigidBody.linearVelocity.y, sceneObject.CoreRigidBody.linearVelocity.z);
            }

            //TODO: Want to apply velocity change than check if its over for more consistant values
            //Vertical Movement
            if (verticalInfluence < 0f && sceneObject.CoreRigidBody.linearVelocity.y > -MaxYVelocity)
            {
                sceneObject.CoreRigidBody.linearVelocity += transform.up * verticalInfluence * fastFallAcceleration * Time.fixedDeltaTime;
            }
        }

        #endregion


        #region Decceleration

        /// <summary>
        /// Update velocity while on the ground to slow down by <paramref name="deccelerationValue"/>
        /// </summary>
        private void UpdateGroundDecceleration(float deccelerationValue)
        {
            //Prevet deccelerate when jumping from ground
            if (curMoveState != MovementType.Jump)
            {
                if (sceneObject.CoreRigidBody.linearVelocity.x != 0)
                {
                    Vector3 dragForce = sceneObject.CoreRigidBody.linearVelocity.normalized * deccelerationValue;
                    sceneObject.CoreRigidBody.linearVelocity -= dragForce * Time.fixedDeltaTime;

                    if ((sceneObject.IsFacingRightDirection() && sceneObject.CoreRigidBody.linearVelocity.x <= 0) ||
                        (!sceneObject.IsFacingRightDirection() && sceneObject.CoreRigidBody.linearVelocity.x >= 0))
                    {
                        sceneObject.CoreRigidBody.linearVelocity = Vector3.zero;

                        if (curMoveData != null)
                            sceneObject.AnimationHandler.EndAnimation(curMoveData.Animation);

                        TrySetCurrentMoveState(MovementType.Null);
                    }
                }
            }
        }


        /// <summary>
        /// Update velocity in the air to slow down by <paramref name="deccelerationValue"/>
        /// </summary>
        private void UpdateAirDeceleration(float deccelerationValue, float maxAerialVelocity)
        {
            float targetXVelocity = maxAerialVelocity;

            if (horizontalInfluence > 0)
                targetXVelocity *= horizontalInfluence;

            if (sceneObject.CoreRigidBody.linearVelocity.x > targetXVelocity)
                sceneObject.CoreRigidBody.linearVelocity -= Vector3.right * deccelerationValue * Time.fixedDeltaTime;

            else if (sceneObject.CoreRigidBody.linearVelocity.x < -targetXVelocity)
                sceneObject.CoreRigidBody.linearVelocity += Vector3.right * deccelerationValue * Time.fixedDeltaTime;
        }


        #endregion

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
                sceneObject.CoreRigidBody.linearVelocity = new Vector3(sceneObject.CoreRigidBody.linearVelocity.x, jumpData.JumpVelocity * jumpInfluence, sceneObject.CoreRigidBody.linearVelocity.z);
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
                sceneObject.CoreRigidBody.linearVelocity = new Vector3(sceneObject.CoreRigidBody.linearVelocity.x, airJumpData.AirJumpVelocity * jumpInfluence, sceneObject.CoreRigidBody.linearVelocity.z);
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
                    sceneObject.CoreRigidBody.linearVelocity = new Vector3((-1) * Mathf.Cos(wallJumpData.JumpAngle * Mathf.Deg2Rad) * wallJumpData.JumpVelocity, Mathf.Sin(wallJumpData.JumpAngle * Mathf.Deg2Rad) * wallJumpData.JumpVelocity, sceneObject.CoreRigidBody.linearVelocity.z);
                else
                    sceneObject.CoreRigidBody.linearVelocity = new Vector3(Mathf.Cos(wallJumpData.JumpAngle * Mathf.Deg2Rad) * wallJumpData.JumpVelocity, Mathf.Sin(wallJumpData.JumpAngle * Mathf.Deg2Rad) * wallJumpData.JumpVelocity, sceneObject.CoreRigidBody.linearVelocity.z);

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
