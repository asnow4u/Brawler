using SceneObj.Animation;
using SceneObj.Attack;
using SceneObj.Movement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SceneObj.Router
{
    public class ActionRouter
    {
        public ActionState actionState { get;}

        private SceneObject sceneObject;
        private MovementHandler movementHandler;
        private AttackHandler attackHandler;
        private AnimationHandler animationHandler;


        public ActionRouter(SceneObject sceneObject, MovementHandler movementHandler, AttackHandler attackHandler, AnimationHandler animationHandler)
        {
            actionState = new ActionState();
            actionState.ChangeState(ActionState.State.Idle);
        
            this.sceneObject = sceneObject;
            
            if (movementHandler != null)
            {
                this.movementHandler = movementHandler;
                this.movementHandler.SetUpHandler(this);
            }

            if (attackHandler != null)
            {
                this.attackHandler = attackHandler;
                this.attackHandler.SetUpHandler(this);
            }

            if (animationHandler != null)
            {
                this.animationHandler = animationHandler;
                this.animationHandler.SetUpHandler(this);
            }
        }

        public bool IsGrounded()
        {
            return sceneObject.isGrounded;
        }

        public bool IsFacingRightDirection()
        {
            return sceneObject.IsFacingRightDirection();
        }

        public void ResetState()
        {
            actionState.ResetState();
            animationHandler.PlayIdleAnimation();
        }


        #region Movement        

        public void OnHorizontalMovement(Vector2 movement)
        {
            if (actionState.ChangeState(ActionState.State.Moving))
            {
                animationHandler.PlayMovementAnimation(movementHandler.PerformMovementFrom(movement), Mathf.Abs(movement.x));
            }
        }

        public void OnHorizontalMovementStopped()
        {
            if (actionState.ChangeState(ActionState.State.Moving))
            {
                animationHandler.PlayMovementStopAnimation(movementHandler.PerformMovementStop());
            }
        }

        public void OnJump()
        {
            if (actionState.ChangeState(ActionState.State.Moving))
            {
                animationHandler.PlayJumpAnimation(movementHandler.PerformJump());               
            }
        }

        public void OnJumpCanceled()
        {
            if (actionState.ChangeState(ActionState.State.Moving))
            {
                movementHandler.PerformFall();
            }
        }
        
        public void OnLand(float fallVelocity)
        {
            if (actionState.ChangeState(ActionState.State.Moving))
            {
                if (movementHandler != null)
                    animationHandler.PlayLandingAnimation(movementHandler.PerformLand());                
            }
        }

        #endregion


        #region Attack

        public void OnUpAttack()
        {
            if (actionState.ChangeState(ActionState.State.Attacking))
            {
                if (sceneObject.isGrounded)
                {
                    animationHandler.PlayAttackAnimation(attackHandler.PerformUpTiltAttack());
                }
                else
                {
                    animationHandler.PlayAttackAnimation(attackHandler.PerformUpAirAttack());
                }
            }
        }

        public void OnDownAttack()
        {
            if (actionState.ChangeState(ActionState.State.Attacking))
            {
                if (sceneObject.isGrounded)
                {
                    animationHandler.PlayAttackAnimation(attackHandler.PerformDownTiltAttack());
                }
                else
                {
                    animationHandler.PlayAttackAnimation(attackHandler.PerformDownAirAttack());
                }
            }
        }

        public void OnRightAttack()
        {
            if (actionState.ChangeState(ActionState.State.Attacking))
            {
                if (sceneObject.isGrounded)
                {
                    if (sceneObject.IsFacingRightDirection())
                    {
                        animationHandler.PlayAttackAnimation(attackHandler.PerformForwardTiltAttack());
                    }

                    else
                    {
                        movementHandler.TurnAround();
                        animationHandler.PlayAttackAnimation(attackHandler.PerformForwardTiltAttack());
                    }
                }
                else
                {
                    if (sceneObject.IsFacingRightDirection())
                    {
                        animationHandler.PlayAttackAnimation(attackHandler.PerformForwardAirAttack());
                    }

                    else
                    {
                        animationHandler.PlayAttackAnimation(attackHandler.PerformBackAirAttack());
                    }
                }
            }
        }


        public void OnLeftAttack()
        {
            if (actionState.ChangeState(ActionState.State.Attacking))
            {
                if (sceneObject.isGrounded)
                {
                    if (sceneObject.IsFacingRightDirection())
                    {
                        movementHandler.TurnAround();
                        animationHandler.PlayAttackAnimation(attackHandler.PerformForwardTiltAttack());
                    }

                    else
                    {
                        animationHandler.PlayAttackAnimation(attackHandler.PerformForwardTiltAttack());
                    }
                }
                else
                {
                    if (sceneObject.IsFacingRightDirection())
                    {
                        animationHandler.PlayAttackAnimation(attackHandler.PerformBackAirAttack());
                    }

                    else
                    {
                        animationHandler.PlayAttackAnimation(attackHandler.PerformForwardAirAttack());
                    }
                }
            }
        }

        #endregion


        public void OnDamaged()
        {
            if (actionState.ChangeState(ActionState.State.Damaged))
            {

            }
        }

    }
}
