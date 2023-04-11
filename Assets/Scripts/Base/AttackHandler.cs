using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Attack
{
    public class AttackHandler : MonoBehaviour
    {
        protected AttackCollection curAttackCollection;
        protected AnimatorStateMachine animator;

        void Start()
        {
            animator = GetComponentInChildren<AnimatorStateMachine>();
        }

        public void SetCollection(AttackCollection collection)
        {
            curAttackCollection = collection;
        }

        public void PerformUpTiltAttack()
        {
 
            if (curAttackCollection.GetAttackByType(AttackType.Type.upTilt, out AttackCollection.Attack attack))
            {
                //animator.ChangeAnimationState("UpTilt", AnimatorStateMachine.ActionState.Attacking);
            }
        }

        public void PerformDownTiltAttack()
        {
            //    animator.ChangeAnimationState("DownTilt", AnimatorStateMachine.ActionState.Attacking);
        }


        public void PerformForwardTiltAttack()
        {
            //animator.ChangeAnimationState("ForwardTilt", AnimatorStateMachine.ActionState.Attacking);
        }

        public void PerformUpAirAttack()
        {
            if (curAttackCollection.GetAttackByType(AttackType.Type.upAir, out AttackCollection.Attack attack))
            {
                //animator.ChangeAnimationState("UpAir", AnimatorStateMachine.ActionState.Attacking);
            }
        }

        public void PerformDownAirAttack()
        {
            //    animator.ChangeAnimationState("DownAir", AnimatorStateMachine.ActionState.Attacking);

        }

        public void PerformForwardAirAttack()
        {
            //animator.ChangeAnimationState("ForwardAir", AnimatorStateMachine.ActionState.Attacking);
        }

        public void PerformBackAirAttack()
        {
            //  animator.ChangeAnimationState("BackAir", AnimatorStateMachine.ActionState.Attacking);
        }


        public void AttackStarted(string attackName)
        {

        }

        public void AttackEnded(string attackName)
        {

        }

        public void AttackCollidersEnabled()
        {
      
        }

        public void AttackCollidersDisabled()
        {
 
        }

    }
}
