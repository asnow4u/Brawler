using Game.SceneObjects;
using Game.SceneObjects.ActionStates;
using Game.SceneObjects.Movement;
using Game.UI.SceneObject;
using UnityEngine;

namespace Game.SceneObject.Movement
{
    public class MovementHandler : SceneObjectHandler
    {
        [SerializeField] private BaseMovementData baseMovementData;

        private Rigidbody rb => sceneObject.Rb;
        private MovementInputHandler inputHandler => sceneObject.MovementInputHandler;

        public override void Setup()
        {
            if (baseMovementData == null)
                Debug.LogException(new MissingReferenceException("MovementHandler Must Contain A BaseMovementData"));
        }

        public override void RegisterToEvents()
        { }

        public override void UnregisterToEvents()
        { }


        #region Getters

        public float GroundedMaxVelocity => baseMovementData.GroundedMaxVelocity;
        public float AerialMaxXVelocity => baseMovementData.AerialMaxXVelocity;
        public float AerialMaxYVelocity => baseMovementData.AerialMaxYVelocity;
        public float GravityScaler => baseMovementData.GravityScaler;
        public float GroundedDeccelerationRate => (baseMovementData.GroundedDecceleration / rb.mass) * Time.fixedDeltaTime;
        public float AerialXDeccelerationRate => (baseMovementData.AerialDecceleration / rb.mass) * Time.fixedDeltaTime;
        public float AerialYDeccelerationRate
        {
            get 
            {
                if (rb.linearVelocity.y > 0)
                    return (baseMovementData.AerialDecceleration / rb.mass) - Physics.gravity.y * Time.fixedDeltaTime;
                else
                    return (baseMovementData.AerialDecceleration / rb.mass) + Physics.gravity.y * Time.fixedDeltaTime;
            }
        }


        #endregion

        /// <returns>
        /// Whether the sceneObject is facing the right direction
        /// </returns>
        public bool IsFacingRightDirection
        {
            get
            {
                float angleRightDiff = Vector3.Angle(transform.right, Vector3.right);
                float angleLeftDiff = Vector3.Angle(transform.right, Vector3.left);

                if (angleRightDiff < angleLeftDiff)
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Turn the sceneObject around 
        /// </summary>
        public void TurnAround()
        {
            if (IsFacingRightDirection)
                transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            else
                transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

            sceneObject.UIHandler.RotateDisplayText();
        }

        /// <summary>
        /// Update the movement of the sceneObject
        /// </summary>
        public void UpdateMovement()
        {
            //Grounded
            if (sceneObject.CurGroundedState == GroundedState.Grounded)
            {
                if (inputHandler == null || !inputHandler.UpdateGroundedMovement())
                    DeccelerateGroundedMovement();
            }

            //Aerial
            else if (sceneObject.CurGroundedState == GroundedState.Airborn)
            {
                if (inputHandler == null || !inputHandler.UpdateAerialXMovement())
                    DeccelerateAerialXMovement();

                if (inputHandler == null || !inputHandler.UpdateAerialYMovement())
                    DeccelerateAerialYMovement();

                //Gravity
                ApplyGravityScaler();
            }

            //CLimbing
            //else if (curGroundedState == GroundedState.Climbing)
            //{
            //}

            //TODO: Update Climb State
        }

        /// <summary>
        /// Deccelerate grounded movement based on base deccelration value and the sceneObject's mass.
        /// </summary>
        private void DeccelerateGroundedMovement()
        {
            //Positive Decceleration
            if (rb.linearVelocity.x > 0)
            {
                float decceleratedXValue = rb.linearVelocity.x - GroundedDeccelerationRate;

                if (decceleratedXValue < 0)
                    decceleratedXValue = 0;

                rb.linearVelocity = new Vector3(decceleratedXValue, rb.linearVelocity.y, 0);
            }

            //Negative Decceleration
            else if (rb.linearVelocity.x < 0)
            {
                float decceleratedXValue = rb.linearVelocity.x + GroundedDeccelerationRate;

                if (decceleratedXValue > 0)
                    decceleratedXValue = 0;

                rb.linearVelocity = new Vector3(decceleratedXValue, rb.linearVelocity.y, 0);
            }
        }

        /// <summary>
        /// Deccelerate in the air on the X axis 
        /// </summary>
        private void DeccelerateAerialXMovement()
        {
            //Only deccelerate if velocity is greater than max velocity
            if (Mathf.Abs(rb.linearVelocity.x) > AerialMaxXVelocity)
            {
                //Positive Decceleration
                if (rb.linearVelocity.x > 0)
                {
                    float decceleratedXValue = rb.linearVelocity.x - AerialXDeccelerationRate;

                    if (decceleratedXValue < 0)
                        decceleratedXValue = 0;

                    rb.linearVelocity = new Vector3(decceleratedXValue, rb.linearVelocity.y, 0);
                }

                //Negative Decceleration
                else if (rb.linearVelocity.x < 0)
                {
                    float decceleratedXValue = rb.linearVelocity.x + AerialXDeccelerationRate;

                    if (decceleratedXValue > 0)
                        decceleratedXValue = 0;

                    rb.linearVelocity = new Vector3(decceleratedXValue, rb.linearVelocity.y, 0);
                }
            }
        }

        /// <summary>
        /// Deccelerate in the air on the Y axis 
        /// </summary>
        private void DeccelerateAerialYMovement()
        {
            if (Mathf.Abs(rb.linearVelocity.y) > AerialMaxYVelocity)
            {
                //Positive Decceleration
                if (rb.linearVelocity.y > 0)
                {
                    float decceleratedYValue = rb.linearVelocity.y - AerialYDeccelerationRate;

                    if (decceleratedYValue < 0)
                        decceleratedYValue = 0;

                    rb.linearVelocity = new Vector3(rb.linearVelocity.x, decceleratedYValue, 0);
                }

                //Negative Decceleration
                else if (rb.linearVelocity.y < 0)
                {
                    float decceleratedYValue = rb.linearVelocity.y + AerialYDeccelerationRate;

                    if (decceleratedYValue > 0)
                        decceleratedYValue = 0;

                    rb.linearVelocity = new Vector3(rb.linearVelocity.x, decceleratedYValue, 0);
                }
            }
        }

        /// <summary>
        /// Apply additional downward velocity ontop of gravity based on <paramref name="airMoveData"/>
        /// </summary>
        private void ApplyGravityScaler()
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y + (Physics.gravity.y * GravityScaler * Time.fixedDeltaTime), 0);
        }
    }
}
