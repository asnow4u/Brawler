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

        //Events
        public event Action<AnimationClip> AnimationStartedEvent;
        public event Action<AnimationClip> AnimationEndedEvent;


        #region Getters

        public Animator Animator => animator;
        public bool IsAnimationPaused { get; private set; }
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
            try
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
            CreateGraphMixers();
            animationGraph.ResetToIdle(sceneObject.CurGroundedState, sceneObject.CurClimbState);
            }

            catch (Exception e)
            {
                Debug.LogError($"AnimationHandler Setup Exception: {e.Message}\n{e.StackTrace}", sceneObject);
            }
        }


        public override void RegisterToEvents()
        {
            sceneObject.ActionStateHandler.ActionStateChangedEvent += OnActionStateChanged;
            sceneObject.GroundedStateChangedEvent += OnGroundedStateChanged;
            sceneObject.ClimbStateChangedEvent += OnClimbStateChanged;
            sceneObject.MovementInputHandler.InputDataChangedEvent += OnMovementInputChanged;
            sceneObject.MovementInputHandler.CollectionChangedEvent += OnMovementCollectionChanged;
            sceneObject.AttackInputHandler.AttackStateChangedEvent += OnAttackStateChanged;
            sceneObject.AttackInputHandler.CollectionChangedEvent += OnAttackCollectionChanged;
        }

        public override void UnregisterToEvents()
        {
            sceneObject.ActionStateHandler.ActionStateChangedEvent -= OnActionStateChanged;
            sceneObject.GroundedStateChangedEvent -= OnGroundedStateChanged;
            sceneObject.ClimbStateChangedEvent -= OnClimbStateChanged;
            sceneObject.MovementInputHandler.InputDataChangedEvent -= OnMovementInputChanged;
            sceneObject.MovementInputHandler.CollectionChangedEvent -= OnMovementCollectionChanged;
            sceneObject.AttackInputHandler.AttackStateChangedEvent -= OnAttackStateChanged;
            sceneObject.AttackInputHandler.CollectionChangedEvent -= OnAttackCollectionChanged;
        }

        #endregion


        #region AnimationGraph

        /// <summary>
        /// Create animationGraph and set animations
        /// </summary>
        private void CreateGraphMixers()
        {
            animationGraph.SetupIdleMixer(groundIdleAnimation, airIdleAnimation, climbIdleAnimation);
            animationGraph.SetupHitStunMixer(airIdleAnimation);

            MovementInputHandler movementInputHandler = sceneObject.MovementInputHandler;
            if (movementInputHandler != null)
                animationGraph.SetupMovementMixer(movementInputHandler.CurrentMovementCollection);

            AttackInputHandler attackInputHandler = sceneObject.AttackInputHandler;
            if (attackInputHandler != null)
                animationGraph.SetupAttackMixer(attackInputHandler.CurAttackCollection);
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

        /// <summary>
        /// Handle change of climb state
        /// </summary>
        private void OnClimbStateChanged(ClimbState prevClimbState, ClimbState climbState)
        {
            animationGraph.ChangeIdleStateInput(sceneObject.CurGroundedState, climbState);
        }

        /// <summary>
        /// Movement state changed
        /// </summary>
        /// <param name="movementState"></param>
        private void OnMovementInputChanged(MovementInputData inputData)
        {
            if (inputData == null)
            {
                if (sceneObject.ActionStateHandler.CurActionState == ActionState.Moving)
                    sceneObject.ActionStateHandler.ChangeState(ActionState.Idle);
            }

            else
            {
                //NOTE: 
                // If weapon or other enhancements improve animation speed, add to multiplier here

                int index = sceneObject.MovementInputHandler.CurrentMovementCollection.GetIndex(inputData);
                animationGraph.ChangeMovementStateInput(index, inputData.AnimationSpeedMultiplier);
            }
        }

        /// <summary>
        /// Handle change of movmement collection
        /// </summary>
        private void OnMovementCollectionChanged(MovementCollection movementCollection)
        {
            if (movementCollection != null)
                animationGraph?.SetMovementAnimations(movementCollection); //Reason for ?, AnimationGraph can be null during setup, collection will be setup in CreateGraphMixers
        }

        /// <summary>
        /// Attack state changed
        /// </summary>
        /// <param name="attackState"></param>
        private void OnAttackStateChanged(AttackType prevAttackState, AttackType currentAttackState)
        {
            //NOTE: 
            // If weapon or other enhancements improve animation speed, add to multiplier here

            AttackInputHandler attackInputHandler = sceneObject.AttackInputHandler;
            if (attackInputHandler != null && attackInputHandler.CurAttackCollection.TryGetAttackByType(currentAttackState, out AttackData requestedAttackData))
               animationGraph.ChangeAttackStateInput(currentAttackState, requestedAttackData.AnimationSpeedMultiplier);
        }

        /// <summary>
        /// Handle attack collection changing
        /// </summary>
        private void OnAttackCollectionChanged(AttackCollection attackCollection)
        {
            if (attackCollection != null)
                animationGraph?.SetAttackAnimations(attackCollection); //Reason for ?, AnimationGraph can be null during setup, collection will be setup in CreateGraphMixers
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
                        EndAnimation(curPlayingAnimation);                                               

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

        public void PauseAnimation()
        {
            Debug.Log("Pausing Animation");
            animationGraph.Graph.Stop();
            IsAnimationPaused = true;
        }

        public void ResumeAnimation()
        {
            animationGraph.Graph.Play();
            IsAnimationPaused = false;
            Debug.Log("Resuming Animation");
        }

        /// <summary>
        /// End the animation if currently playing and go to Idle
        /// </summary>
        public void EndAnimation(AnimationClip clip)
        {
            if (curPlayingAnimation != null &&
                curPlayingAnimation == clip)
            {
                AnimationEndedEvent?.Invoke(curPlayingAnimation);
            }
        }
    }
}
