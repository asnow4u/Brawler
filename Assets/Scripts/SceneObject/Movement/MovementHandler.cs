using Game.SceneObjects;
using Game.SceneObjects.Movement;
using UnityEngine;

namespace Game.SceneObjects.Movement
{
    public class MovementHandler
    {
        private SceneObject sceneObject;
        private Rigidbody rb => sceneObject.Rb;
        private MovementInputHandler inputHandler => sceneObject.MovementInputHandler;

        private SceneObjectData baseData;

        public MovementHandler(SceneObjectData movementData, SceneObject sceneObject)
        {
            this.sceneObject = sceneObject;
            this.baseData = movementData;
        }

        public void Setup()
        {
            if (sceneObject == null)
                throw new MissingReferenceException("MovementHandelr was not given a reference to a SceneObject");
            
            if (baseData == null)
                throw new MissingReferenceException("MovementHandelr was not given a reference to a MovementData ScriptableObject");
        }


        #region Getters

        public float GroundedMaxVelocity => Mathf.Lerp(baseData.GroundedMaxVelocityMin, baseData.GroundedMaxVelocityMax, 1 - (sceneObject.Mass / baseData.MaxMass));
        public float AerialMaxXVelocity => Mathf.Lerp(baseData.AerialMaxXVelocityMin, baseData.AerialMaxXVelocityMax, 1 - (sceneObject.Mass / baseData.MaxMass));
        public float AerialMaxYVelocity => Mathf.Lerp(baseData.AerialMaxYVelocityMin, baseData.AerialMaxYVelocityMax, 1- (sceneObject.Mass / baseData.MaxMass));
        public float GravityScaler => baseData.GravityScaler;
        public float GroundedDeccelerationRate => (baseData.GroundedDecceleration / sceneObject.Mass) * Time.fixedDeltaTime;
        public float AerialXDeccelerationRate => (baseData.AerialDecceleration / sceneObject.Mass) * Time.fixedDeltaTime;
        public float AerialYDeccelerationRate
        {
            get 
            {
                if (rb.linearVelocity.y > 0)
                    return (baseData.AerialDecceleration / sceneObject.Mass) - Physics.gravity.y * Time.fixedDeltaTime;
                else
                    return (baseData.AerialDecceleration / sceneObject.Mass) + Physics.gravity.y * Time.fixedDeltaTime;
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
                float angleRightDiff = Vector3.Angle(sceneObject.transform.right, Vector3.right);
                float angleLeftDiff = Vector3.Angle(sceneObject.transform.right, Vector3.left);

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
                sceneObject.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            else
                sceneObject.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

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
                if (inputHandler != null)
                    inputHandler.UpdateGroundedMovement(DeccelerateGroundedMovement);
                else
                    DeccelerateGroundedMovement();
            }

            //Aerial
            else if (sceneObject.CurGroundedState == GroundedState.Airborn)
            {
                if (inputHandler != null)
                    inputHandler.UpdateAerialMovement(DeccelerateAerialXMovement, DeccelerateAerialYMovement);

                else
                {
                    DeccelerateAerialXMovement();
                    DeccelerateAerialYMovement();
                }

                //Gravity
                ApplyGravityScaler();
            }

            //CLimbing
            else if (sceneObject.CurGroundedState == GroundedState.Climbing)
            {
                if (inputHandler != null)
                    inputHandler.UpdateClimbMovement();
                else
                    Debug.LogWarning("How did you start climbing without a MovementInputHandler?");
            }
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
