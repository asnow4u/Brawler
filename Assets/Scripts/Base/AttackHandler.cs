using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Animation;
using System;

namespace Attack
{
    public class AttackHandler : MonoBehaviour
    {
        private AttackCollection curAttackCollection;
        private AnimationHandler animator;

        public void SetUpHandler()
        {
            animator = GetComponentInChildren<AnimationHandler>();
            SetUpAttacks(transform.GetChild(0).gameObject);
        }

        public void SetUpAttacks(GameObject weapon)
        {
            SetAttackCollection(weapon.GetComponent<AttackCollection>());
            SetAnimationAttackEvents(weapon.GetComponent<AttackAnimationEventListener>());
        }

        private void SetAttackCollection(AttackCollection collection)
        {
            curAttackCollection = collection;
        }

        private void SetAnimationAttackEvents(AttackAnimationEventListener listener)
        {
            listener.AttackAnimationStarted += OnAttackAnimationStarted;
            listener.AttackAnimationEnded += OnAttackAnimationEnded;
            listener.AttackAnimationCollidersEnabled += OnAttackCollidersEnabled;
            listener.AttackAnimationCollidersDisabled += OnAttackCollidersDisabled;
        }

        #region Attack Performed Events

        private void OnAttackAnimationStarted(AnimationClip clip)
        {
            
        }

        private void OnAttackAnimationEnded(AnimationClip clip)
        {
            //TODO: Switch to correct idle state
        }

        private void OnAttackCollidersEnabled(AnimationClip clip)
        {
            if (curAttackCollection.GetAttackByClip(clip, out AttackCollection.Attack attack))
            {
                attack.EnableColliders();
            }
        }

        private void OnAttackCollidersDisabled(AnimationClip clip)
        {
            if (curAttackCollection.GetAttackByClip(clip, out AttackCollection.Attack attack))
            {
                attack.DisableColliders();
            }
        }

        #endregion

        #region Perform Attack

        private void PerformAttack(AttackType.Type attackType)
        {
            if (curAttackCollection.GetAttackByType(attackType, out AttackCollection.Attack attack))
            {
                animator.ChangeAnimationState(attack.animationClip, AnimationPriorityState.State.Attacking);
            }
        }

        public void PerformUpTiltAttack()
        {            
            PerformAttack(AttackType.Type.upTilt);
        }

        public void PerformDownTiltAttack()
        {
            PerformAttack(AttackType.Type.downTilt);
        }


        public void PerformForwardTiltAttack()
        {
            PerformAttack(AttackType.Type.forwardTilt);
        }

        public void PerformUpAirAttack()
        {
            PerformAttack(AttackType.Type.upAir);
        }

        public void PerformDownAirAttack()
        {
            PerformAttack(AttackType.Type.downAir);
        }

        public void PerformForwardAirAttack()
        {
            PerformAttack(AttackType.Type.forwardAir);
        }

        public void PerformBackAirAttack()
        {
            PerformAttack(AttackType.Type.backAir);
        }

        #endregion
    }
}
