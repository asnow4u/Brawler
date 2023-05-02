using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SceneObj.Router;

namespace SceneObj.Animation
{
    public class AnimationHandler : MonoBehaviour
    {
        private ActionRouter router;
        protected Animator animator;

        private Coroutine finishAnimationCheck;

        public void SetUpHandler(ActionRouter actionRouter)
        {
            animator = GetComponentInChildren<Animator>();
            
            router = actionRouter;         
        }
     

        private void PlayAnimation(string animation)
        {
            animator.Play("Base Layer." + animation);

            if (finishAnimationCheck != null)
                StopCoroutine(finishAnimationCheck);
            
            finishAnimationCheck = StartCoroutine(CheckIfAnimationFinished(animation));            
        }


        private IEnumerator CheckIfAnimationFinished(string animation)
        {
            int animationHash = Animator.StringToHash(animation);

            while (animationHash != animator.GetCurrentAnimatorStateInfo(0).shortNameHash)
            {
                yield return null;
            }

            if (animator.GetCurrentAnimatorStateInfo(0).loop) yield break;

            while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1)
            {
                yield return null;
            }

            Debug.Log("Animation " + animation + " finished");

            router.ResetState();
        }


        public void PlayIdleAnimation()
        {
            if (router.IsGrounded())
                PlayAnimation("BaseIdle");
            else
                PlayAnimation("BaseAirIdle"); 
        }


        public void PlayMovementAnimation(string animation, float velocity)
        {
            animator.SetFloat("Velocity", velocity);
            PlayAnimation(animation);
        }


        //TODO: Should play stopping animation
        public void PlayMovementStopAnimation(string animation)
        {            
            router.ResetState();
        }


        public void PlayJumpAnimation(string animation)
        {
            PlayAnimation(animation);
        }

        public void PlayLandingAnimation(string animation)
        {
            router.ResetState();
        }


        public void PlayAttackAnimation(string animation)
        {
            PlayAnimation(animation);
        }        
    }
}

