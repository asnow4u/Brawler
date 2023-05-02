using SceneObj.Movement;
using UnityEngine;

namespace SceneObj.Movement
{
    public class PlayerMovement : MovementHandler
    {
        [Header("Jump")]
        [SerializeField] private int jumpsAvailable;
        [SerializeField] private float airJumpVelocity;
        private int additionalJumpsPerformed;

        // Update is called once per frame
        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            if (IsGrounded())
            {
                additionalJumpsPerformed = 0;
            }
        }



        protected override string Jump()
        {
            if (IsGrounded())
                return base.Jump();

            else
            {
                if (additionalJumpsPerformed < jumpsAvailable)
                {
                    if (curMovementCollection.GetMovementByType(MovementType.Type.airJump, out MovementCollection.Movement movement))
                    {
                        rb.velocity = new Vector3(rb.velocity.x, airJumpVelocity, rb.velocity.z);
                        additionalJumpsPerformed++;

                        return movement.animationClip.name;
                    }
                }

                return null;
            }       
        }
    }
}
