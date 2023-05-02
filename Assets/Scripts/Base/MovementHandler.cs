using SceneObj.Router;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SceneObj.Movement
{
    public abstract class MovementHandler : MonoBehaviour
    {
        protected ActionRouter router;
        protected Rigidbody rb;
        protected MovementCollection curMovementCollection;

        [Header("Movement Speed")]
        [SerializeField] private float accelerationX;
        [SerializeField] private float decelerationX;
        [SerializeField] private float accelerationY;
        [Range(0,1)]
        [SerializeField] private float drag;
        [Range(0,1)]
        [SerializeField] private float airDrag;
        [Range(0, 20)]
        [SerializeField] private float maxVelocityX;
        [Range(0, 20)]
        [SerializeField] private float maxVelocityY;

        [Header("Jump")]
        [SerializeField] protected float jumpVelocity;
        [SerializeField] protected float fallVelocity;
    
        protected float horizontalInputValue;
        protected float verticalInputValue;


        public void SetUpHandler(ActionRouter actionRouter)
        {
            router = actionRouter;
            rb = GetComponent<Rigidbody>();
            SetUpMovements(transform.transform.GetChild(0).gameObject);
        }

        public void SetUpMovements(GameObject Weapon)
        {
            SetMovementCollection(Weapon.GetComponent<MovementCollection>());
        }

        private void SetMovementCollection(MovementCollection collection)
        {
            curMovementCollection = collection;
        }

        protected bool IsGrounded()
        {
            return router.IsGrounded();
        }


        protected virtual void FixedUpdate()
        {           
            if (IsGrounded())
            {            
                UpdateGroundMovement();
            }

            else
            {
                UpdateAirMovement();
            }            
        }


        private void UpdateGroundMovement()
        {      
            //Turn around
            if ((IsFacingRightDirection() && horizontalInputValue < 0) || (!IsFacingRightDirection() && horizontalInputValue > 0))
            {
                TurnAround();
            }
        
            //Accelerate
            if (Mathf.Abs(horizontalInputValue) > 0)
            {
                if (rb.velocity.x < maxVelocityX && rb.velocity.x > -maxVelocityX)
                {
                    rb.velocity += transform.right * Mathf.Abs(horizontalInputValue) * accelerationX * Time.fixedDeltaTime;                                    
                }
            }

            //Decelerate
            else
            {
                rb.velocity -= transform.right * decelerationX * Time.fixedDeltaTime;
                if (IsFacingRightDirection())
                {
                    if (rb.velocity.x < 0)
                        rb.velocity = new Vector3(0, rb.velocity.y, rb.velocity.z);            
                } 
                else
                {                
                    if (rb.velocity.x > 0)
                        rb.velocity = new Vector3(0, rb.velocity.y, rb.velocity.z);
                }
            }
        }

        private void UpdateAirMovement()
        {
            if (rb.velocity.x < maxVelocityX && rb.velocity.x > -maxVelocityX)
            {
                rb.velocity += transform.right * Mathf.Abs(horizontalInputValue) * accelerationX * Time.fixedDeltaTime;
            }
        }


        public string PerformMovementFrom(Vector2 axisInput)
        {
            if (curMovementCollection.GetMovementByType(MovementType.Type.move, out MovementCollection.Movement movement))
            {
                horizontalInputValue = axisInput.x;
                verticalInputValue = axisInput.y;

                return movement.animationClip.name;
            }

            return null;
        }

        //TODO: Should stop the player and return stop movement animation from movement collection
        public string PerformMovementStop()
        {
            horizontalInputValue = 0;
            verticalInputValue = 0;
            rb.velocity = Vector3.zero;

            return null;
        }

        public string PerformJump()
        {     
            return Jump();            
        }

        protected virtual string Jump()
        {
            if (curMovementCollection.GetMovementByType(MovementType.Type.jump, out MovementCollection.Movement movement))
            {
                rb.velocity = new Vector3(rb.velocity.x, jumpVelocity, rb.velocity.z);
                return movement.animationClip.name;
            }

            return null;
        }


        public void PerformFall()
        {
            rb.velocity = new Vector3(rb.velocity.x, rb.velocity.y / fallVelocity, rb.velocity.z);
        }

        //TODO: return animation from movement collection
        public string PerformLand()
        {
            horizontalInputValue = 0;
            verticalInputValue = 0;
            rb.velocity = Vector3.zero;

            return null;
        }

        public bool IsFacingRightDirection()
        {
            float angleRightDiff = Vector3.Angle(transform.right, Vector3.right);
            float angleLeftDiff = Vector3.Angle(transform.right, Vector3.left);

            if (angleRightDiff < angleLeftDiff)
            {
                return true;
            }

            return false;
        }

        public void TurnAround()
        {
            transform.Rotate(transform.up, 180f);
        }
    }
}
