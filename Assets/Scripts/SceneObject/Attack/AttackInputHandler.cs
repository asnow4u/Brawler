using Game.SceneObjects.ActionStates;
using Game.SceneObjects.Movement;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Game.SceneObjects.Attack
{
    public enum AttackType { Null, UpTilt, DownTilt, ForwardTilt, UpAir, DownAir, ForwardAir };

    public class AttackInputHandler : SceneObjectHandler
    {
        const ActionState ATTACKSTATE = ActionState.Attacking;

        private IAttackInput attackInput;
        private MovementHandler movementHandler => sceneObject.MovementHandler;

        [Header("State")]
        //Attack Data
        //NOTE: This tracks what attack is currently happening. This prevents multiple attacks from overwriting one another before an attack animation starts
        [SerializeField] private AttackType curAttackState;
        //NOTE: THis tracks what attack happend before the current attack.
        [SerializeField] private AttackType previousAttackState;

        //NOTE: This tracks the current attack data being used
        private AttackData curAttackData;
        private AttackCollection curAttackCollection;
        private AttackHandler curAttackHandler;

        //NOTE: This is the max amount of time that this attack can be held before it is released
        private const float MAX_ATTACK_CHARGE_TIME = 1f;
        //NOTE: This is the max amount of additional damage that can be added to the attack (30%)
        private const float Max_CHARGE_ATTACK_MULTIPLIER = 1f;
        private float chargeAttackMultiplier = 0;
        private CancellationTokenSource chargeAttackCTS;

        private HashSet<string> objectHitByAttack = new HashSet<string>();


        /// <summary>
        /// Event that is fired when an attack is performed. <br/>
        /// The first attackType defines the current attack state <br/>
        /// The second attackType defines the attack to transition to <br/>
        /// </summary>
        public event Action<AttackType, AttackType> AttackStateChangedEvent;
        public event Action<AttackCollection> CollectionChangedEvent;


        #region Getters

        public AttackData CurAttackData => curAttackData;
        public AttackCollection CurAttackCollection => curAttackCollection;

        #endregion


        #region Initialize    

        public override void RegisterToEvents()
        {
            sceneObject.GroundedStateChangedEvent += OnGroundedStateChanged;
            sceneObject.EquipmentHandler.WeaponHandler.OnWeaponEquippedEvent += OnWeaponEquipped;
            sceneObject.AnimationHandler.AnimationStartedEvent += OnAnimationStarted;
            sceneObject.AnimationHandler.AnimationEndedEvent += OnAnimationEnded;
        }

        public override void UnregisterToEvents()
        {
            sceneObject.GroundedStateChangedEvent -= OnGroundedStateChanged;
            sceneObject.EquipmentHandler.WeaponHandler.OnWeaponEquippedEvent -= OnWeaponEquipped;
            sceneObject.AnimationHandler.AnimationStartedEvent -= OnAnimationStarted;
            sceneObject.AnimationHandler.AnimationEndedEvent -= OnAnimationEnded;
        }

        public override void Setup()
        {
            if (sceneObject is IAttackInput input)
                this.attackInput = input;
            else
                throw new Exception($"SceneObject {sceneObject.name} does not implement AttackInput interface");
        }

        #endregion

        #region Events

        /// <summary>
        /// Handle grounded state changes while performing an aerial attack
        /// </summary>
        private void OnGroundedStateChanged(GroundedState groundedState)
        {
            if (groundedState == GroundedState.Grounded && curAttackState != AttackType.Null)
            {
                switch (curAttackState)
                {
                    case AttackType.UpAir:
                        if (attackInput.IsUpAttackActive())
                            SetCurrentAttackState(AttackType.UpTilt);
                        else
                            SetCurrentAttackState(AttackType.Null);
                        break;

                    case AttackType.ForwardAir:
                        if (attackInput.IsRightAttackActive() || attackInput.IsLeftAttackActive())
                            SetCurrentAttackState(AttackType.ForwardTilt);
                        else
                            SetCurrentAttackState(AttackType.Null);
                        break;

                    case AttackType.DownAir:
                        if (attackInput.IsDownAttackActive())
                            SetCurrentAttackState(AttackType.DownTilt);
                        else
                            SetCurrentAttackState(AttackType.Null);
                        break;
                }
            }
        }

        /// <summary>
        /// Handle weapon equipping
        /// </summary>
        private void OnWeaponEquipped(Weapon previousWeapon, Weapon weapon)
        {
            if (weapon != null)
            {
                curAttackCollection = weapon.AttackCollection;
                CollectionChangedEvent?.Invoke(curAttackCollection);
            }
        }

        /// <summary>
        /// Check for attack animation
        /// </summary>
        private void OnAnimationStarted(AnimationClip clip)
        {
            if (curAttackCollection != null && curAttackCollection.TryGetAttackByAnimation(clip, out AttackData attackData))
            {
                curAttackData = attackData;
                curAttackData.ResetAttackTriggers();

                curAttackHandler = new AttackHandler(
                    curAttackData,
                    sceneObject.EquipmentHandler.WeaponHandler.EquippedWeapon,
                    sceneObject.AnimationHandler.GetFrameOfCurrentAnimation,
                    PerformChargeAttack);
            }
        }

        /// <summary>
        /// Check if attack animation ended
        /// </summary>
        private void OnAnimationEnded(AnimationClip clip)
        {
            if (curAttackCollection != null && curAttackCollection.TryGetAttackByAnimation(clip, out AttackData attackData))
            {
                //Handle the case where the current attack animation is ended (actionState change)
                if (curAttackState == attackData.Type)
                {
                    SetCurrentAttackState(AttackType.Null);
                    chargeAttackMultiplier = 0;
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
                if (curAttackHandler != null)
                    curAttackHandler.CheckForAnimationTriggers();

                //Charge Attack
                UpdateChargeAttack();                
            }
        }


        #region Attack State

        /// <summary>
        /// Attempt to set the current attack state <br></br>
        /// This will initiate the animation of the attackType
        /// </summary>
        private void SetCurrentAttackState(AttackType attackType)
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
                curAttackHandler = null;

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
            if (curAttackState == AttackType.Null)
            {
                if (sceneObject.CurGroundedState == GroundedState.Grounded)
                    SetCurrentAttackState(AttackType.UpTilt);                            

                else
                    SetCurrentAttackState(AttackType.UpAir);
            }
        }

        /// <summary>
        /// Try to perform a grounded / Air Down attack
        /// </summary>
        public void PerformDownAttack()
        {
            if (curAttackState == AttackType.Null)
            {
                if (sceneObject.CurGroundedState == GroundedState.Grounded)
                    SetCurrentAttackState(AttackType.DownTilt);

                else
                    SetCurrentAttackState(AttackType.DownAir);
            }
        }

        /// <summary>
        /// Try to perform a grounded / Air Forward attack <\br>
        /// Turn around if facing the wrong direction
        /// </summary>
        public void PerformForwardAttack()
        {
            if (curAttackState == AttackType.Null)
            {                
                if (sceneObject.CurGroundedState == GroundedState.Grounded)
                {
                    //NOTE: Resets y velocity. This helps prevent an areal grounded attack if performed on first few frame of jump
                    sceneObject.Rb.linearVelocity = new Vector3(sceneObject.Rb.linearVelocity.x, 0, 0);

                    SetCurrentAttackState(AttackType.ForwardTilt);

                    if (!movementHandler.IsFacingRightDirection)
                        movementHandler.TurnAround();                   
                }
                    
                else
                {
                    SetCurrentAttackState(AttackType.ForwardAir);

                    if (!movementHandler.IsFacingRightDirection)
                        movementHandler.TurnAround();
                }
            }
        }

        /// <summary>
        /// Try to perform a grounded / Air Forward attack <\br>
        /// Turn around if facing the wrong direction
        /// </summary>
        public void PerformLeftAttack()
        {
            if (curAttackState == AttackType.Null)
            {
                if (sceneObject.CurGroundedState == GroundedState.Grounded)
                {
                    SetCurrentAttackState(AttackType.ForwardTilt);

                    if (movementHandler.IsFacingRightDirection)
                        movementHandler.TurnAround();
                }

                else
                {
                    SetCurrentAttackState(AttackType.ForwardAir);

                    if (movementHandler.IsFacingRightDirection)
                        movementHandler.TurnAround();
                }                
            }
        }

        #endregion

        #region Charged Attack

        private void PerformChargeAttack()
        {
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
        }

        /// <summary>
        /// Determine if a charged attack needs to be released
        /// </summary>
        private void UpdateChargeAttack()
        {
            //TODO: Should check if that animation is an attack
            if (sceneObject.AnimationHandler.IsAnimationPaused)
            {
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
            if (attackInput.IsUpAttackActive())
                StartChargeAttack();
        }

        /// <summary>
        /// Release charged upward attack
        /// </summary>
        private void ReleaseUpChargeAttack()
        {
            if (!attackInput.IsUpAttackActive())
                ExecuteChargeAttack();
        }

        /// <summary>
        /// Perform a charged forward attack. <br/>
        /// Pauses animation till release. <br/>
        /// Fired from animationTrigger
        /// </summary>
        private void PerformForwardChangeAttack()
        {
            if (attackInput.IsRightAttackActive() && movementHandler.IsFacingRightDirection)
                StartChargeAttack();

            else if (attackInput.IsLeftAttackActive() && !movementHandler.IsFacingRightDirection)
                StartChargeAttack();
        }

        /// <summary>
        /// Release charged forward attack
        /// </summary>
        private void ReleaseForwardChargeAttack()
        {
            if (!attackInput.IsRightAttackActive() && movementHandler.IsFacingRightDirection)
                ExecuteChargeAttack();

            else if (!attackInput.IsLeftAttackActive() && !movementHandler.IsFacingRightDirection)
                ExecuteChargeAttack();
        }

        /// <summary>
        /// Perform a charged downward attack. <br/>
        /// Pauses animation till release. <br/>
        /// Fired from animationTrigger
        /// </summary>
        private void PerformDownChargeAttack()
        {
            if (attackInput.IsDownAttackActive())
                StartChargeAttack();
        }

        /// <summary>
        /// Release charged downward attack
        /// </summary>
        private void ReleaseDownChargeAttack()
        {
            if (!attackInput.IsDownAttackActive())
                ExecuteChargeAttack();
        }

        private void StartChargeAttack()
        {
            Debug.Log("Start Charge Attack");
            chargeAttackCTS?.Cancel();
            chargeAttackCTS?.Dispose();
            chargeAttackCTS = new CancellationTokenSource();
            _ = ChargeAttackTimer(chargeAttackCTS.Token);
        }

        private void ExecuteChargeAttack()
        {
            chargeAttackCTS?.Cancel();
            chargeAttackCTS?.Dispose();
            chargeAttackCTS = null;
        }

        private async Task ChargeAttackTimer(CancellationToken token)
        {
            float startTime = Time.realtimeSinceStartup;
            sceneObject.AnimationHandler.PauseAnimation();

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(MAX_ATTACK_CHARGE_TIME), token);
                Debug.Log("Charge Attack Maxed Out");
            }

            catch (TaskCanceledException)
            { 
                Debug.Log("Charge Attack Released Early");
            }

            finally
            {
                float elapsed = Time.realtimeSinceStartup - startTime;
                float attackChargeTime = Mathf.Min(elapsed, MAX_ATTACK_CHARGE_TIME);
                chargeAttackMultiplier = Mathf.Lerp(0, Max_CHARGE_ATTACK_MULTIPLIER, attackChargeTime / MAX_ATTACK_CHARGE_TIME);

                sceneObject.AnimationHandler.ResumeAnimation();
            }
        }

        #endregion
    }
}
