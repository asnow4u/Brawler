using SceneObj.Movement;
using UnityEngine;

namespace SceneObj.Movement
{
    public class PlayerMovement : MovementInputHandler
    {
        [Header("Player Jump")]
        [SerializeField] private int jumpsAvailable = 2;
        [SerializeField] private float airJumpVelocity = 7f;
        private int additionalJumpsPerformed;

        [Header("FastFall")]
        [SerializeField] private float fastFallVelocity = 1f;

        // Update is called once per frame
        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            if (isGrounded)
            {
                additionalJumpsPerformed = 0;
            }
        }


        protected override void Jump()
        {
            if (isGrounded)
                base.Jump();

            else
            {
                if (additionalJumpsPerformed < jumpsAvailable)
                {
                    if (curMovementCollection.GetMovementByType(MovementType.Type.airJump, out MovementCollection.Movement movement))
                    {
                        rb.velocity = new Vector3(rb.velocity.x, airJumpVelocity, rb.velocity.z);
                        additionalJumpsPerformed++;

                        sceneObj.animator.PlayAnimation(movement.animationClip.name);
                    }
                }
            }       
        }


        protected override void UpdateAirMovement()
        {
            base.UpdateAirMovement();

            if (verticalInputValue < 0f)
            {
                rb.velocity += transform.up * verticalInputValue * fastFallVelocity * Time.fixedDeltaTime;
            }
        }
    }
}
