using Game.SceneObjects.ActionStates;
using Game.SceneObjects.Movement;
using System;
using UnityEngine;

namespace Game.SceneObjects.Attack
{
    public enum AttackType { Null, UpTilt, DownTilt, ForwardTilt, UpAir, DownAir, ForwardAir };

    public class AttackInputHandler : SceneObjectHandler
    {
        const ActionState ATTACKSTATE = ActionState.Attacking;        

        [Header("State")]
        //Attack Data
        //NOTE: This tracks what attack is currently happening. This prevents multiple attacks from overwriting one another before an attack animation starts
        [SerializeField] private AttackType curAttackState;
        //NOTE: This tracks the current attack data being used
        private AttackData curAttackData;
        //NOTE: This tracks any buffered attack
        private Action bufferedAttackAction = null;

        //Events
        public event Action<AttackType> AttackStateChangedEvent;


        #region Getters

        public AttackData CurAttackData => curAttackData;

        public bool TryGetCurrentAttackCollection(out AttackCollection curAttackCollection)
        {
            curAttackCollection = null;

            if (sceneObject.EquipmentHandler.CurWeapon != null)
                curAttackCollection = sceneObject.EquipmentHandler.CurWeapon.AttackCollection;

            return curAttackCollection != null;
        }

        #endregion


        #region Initialize    

        public override void RegisterToEvents()
        {
            //Animation Events
            sceneObject.AnimationHandler.AnimationStartedEvent += OnAnimationStarted;
            sceneObject.AnimationHandler.AnimationEndedEvent += OnAnimationEnded;
        }


        public override void UnregisterToEvents()
        {
            //Animation Events
            sceneObject.AnimationHandler.AnimationStartedEvent -= OnAnimationStarted;
            sceneObject.AnimationHandler.AnimationEndedEvent -= OnAnimationEnded;
        }

        #endregion

        #region Events

        /// <summary>
        /// Check for attack animation
        /// </summary>
        private void OnAnimationStarted(AnimationClip clip)
        {
            if (TryGetCurrentAttackCollection(out AttackCollection curAttackCollection))
            {
                if (curAttackCollection.TryGetAttackByAnimation(clip, out AttackData attackData))
                {
                    curAttackData = attackData;

                    foreach (AnimationTrigger trigger in attackData.GetAttackTriggers())
                        trigger.Reset();
                }
            }
        }


        /// <summary>
        /// Check if attack animation ended
        /// </summary>
        private void OnAnimationEnded(AnimationClip clip)
        {
            if (TryGetCurrentAttackCollection(out AttackCollection curAttackCollection))
            {
                if (curAttackCollection.TryGetAttackByAnimation(clip, out AttackData attackData))
                {
                    //Check if activly in an attack
                    if (curAttackData != null)
                        SetCurrentAttackState(AttackType.Null, curAttackCollection);
                }
            }
        }

        #endregion


        /// <summary>
        /// Each Update frame check any attack animation triggers that need to invoke
        /// </summary>
        public void HandleUpdate()
        {
            if (TryGetCurrentAttackCollection(out AttackCollection curAttackCollection))
                CheckForAnimationTriggers();
        }



        #region Attack State

        /// <summary>
        /// Attempt to set the current attack state <br></br>
        /// This will initiate the animation of the attackType
        /// </summary>
        /// <param name="attackType"></param>
        /// <returns></returns>
        private void SetCurrentAttackState(AttackType attackType, AttackCollection curAttackCollection)
        {            
            if (attackType != AttackType.Null)
            {
                //Check not currently attacking and Check that attack exists
                if (curAttackCollection.TryGetAttackByType(attackType, out AttackData attack))
                {
                    //Change state
                    if (sceneObject.ActionStateHandler.TryChangeState(ATTACKSTATE))
                    {
                        curAttackState = attackType;
                        AttackStateChangedEvent?.Invoke(attackType);
                    }
                }
            }

            else
            {
                curAttackState = AttackType.Null;
                curAttackData = null;
                AttackStateChangedEvent?.Invoke(AttackType.Null);
            }
        }

        #endregion


        #region Perform Attack

        /// <summary>
        /// Try to perform a grounded / Air Up attack
        /// </summary>
        public void PerformUpAttack()
        {
            if (curAttackState == AttackType.Null && TryGetCurrentAttackCollection(out AttackCollection curAttackCollection))
            {
                if (sceneObject.CurGroundedState == GroundedState.Grounded)
                    SetCurrentAttackState(AttackType.UpTilt, curAttackCollection);                            

                else
                    SetCurrentAttackState(AttackType.UpAir, curAttackCollection);
            }
        }


        /// <summary>
        /// Try to perform a grounded / Air Down attack
        /// </summary>
        public void PerformDownAttack()
        {
            if (curAttackState == AttackType.Null && TryGetCurrentAttackCollection(out AttackCollection curAttackCollection))
            {
                if (sceneObject.CurGroundedState == GroundedState.Grounded)
                    SetCurrentAttackState(AttackType.DownTilt, curAttackCollection);

                else
                    SetCurrentAttackState(AttackType.DownAir, curAttackCollection);
            }
        }


        /// <summary>
        /// Try to perform a grounded / Air Forward attack <\br>
        /// Turn around if facing the wrong direction
        /// </summary>
        public void PerformRightAttack()
        {
            if (curAttackState == AttackType.Null && TryGetCurrentAttackCollection(out AttackCollection curAttackCollection))
            {                
                if (sceneObject.CurGroundedState == GroundedState.Grounded)
                {
                    SetCurrentAttackState(AttackType.ForwardTilt, curAttackCollection);

                    if (!sceneObject.IsFacingRightDirection())
                        sceneObject.TurnAround();                   
                }
                    
                else
                {
                    SetCurrentAttackState(AttackType.ForwardAir, curAttackCollection);

                    if (!sceneObject.IsFacingRightDirection())
                        sceneObject.TurnAround();
                }
            }
        }


        /// <summary>
        /// Try to perform a grounded / Air Forward attack <\br>
        /// Turn around if facing the wrong direction
        /// </summary>
        public void PerformLeftAttack()
        {
            if (curAttackState == AttackType.Null && TryGetCurrentAttackCollection(out AttackCollection curAttackCollection))
            {
                if (sceneObject.CurGroundedState == GroundedState.Grounded)
                {
                    SetCurrentAttackState(AttackType.ForwardTilt, curAttackCollection);

                    if (sceneObject.IsFacingRightDirection())
                        sceneObject.TurnAround();
                }

                else
                {
                    SetCurrentAttackState(AttackType.ForwardAir, curAttackCollection);

                    if (sceneObject.IsFacingRightDirection())
                        sceneObject.TurnAround();
                }                
            }
        }

        #endregion


        #region Animation Triggers

        /// <summary>
        /// Check for animation triggers that need to fire
        /// </summary>
        private void CheckForAnimationTriggers()
        {
            if (sceneObject.ActionStateHandler.CurActionState == ActionState.Attacking && curAttackData != null)
            {
                int curAnimationFrame = sceneObject.AnimationHandler.GetFrameOfCurrentAnimation();

                foreach (AnimationTrigger trigger in curAttackData.GetAttackTriggers())
                {
                    if (!trigger.WasTriggered && curAnimationFrame >= trigger.TriggerFrame)
                        ExecuteTrigger(trigger);
                }
            }
        }


        /// <summary>
        /// Execute animation trigger
        /// </summary>
        /// <param name="trigger"></param>
        private void ExecuteTrigger(AnimationTrigger trigger)
        {
            trigger.WasTriggered = true;

            switch (trigger.TriggerType)
            {
                case AnimationTrigger.Type.EnableCollider:
                    sceneObject.EquipmentHandler.CurWeapon.EnableCollidersForAttack(curAttackData, AttackConnected);
                    break;

                case AnimationTrigger.Type.DisableCollider:
                    sceneObject.EquipmentHandler.CurWeapon.DisableAllColliders();
                    break;
            }
        }


        /// <summary>
        /// Callback used when an attack makes contact with a <paramref name="col"/> of <paramref name="target"/>
        /// </summary>
        /// <param name="target"></param>
        private void AttackConnected(ITakeDamage target, Collider col)
        {
            //Current Attack
            if (curAttackData != null)
            {
                //Attack Details
                SceneObject sceneObject = GetComponentInParent<SceneObject>();
                int curFrame = sceneObject.AnimationHandler.GetFrameOfCurrentAnimation();
                float launchAngle = curAttackData.GetAttackLaunchAngle(curFrame);

                //Reverse launch angle
                if (!sceneObject.IsFacingRightDirection())
                    launchAngle = 180 - launchAngle;

                target.HitByAttack(curAttackData.GetInfluence(), col.ClosestPoint(col.transform.position), curAttackData.GetAttackDamage(curFrame), launchAngle);
            }
        }

        #endregion
    }
}
