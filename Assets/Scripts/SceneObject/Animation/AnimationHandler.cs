using Game.SceneObjects.ActionStates;
using Game.SceneObjects.Attack;
using Game.SceneObjects.Movement;
using System;
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

        [SerializeField] private AnimationClip curPlayingAnimation;

        //Coroutines
        private Coroutine animationEventCorutine;

        //Events
        public event Action<AnimationClip> AnimationStartedEvent;
        public event Action<AnimationClip> AnimationEndedEvent;


        #region Getters

        public Animator Animator => animator;
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
            base.Setup();

            //Animator
            animator = GetComponentInChildren<Animator>();

            if (animator == null)
                throw new NullReferenceException("Animator is null");
            if (groundIdleAnimation == null)
                throw new NullReferenceException("AnimationHandlers Ground Idle Animation is null");
            if (airIdleAnimation == null)
                throw new NullReferenceException("AnimationHandlers Aerial Idle Animation is null");

            animationGraph = animator.gameObject.AddComponent<AnimationGraph>();

            animationGraph.Initialize();
            SetAnimationToGraph();
            animationGraph.ResetToIdle(sceneObject.CurGroundedState);
        }


        public override void RegisterToEvents()
        {
            sceneObject.ActionStateHandler.ActionStateChangedEvent += OnActionStateChanged;
            sceneObject.GroundedStateChangeEvent += OnGroundedStateChanged;
            sceneObject.MovementInputHandler.MoveStateChangedEvent += OnMovementStateChanged;
            sceneObject.AttackInputHandler.AttackStateChangedEvent += OnAttackStateChanged;
        }

        public override void UnregisterToEvents()
        {
            sceneObject.ActionStateHandler.ActionStateChangedEvent -= OnActionStateChanged;
            sceneObject.GroundedStateChangeEvent -= OnGroundedStateChanged;
            sceneObject.MovementInputHandler.MoveStateChangedEvent -= OnMovementStateChanged;
            sceneObject.AttackInputHandler.AttackStateChangedEvent -= OnAttackStateChanged;
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
            //SetHitStunAnimations();
        }


        /// <summary>
        /// Set animationGraphs idle animations
        /// </summary>
        private void SetIdleAnimations()
        {
            animationGraph.SetIdleAnimations(groundIdleAnimation, airIdleAnimation);
        }


        /// <summary>
        /// Set animationGraphs movement animations
        /// </summary>
        private void SetMovementAnimations()
        {
            if (sceneObject.MovementInputHandler.TryGetCurrentMovementCollection(out MovementCollection curMovementCollection))
                animationGraph.SetMovementAnimations(curMovementCollection);
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
            animationGraph.SetHitStunAnimations();
        }

        #endregion


        #region Events

        /// <summary>
        /// Listen to needed events
        /// </summary>
        private void SetUpEventListeners()
        {
        }


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
            animationGraph.ChangeGroundedStateInput(groundedState);
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

                if (sceneObject.MovementInputHandler.TryGetCurrentMovementCollection(out MovementCollection moveCollection))
                {
                    if (moveCollection.TryGetMovementByType(movementState, out MovementData requestedMovementData))
                        animationGraph.ChangeMovementStateInput(movementState, requestedMovementData.AnimationSpeedMultiplier);
                }
            }
        }


        /// <summary>
        /// Attack state changed
        /// </summary>
        /// <param name="attackState"></param>
        private void OnAttackStateChanged(AttackType attackState)
        {
            //NOTE: 
            // If weapon or other enhancements improve animation speed, add to multiplier here

            if (sceneObject.AttackInputHandler.TryGetCurrentAttackCollection(out AttackCollection attackCollection))
            {
                if (attackCollection.TryGetAttackByType(attackState, out AttackData requestedAttackData))
                    animationGraph.ChangeAttackStateInput(attackState, requestedAttackData.AnimationSpeedMultiplier);
            }
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
                        AnimationEndedEvent?.Invoke(curPlayingAnimation);

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



        public void EndAnimation(AnimationClip clip)
        {
            if (curPlayingAnimation != null &&
                curPlayingAnimation == clip)
            {
                GetComponent<ActionStateHandler>().ChangeState(ActionState.Idle);
            }
        }
    }
}
