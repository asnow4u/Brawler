using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Animation
{
    public class AnimationHandler : MonoBehaviour
    {
        protected AnimationPriorityState.State state;

        protected Animator animator;

        public void SetUpHandler()
        {
            animator = GetComponent<Animator>();

            state = AnimationPriorityState.State.Idle;
        }

        private bool CheckAnimationPriority(AnimationPriorityState.State newState)
        {
            if ((int)newState > (int)state)
            {
                return true;
            }

            return false;
        }

        public void ChangeAnimationState(AnimationClip clip, AnimationPriorityState.State newState)
        {
            if (CheckAnimationPriority(newState))
            {
                animator.Play(clip.name);                
                state = newState;
            }
        }
    }
}

