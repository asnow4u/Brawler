using Game.SceneObjects.ActionStates;
using Game.SceneObjects.Attack;
using Game.SceneObjects.Movement;
using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using static UnityEditor.Rendering.CameraUI;


//TODO:
//Want to remove monobehaviour from this class

namespace Game.SceneObjects.Animation 
{
    public class AnimationGraph : MonoBehaviour
    {
        private Animator animator;

        //Playables
        internal PlayableGraph Graph;

        private AnimationMixerPlayable stateAnimationMixer;
        private AnimationMixerPlayable idleAnimationMixer;
        private AnimationMixerPlayable movementAnimationMixer;
        private AnimationMixerPlayable attackAnimationMixer;
        private AnimationMixerPlayable hitAnimationMixer;


        #region Getters

        /// <summary>
        /// Get the current animationClipPlayable that is currently playing
        /// </summary>
        /// <returns></returns>
        public AnimationClipPlayable GetCurrentAnimationPlayable()
        {
            AnimationClipPlayable playable = new AnimationClipPlayable();

            for (int i = 0; i < stateAnimationMixer.GetInputCount(); i++)
            {
                if (stateAnimationMixer.GetInputWeight(i) > 0)
                {
                    AnimationMixerPlayable mixer = (AnimationMixerPlayable)stateAnimationMixer.GetInput(i);                    

                    for (int j = 0; j < mixer.GetInputCount(); j++)
                    {
                        if (mixer.GetInputWeight(j) > 0)
                            playable = (AnimationClipPlayable)mixer.GetInput(j);
                    }
                }
            }

            return playable;
        }

        #endregion


        public void Initialize()
        {
            this.animator = GetComponent<Animator>();

            //Graph
            Graph = PlayableGraph.Create("AnimationGraph");
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(Graph, "Animation", animator);            

            //State Mixer
            stateAnimationMixer = AnimationMixerPlayable.Create(Graph, Enum.GetValues(typeof(ActionState)).Length);
            output.SetSourcePlayable(stateAnimationMixer);

            animator.Rebind();
            animator.Update(0);

            Graph.Play();
        }

        //public void SetupAnimationMixers(MovementCollection movementCollection, AttackCollection attackCollection)
        //{
        //    SetupIdleMixer();
        //    SetupHitStunMixer();

        //    //Setup movmement mixer if movement is available
        //    if (movementCollection != null)
        //        SetupMovementMixer(movementCollection);

        //    //Setup movmement mixer if attacking is available
        //    if (attackCollection != null)
        //        SetupAttackMixer(attackCollection);

            
        //}

        #region Setup Mixers

        /// <summary>
        /// Setup idle mixer for idle animations
        /// Inputs: Grounded, Air
        /// </summary>
        public void SetupIdleMixer(AnimationClip groundIdleAnimation, AnimationClip airIdleAnimation, AnimationClip climbIdleAnimation)
        {
            idleAnimationMixer = AnimationMixerPlayable.Create(Graph, 3);
            stateAnimationMixer.ConnectInput((int)ActionState.Idle, idleAnimationMixer, 0);

            SetIdleAnimations(groundIdleAnimation, airIdleAnimation, climbIdleAnimation);
        }

        /// <summary>
        /// Setup hit mixer for hit animations
        /// Input: 
        /// </summary>
        public void SetupHitStunMixer(AnimationClip hitStunAnimation)
        {
            hitAnimationMixer = AnimationMixerPlayable.Create(Graph, 1);
            stateAnimationMixer.ConnectInput((int)ActionState.HitStun, hitAnimationMixer, 0);

            SetHitStunAnimations(hitStunAnimation);
        }

        /// <summary>
        /// Setup movement mixer for move animations
        /// Inputs: (walk, run), jump, airJump, land
        /// </summary>
        public void SetupMovementMixer(MovementCollection movementCollection)
        {
            movementAnimationMixer = AnimationMixerPlayable.Create(Graph, movementCollection.MovementData.Count);
            stateAnimationMixer.ConnectInput((int)ActionState.Moving, movementAnimationMixer, 0);

            SetMovementAnimations(movementCollection);
        }


        /// <summary>
        /// Setup attack mixer for attack animations
        /// Inputs: forwardTilt, upTilt, downTilt, forwardAir, upAir, downAir
        /// </summary>
        public void SetupAttackMixer(AttackCollection attackCollection)
        {
            attackAnimationMixer = AnimationMixerPlayable.Create(Graph, Enum.GetValues(typeof(AttackType)).Length);
            stateAnimationMixer.ConnectInput((int)ActionState.Attacking, attackAnimationMixer, 0);

            SetAttackAnimations(attackCollection);
        }

        #endregion


        #region Mixer Animations

        /// <summary>
        /// Set idle animations to use
        /// </summary>
        public void SetIdleAnimations(AnimationClip groundIdleAnimation, AnimationClip airIdleAnimation, AnimationClip climbIdleAnimation)
        {
            //Grounded
            AnimationClipPlayable groundIdle = AnimationClipPlayable.Create(Graph, groundIdleAnimation);
            idleAnimationMixer.ConnectInput(0, groundIdle, 0);
            
            //Areial
            AnimationClipPlayable airIdle = AnimationClipPlayable.Create(Graph, airIdleAnimation);
            idleAnimationMixer.ConnectInput(1, airIdle, 0);

            //Climb
            AnimationClipPlayable climbIdle = AnimationClipPlayable.Create(Graph, climbIdleAnimation);
            idleAnimationMixer.ConnectInput(2, climbIdle, 0);
        }

        /// <summary>
        /// Set hitstun animations to use
        /// </summary>
        public void SetHitStunAnimations(AnimationClip hitStunAnimation)
        {
            AnimationClipPlayable hitstunPlayable = AnimationClipPlayable.Create(Graph, hitStunAnimation);
            hitAnimationMixer.ConnectInput(0, hitstunPlayable, 0);

            hitAnimationMixer.SetInputWeight(0, 1);
        }


        /// <summary>
        /// Set movement animations to use
        /// </summary>
        public void SetMovementAnimations(MovementCollection moveCollection)
        {
            //Get active input index
            int activeInputIndex = -1;
            for (int i= 0; i < movementAnimationMixer.GetInputCount(); i++)
            {
                if (movementAnimationMixer.GetInputWeight(i) > 0)
                {
                    activeInputIndex = i;
                    break;
                }
            }

            //Disconnect existing inputs
            for (int i = 0; i < movementAnimationMixer.GetInputCount(); i++)
                movementAnimationMixer.DisconnectInput(i);

            //Connect inputs from new collection
            for (int i=0; i < moveCollection.MovementData.Count; i++)
            {
                MovementInputData inputData = moveCollection.MovementData[i];

                if (inputData == null || inputData.Animation == null) continue;
                AnimationClipPlayable movePlayable = AnimationClipPlayable.Create(Graph, inputData.Animation);
                movementAnimationMixer.ConnectInput(i, movePlayable, 0);
            }

            //Set active input
            if (activeInputIndex > -1)
            {
                ResetInputWeights(movementAnimationMixer);
                movementAnimationMixer.SetInputWeight(activeInputIndex, 1);
            }
        }


        /// <summary>
        /// Set attack animations to use
        /// </summary>
        /// <param name="attackCollection"></param>
        public void SetAttackAnimations(AttackCollection attackCollection)
        {
            foreach (AttackType attackType in Enum.GetValues(typeof(AttackType)))
            {
                if (attackCollection.TryGetAttackByType(attackType, out AttackData attackData))
                {
                    attackAnimationMixer.DisconnectInput((int)attackType);
                    
                    AnimationClipPlayable attackPlayable = AnimationClipPlayable.Create(Graph, attackData.Animation);
                    attackAnimationMixer.ConnectInput((int)attackType, attackPlayable, 0);
                }
            }
        }

        #endregion


        #region Input Changes

        /// <summary>
        /// Reset all inputs for given mixer
        /// </summary>
        /// <param name="mixer"></param>
        private void ResetInputWeights(AnimationMixerPlayable mixer)
        {
            for (int i = 0; i < mixer.GetInputCount(); i++)
            {
                mixer.SetInputWeight(i, 0);
            }
        }


        /// <summary>
        /// Reset all mixers and set weights for idle
        /// </summary>
        public void ResetToIdle(GroundedState groundState, ClimbState climbState)
        {
            ResetInputWeights(stateAnimationMixer);
            ResetInputWeights(idleAnimationMixer);
            ResetInputWeights(movementAnimationMixer);
            ResetInputWeights(attackAnimationMixer);
            //ResetInputWeights(hitAnimationMixer);

            stateAnimationMixer.SetInputWeight(0, 1);

            if (climbState == ClimbState.Climbing)
                idleAnimationMixer.SetInputWeight(2, 1);            
            else if (groundState == GroundedState.Airborn)
                idleAnimationMixer.SetInputWeight(1, 1);
            else
                idleAnimationMixer.SetInputWeight(0, 1);
        }


        /// <summary>
        /// Change stateMixer to prioritize current actionState
        /// </summary>
        /// <param name="actionState"></param>
        public void ChangeActionStateInput(ActionState actionState)
        {
            ResetInputWeights(stateAnimationMixer);
            stateAnimationMixer.SetInputWeight((int)actionState, 1);
        }


        /// <summary>
        /// Change any mixers affected by grounded state to prioritize current grounded state
        /// </summary>
        /// <param name="groundedState"></param>
        public void ChangeIdleStateInput(GroundedState groundedState, ClimbState climbState)
        {
            ResetInputWeights(idleAnimationMixer);

            if (climbState == ClimbState.Climbing)
                idleAnimationMixer.SetInputWeight(2, 1);            
            else if (groundedState == GroundedState.Airborn)
                idleAnimationMixer.SetInputWeight(1, 1);
            else
                idleAnimationMixer.SetInputWeight(0, 1);
        }


        /// <summary>
        /// Change movement mixer to prioritize current move state
        /// </summary>
        /// <param name="moveState"></param>
        public void ChangeMovementStateInput(int index, float animationSpeed)
        {
            //reset animation clip
            AnimationClipPlayable clipPlayable = (AnimationClipPlayable)movementAnimationMixer.GetInput(index);
            clipPlayable.SetTime(0);
            PlayableExtensions.SetSpeed(clipPlayable, animationSpeed);            

            ResetInputWeights(movementAnimationMixer);
            movementAnimationMixer.SetInputWeight(index, 1);
        }


        /// <summary>
        /// Change attack mixer to prioritize current attack state
        /// </summary>
        /// <param name="attackState"></param>
        public void ChangeAttackStateInput(AttackType attackState, float speedMultiplier)
        {
            if (attackState != AttackType.Null)
            {
                //reset animation clip
                AnimationClipPlayable clipPlayable = (AnimationClipPlayable)attackAnimationMixer.GetInput((int)attackState);
                clipPlayable.SetTime(0);
                PlayableExtensions.SetSpeed(clipPlayable, speedMultiplier);

                ResetInputWeights(attackAnimationMixer);
                attackAnimationMixer.SetInputWeight((int)attackState, 1);
            }
        }

        #endregion



        //TODO: Implement blending from different animations over a period of time
        //private IEnumerator AnimationClipBlending(AnimationMixerPlayable mixer, int firstInput, int secondInput, Func<float> GetBlendWeight)
        //{
        //    while (true)
        //    {
        //        float blendedWeight = Mathf.Clamp01(GetBlendWeight());

        //        mixer.SetInputWeight(firstInput, 1.0f - blendedWeight);
        //        mixer.SetInputWeight(secondInput, blendedWeight);

        //        yield return null;
        //    }
        //}



        private void OnDestroy()
        {
            Graph.Destroy();
        }
    }
}
