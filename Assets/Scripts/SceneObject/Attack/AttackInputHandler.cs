using Game.SceneObjects.ActionStates;
using Game.SceneObjects.Movement;
using NUnit.Framework;
using System;
using System.Collections.Generic;
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
        //NOTE: THis tracks what attack happend before the current attack.
        [SerializeField] private AttackType previousAttackState;

        //NOTE: This tracks the current attack data being used
        private AttackData curAttackData;

        //NOTE: This is the max amount of time that this attack can be held before it is released
        private const float MAX_ATTACK_CHARGE_TIME = 1f;
        private float attackChargeTime = 0f;

        //NOTE: This is the max amount of additional damage that can be added to the attack (30%)
        private const float Max_CHARGE_ATTACK_MULTIPLIER = 1f;
        private float chargeAttackMultiplier = 0;

        private HashSet<ITakeDamage> objectHitByAttack = new HashSet<ITakeDamage>();


        /// <summary>
        /// Event that is fired when an attack is performed. <br/>
        /// The first attackType defines the current attack state <br/>
        /// The second attackType defines the attack to transition to <br/>
        /// </summary>
        public event Action<AttackType, AttackType> AttackStateChangedEvent;


        #region Getters

        public AttackData CurAttackData => curAttackData;

        public bool TryGetCurrentAttackCollection(out AttackCollection curAttackCollection)
        {
            curAttackCollection = null;

            if (sceneObject.EquipmentHandler.WeaponHandler.EquippedWeapon != null)
                curAttackCollection = sceneObject.EquipmentHandler.WeaponHandler.EquippedWeapon.AttackCollection;

            return curAttackCollection != null;
        }

        #endregion


        #region Initialize    

        public override void RegisterToEvents()
        {
            //Animation Events
            sceneObject.GroundedStateChangedEvent += OnGroundedStateChanged;
            sceneObject.AnimationHandler.AnimationStartedEvent += OnAnimationStarted;
            sceneObject.AnimationHandler.AnimationEndedEvent += OnAnimationEnded;
        }

        public override void UnregisterToEvents()
        {
            //Animation Events
            sceneObject.AnimationHandler.AnimationStartedEvent -= OnAnimationStarted;
            sceneObject.AnimationHandler.AnimationEndedEvent -= OnAnimationEnded;
        }

        public override void Setup()
        { }

        #endregion

        #region Events

        /// <summary>
        /// Handle grounded state changes while performing an aerial attack
        /// </summary>
        private void OnGroundedStateChanged(GroundedState groundedState)
        {
            if (groundedState == GroundedState.Grounded && curAttackState != AttackType.Null && TryGetCurrentAttackCollection(out AttackCollection curAttackCollection))
            {
                switch (curAttackState)
                {
                    case AttackType.UpAir:
                        if (sceneObject.IsUpAttackActive())
                            SetCurrentAttackState(AttackType.UpTilt, curAttackCollection);
                        else
                            SetCurrentAttackState(AttackType.Null, curAttackCollection);
                        break;

                    case AttackType.ForwardAir:
                        if (sceneObject.IsRightAttackActive() || sceneObject.IsLeftAttackActive())
                            SetCurrentAttackState(AttackType.ForwardTilt, curAttackCollection);
                        else
                            SetCurrentAttackState(AttackType.Null, curAttackCollection);
                        break;

                    case AttackType.DownAir:
                        if (sceneObject.IsDownAttackActive())
                            SetCurrentAttackState(AttackType.DownTilt, curAttackCollection);
                        else
                            SetCurrentAttackState(AttackType.Null, curAttackCollection);
                        break;
                }
            }
        }


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
                    //Handle the case where the current attack animation is ended (actionState change)
                    if (curAttackState == attackData.Type)
                    {
                        SetCurrentAttackState(AttackType.Null, curAttackCollection);

                        attackChargeTime = 0;
                        chargeAttackMultiplier = 0;
                    }
                }
            }
        }

        #endregion


        /// <summary>
        /// Each Update frame check any attack animation triggers that need to invoke
        /// </summary>
        public void HandleUpdate()
        {
            if (sceneObject.ActionStateHandler.CurActionState == ActionState.Attacking && curAttackData != null)
            {
                //Check Collision
                if (TryGetCurrentAttackCollection(out AttackCollection curAttackCollection))
                    CheckForAnimationTriggers();

                //Charge Attack
                UpdateChargeAttack();
                
            }
        }



        #region Attack State

        /// <summary>
        /// Attempt to set the current attack state <br></br>
        /// This will initiate the animation of the attackType
        /// </summary>
        private void SetCurrentAttackState(AttackType attackType, AttackCollection curAttackCollection)
        {            
            if (attackType != AttackType.Null)
            {
                if (curAttackCollection.TryGetAttackByType(attackType, out AttackData attack) && 
                    sceneObject.ActionStateHandler.TryChangeState(ATTACKSTATE))
                {
                    previousAttackState = curAttackState;
                    curAttackState = attackType;
                    AttackStateChangedEvent?.Invoke(previousAttackState, curAttackState);
                }
            }

            else
            {
                previousAttackState = curAttackState;
                curAttackState = AttackType.Null;
                curAttackData = null;
                objectHitByAttack.Clear();
                sceneObject.ActionStateHandler.ChangeState(ActionState.Idle);
                AttackStateChangedEvent?.Invoke(previousAttackState, curAttackState);
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
        public void PerformForwardAttack()
        {
            if (curAttackState == AttackType.Null && TryGetCurrentAttackCollection(out AttackCollection curAttackCollection))
            {                
                if (sceneObject.CurGroundedState == GroundedState.Grounded)
                {
                    //NOTE: Resets y velocity. This helps prevent an areal grounded attack if performed on first few frame of jump
                    sceneObject.Rb.linearVelocity = new Vector3(sceneObject.Rb.linearVelocity.x, 0, 0);

                    SetCurrentAttackState(AttackType.ForwardTilt, curAttackCollection);

                    if (!sceneObject.IsFacingRightDirection)
                        sceneObject.TurnAround();                   
                }
                    
                else
                {
                    SetCurrentAttackState(AttackType.ForwardAir, curAttackCollection);

                    if (!sceneObject.IsFacingRightDirection)
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

                    if (sceneObject.IsFacingRightDirection)
                        sceneObject.TurnAround();
                }

                else
                {
                    SetCurrentAttackState(AttackType.ForwardAir, curAttackCollection);

                    if (sceneObject.IsFacingRightDirection)
                        sceneObject.TurnAround();
                }                
            }
        }

        #endregion


        #region Charged Attack

        /// <summary>
        /// Determine if a charged attack needs to be released
        /// </summary>
        private void UpdateChargeAttack()
        {
            //TODO: Should check if that animation is an attack
            if (sceneObject.AnimationHandler.IsAnimationPaused)
            {
                attackChargeTime += Time.fixedDeltaTime;
                chargeAttackMultiplier = Mathf.Lerp(0, Max_CHARGE_ATTACK_MULTIPLIER, attackChargeTime / MAX_ATTACK_CHARGE_TIME);

                switch (curAttackState)
                {
                    case AttackType.UpTilt:
                    case AttackType.UpAir:
                        ReleaseUpChargeAttack();                        
                        break;

                    case AttackType.ForwardTilt:
                    case AttackType.ForwardAir:
                        ReleaseForwardChargeAttack();
                        break;

                    case AttackType.DownTilt:
                    case AttackType.DownAir:
                        ReleaseDownChargeAttack();
                        break;
                }
            }
        }


        /// <summary>
        /// Perform a charged upward attack. <br/>
        /// Pauses animation till release. <br/>
        /// Fired from animationTrigger
        /// </summary>
        private void PerformUpChargeAttack()
        {
            if (sceneObject.IsUpAttackActive())
                sceneObject.AnimationHandler.PauseCurrentAnimation(MAX_ATTACK_CHARGE_TIME);
        }

        /// <summary>
        /// Release charged upward attack
        /// </summary>
        private void ReleaseUpChargeAttack()
        {
            if (!sceneObject.IsUpAttackActive())
                sceneObject.AnimationHandler.ResumeCurrentAnimation();
        }

        /// <summary>
        /// Perform a charged forward attack. <br/>
        /// Pauses animation till release. <br/>
        /// Fired from animationTrigger
        /// </summary>
        private void PerformForwardChangeAttack()
        {
            if (sceneObject.IsRightAttackActive() && sceneObject.IsFacingRightDirection)
                sceneObject.AnimationHandler.PauseCurrentAnimation(MAX_ATTACK_CHARGE_TIME);


            else if (sceneObject.IsLeftAttackActive() && !sceneObject.IsFacingRightDirection)
                sceneObject.AnimationHandler.PauseCurrentAnimation(MAX_ATTACK_CHARGE_TIME);
        }

        /// <summary>
        /// Release charged forward attack
        /// </summary>
        private void ReleaseForwardChargeAttack()
        {
            if (!sceneObject.IsRightAttackActive() && sceneObject.IsFacingRightDirection)
                sceneObject.AnimationHandler.ResumeCurrentAnimation();

            else if (!sceneObject.IsLeftAttackActive() && !sceneObject.IsFacingRightDirection)
                sceneObject.AnimationHandler.ResumeCurrentAnimation();
        }


        /// <summary>
        /// Perform a charged downward attack. <br/>
        /// Pauses animation till release. <br/>
        /// Fired from animationTrigger
        /// </summary>
        private void PerformDownChargeAttack()
        {
            if (sceneObject.IsDownAttackActive())
                sceneObject.AnimationHandler.PauseCurrentAnimation(MAX_ATTACK_CHARGE_TIME);
        }


        /// <summary>
        /// Release charged downward attack
        /// </summary>
        private void ReleaseDownChargeAttack()
        {
            if (!sceneObject.IsDownAttackActive())
                sceneObject.AnimationHandler.ResumeCurrentAnimation();
        }


        #endregion

        #region Animation Triggers

        /// <summary>
        /// Check for animation triggers that need to fire
        /// </summary>
        private void CheckForAnimationTriggers()
        {
            int curAnimationFrame = sceneObject.AnimationHandler.GetFrameOfCurrentAnimation();

            foreach (AnimationTrigger trigger in curAttackData.GetAttackTriggers())
            {
                if (!trigger.WasTriggered && curAnimationFrame >= trigger.TriggerFrame)
                    ExecuteTrigger(trigger);
            }
        }


        /// <summary>
        /// Execute <paramref name="trigger"/>
        /// </summary>
        private void ExecuteTrigger(AnimationTrigger trigger)
        {
            trigger.WasTriggered = true;

            switch (trigger.TriggerType)
            {
                case AnimationTriggerType.EnableCollider:
                    sceneObject.EquipmentHandler.WeaponHandler.EquippedWeapon.EnableCollidersForAttack(curAttackData, AttackConnected);
                    break;

                case AnimationTriggerType.DisableCollider:
                    sceneObject.EquipmentHandler.WeaponHandler.EquippedWeapon.DisableAllColliders();
                    break;

                case AnimationTriggerType.ChargeAction:
                    switch (curAttackState)
                    {
                        case AttackType.UpTilt:
                        case AttackType.UpAir:
                            PerformUpChargeAttack();
                            break;

                        case AttackType.ForwardTilt:
                        case AttackType.ForwardAir:
                            PerformForwardChangeAttack();
                            break;

                        case AttackType.DownTilt:
                        case AttackType.DownAir:
                            PerformDownChargeAttack();
                            break;
                    }
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
            if (curAttackData != null && !objectHitByAttack.Contains(target))
            {
                objectHitByAttack.Add(target);

                //Attack Details
                SceneObject sceneObject = GetComponentInParent<SceneObject>();
                int curFrame = sceneObject.AnimationHandler.GetFrameOfCurrentAnimation();

                //Launch Angle
                float launchAngle = curAttackData.LaunchAngle;
                if (!sceneObject.IsFacingRightDirection)
                    launchAngle = 180 - launchAngle;

                //Attack Damage
                float attackDamage = curAttackData.GetAttackDamage(curFrame);
                attackDamage += attackDamage * chargeAttackMultiplier;

                target.HitByAttack(curAttackData.Influence, col.ClosestPoint(col.transform.position), attackDamage, launchAngle);
            }
        }

        #endregion
    }
}
