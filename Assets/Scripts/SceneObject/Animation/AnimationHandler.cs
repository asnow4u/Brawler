using Game.SceneObjects.ActionStates;
using Game.SceneObjects.Attack;
using Game.SceneObjects.Movement;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;


/*NOTES: 
    *   The playable api bypasses the animator controller when playing animations
        Methods like animator.GetCurrentAnimatorClipInfo() will not work while using the playable api
*/

namespace Game.SceneObjects.Animation
{
    public class AnimationHandler : SceneObjectHandler
    {
        private Animator animator;
        private AnimationGraph animationGraph;

        [SerializeField] private AnimationClip groundIdleAnimation;
        [SerializeField] private AnimationClip airIdleAnimation;
        [SerializeField] private AnimationClip climbIdleAnimation;

        [SerializeField] private AnimationClip curPlayingAnimation;

        //Coroutines
        private Coroutine animationPauseCoroutine;

        //Events
        public event Action<AnimationClip> AnimationStartedEvent;
        public event Action<AnimationClip> AnimationEndedEvent;


        #region Getters

        public Animator Animator => animator;
        public bool IsAnimationPaused => animationPauseCoroutine != null;
        public AnimationClip GroundIdleAnimation => groundIdleAnimation;
        public AnimationClip AirIdleAnimation => airIdleAnimation;


        /// <summary>
        /// Get the current frame that the animation is on
        /// </summary>
        /// <returns></returns>
        public int GetFrameOfCurrentAnimation()
        {
            AnimationClipPlayable clipPlayable = animationGraph.GetCurrentAnimationPlayable();

            double wrappedTime = clipPlayable.GetTime() % clipPlayable.GetAnimationClip().length;

            float frameRate = clipPlayable.GetAnimationClip().frameRate;
            int currentFrame = Mathf.FloorToInt((float)wrappedTime * frameRate);

            return currentFrame;
        }

        #endregion


        #region Initialize

        public override void Setup()
        {
            //Animator
            animator = GetComponentInChildren<Animator>();

            if (animator == null)
                throw new NullReferenceException("Animator is null");
            if (groundIdleAnimation == null)
                throw new NullReferenceException("AnimationHandlers Ground Idle Animation is null");
            if (airIdleAnimation == null)
                throw new NullReferenceException("AnimationHandlers Aerial Idle Animation is null");
            if (climbIdleAnimation == null)
                throw new NullReferenceException("AnimationHandlers Climb Idle Animation is null");

            animationGraph = animator.gameObject.AddComponent<AnimationGraph>();

            animationGraph.Initialize();
            SetAnimationToGraph();
            animationGraph.ResetToIdle(sceneObject.CurGroundedState, sceneObject.CurClimbState);
        }


        public override void RegisterToEvents()
        {
            sceneObject.ActionStateHandler.ActionStateChangedEvent += OnActionStateChanged;
            sceneObject.GroundedStateChangedEvent += OnGroundedStateChanged;
            sceneObject.ClimbStateChangedEvent += OnClimbStateChanged;
            sceneObject.MovementInputHandler.MoveStateChangedEvent += OnMovementStateChanged;
            sceneObject.AttackInputHandler.AttackStateChangedEvent += OnAttackStateChanged;
            sceneObject.EquipmentHandler.WeaponHandler.OnWeaponEquipped += OnWeaponEquipped;
        }

        public override void UnregisterToEvents()
        {
            sceneObject.ActionStateHandler.ActionStateChangedEvent -= OnActionStateChanged;
            sceneObject.GroundedStateChangedEvent -= OnGroundedStateChanged;
            sceneObject.ClimbStateChangedEvent -= OnClimbStateChanged;
            sceneObject.MovementInputHandler.MoveStateChangedEvent -= OnMovementStateChanged;
            sceneObject.AttackInputHandler.AttackStateChangedEvent -= OnAttackStateChanged;
            sceneObject.EquipmentHandler.WeaponHandler.OnWeaponEquipped -= OnWeaponEquipped;
        }

        #endregion


        #region AnimationGraph

        /// <summary>
        /// Create animationGraph and set animations
        /// </summary>
        private void SetAnimationToGraph()
        {
            SetIdleAnimations();
            SetMovementAnimations();
            SetAttackAnimations();
            SetHitStunAnimations();
        }


        /// <summary>
        /// Set animationGraphs idle animations
        /// </summary>
        private void SetIdleAnimations()
        {
            animationGraph.SetIdleAnimations(groundIdleAnimation, airIdleAnimation, climbIdleAnimation);
        }


        /// <summary>
        /// Set animationGraphs movement animations
        /// </summary>
        private void SetMovementAnimations()
        {
            if (sceneObject.MovementInputHandler != null)
                animationGraph.SetMovementAnimations(sceneObject.MovementInputHandler.CurrentMovementCollection);
        }


        /// <summary>
        /// Set animationGraphs attack animations
        /// </summary>
        private void SetAttackAnimations()
        {
            if (TryGetComponent(out AttackInputHandler attackInputHandler))
            {
                if (attackInputHandler.TryGetCurrentAttackCollection(out AttackCollection curAttackCollection))
                    animationGraph.SetAttackAnimations(curAttackCollection);
            }
        }


        /// <summary>
        /// Set animationGraphs hitstun animations
        /// </summary>
        private void SetHitStunAnimations()
        {
            animationGraph.SetHitStunAnimations(airIdleAnimation);
        }

        #endregion


        #region Events       

        /// <summary>
        /// Action State Changed
        /// </summary>
        /// <param name="actionState"></param>
        private void OnActionStateChanged(ActionState actionState)
        {
            animationGraph.ChangeActionStateInput(actionState);
        }


        /// <summary>
        /// Grounded State Changed
        /// </summary>
        /// <param name="groundedState"></param>
        private void OnGroundedStateChanged(GroundedState groundedState)
        {
            animationGraph.ChangeIdleStateInput(groundedState, sceneObject.CurClimbState);
        }


        private void OnClimbStateChanged(ClimbState prevClimbState, ClimbState climbState)
        {
            animationGraph.ChangeIdleStateInput(sceneObject.CurGroundedState, climbState);
        }


        /// <summary>
        /// Movement state changed
        /// </summary>
        /// <param name="movementState"></param>
        private void OnMovementStateChanged(MovementType movementState)
        {
            if (movementState == MovementType.Null)
            {
                if (sceneObject.ActionStateHandler.CurActionState == ActionState.Moving)
                    sceneObject.ActionStateHandler.ChangeState(ActionState.Idle);
            }

            else
            {
                //NOTE: 
                // If weapon or other enhancements improve animation speed, add to multiplier here

                if (sceneObject.MovementInputHandler.CurrentMovementCollection.TryGetMovementByType(movementState, out MovementInputData requestedMovementData))
                    animationGraph.ChangeMovementStateInput(movementState, requestedMovementData.AnimationSpeedMultiplier);
            }
        }


        /// <summary>
        /// Attack state changed
        /// </summary>
        /// <param name="attackState"></param>
        private void OnAttackStateChanged(AttackType prevAttackState, AttackType currentAttackState)
        {
            //NOTE: 
            // If weapon or other enhancements improve animation speed, add to multiplier here

            if (sceneObject.AttackInputHandler.TryGetCurrentAttackCollection(out AttackCollection attackCollection))
            {
                if (attackCollection.TryGetAttackByType(currentAttackState, out AttackData requestedAttackData))
                    animationGraph.ChangeAttackStateInput(currentAttackState, requestedAttackData.AnimationSpeedMultiplier);
            }
        }

        /// <summary>
        /// Handle weapon being equipped
        /// </summary>
        private void OnWeaponEquipped(Weapon weapon)
        {            
            animationGraph.SetAttackAnimations(weapon.AttackCollection);
        }

        #endregion


        /// <summary>
        /// Check the current animation playing from graph </br>
        /// Invoke events on changes to the currentPlayingAnimation </br>
        /// Determine when an animation ends   
        /// </summary>
        private void Update()
        {
            //Get animationClip from graph
            AnimationClipPlayable clipPlayable = animationGraph.GetCurrentAnimationPlayable();

            if (clipPlayable.GetAnimationClip() != null)
            {
                //Check if animation changed
                if (curPlayingAnimation != clipPlayable.GetAnimationClip())
                {
                    if (curPlayingAnimation != null)
                    {
                        animationPauseCoroutine = null;
                        AnimationEndedEvent?.Invoke(curPlayingAnimation);
                    }

                    clipPlayable.Play();
                    curPlayingAnimation = clipPlayable.GetAnimationClip();
                    AnimationStartedEvent?.Invoke(curPlayingAnimation);
                }


                //Determine when the clip ends
                if (!curPlayingAnimation.isLooping)
                {
                    if (clipPlayable.GetTime() > curPlayingAnimation.length)
                        EndAnimation(curPlayingAnimation);
                }
            }
        }


        /// <summary>
        /// Pause the current animation for <paramref name="timer"/> seconds
        /// </summary>
        /// <param name="timer"></param>
        public void PauseCurrentAnimation(float timer)
        {
            //Determine a animation is not already paused
            if (curPlayingAnimation != null)
            {
                AnimationClipPlayable clipPlayable = animationGraph.GetCurrentAnimationPlayable();
                clipPlayable.Pause();
         
                animationPauseCoroutine = StartCoroutine(PauseAnimationTimer(timer));
            }
        }

        /// <summary>
        /// Resume a paused animation
        /// </summary>
        public void ResumeCurrentAnimation()
        {
            if (animationPauseCoroutine != null)
            {
                animationPauseCoroutine = null;

                AnimationClipPlayable clipPlayable = animationGraph.GetCurrentAnimationPlayable();
                clipPlayable.Play();
            }
        }


        /// <summary>
        /// Coroutine timer for how long an animation is paused for
        /// </summary>
        private IEnumerator PauseAnimationTimer(float timer)
        {
            while (timer > 0)
            {
                timer -= Time.deltaTime;
                yield return null;
            }

            ResumeCurrentAnimation();
        }


        /// <summary>
        /// End the animation if currently playing and go to Idle
        /// </summary>
        public void EndAnimation(AnimationClip clip)
        {
            if (curPlayingAnimation != null &&
                curPlayingAnimation == clip)
            {
                animationPauseCoroutine = null;
                GetComponent<ActionStateHandler>().ChangeState(ActionState.Idle);
            }
        }
    }
}
