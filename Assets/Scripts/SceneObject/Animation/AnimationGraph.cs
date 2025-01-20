using Game.SceneObjects.ActionStates;
using Game.SceneObjects.Attack;
using Game.SceneObjects.Movement;
using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;


//TODO:
//Want to remove monobehaviour from this class

namespace Game.SceneObjects.Animation 
{
    public class AnimationGraph : MonoBehaviour
    {
        private Animator animator;

        //Playables
        private PlayableGraph animationGraph;

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
            animationGraph = PlayableGraph.Create("AnimationGraph");
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(animationGraph, "Animation", animator);

            //State Mixer
            stateAnimationMixer = AnimationMixerPlayable.Create(animationGraph, Enum.GetValues(typeof(ActionState)).Length);
            output.SetSourcePlayable(stateAnimationMixer);

            SetupMixers();

            animationGraph.Play();
        }


        #region Setup Mixers

        private void SetupMixers()
        {
            SetupIdleMixer();
            SetupMovementMixer();
            SetupAttackMixer();
            SetupHitStunMixer();
        }


        /// <summary>
        /// Setup idle mixer for idle animations
        /// Inputs: Grounded, Air
        /// </summary>
        private void SetupIdleMixer()
        {
            idleAnimationMixer = AnimationMixerPlayable.Create(animationGraph, 2);
            stateAnimationMixer.ConnectInput((int)ActionState.Idle, idleAnimationMixer, 0);
        }


        /// <summary>
        /// Setup movement mixer for move animations
        /// Inputs: (walk, run), jump, airJump, land
        /// </summary>
        /// <param name="moveCollection"></param>
        private void SetupMovementMixer()
        {
            movementAnimationMixer = AnimationMixerPlayable.Create(animationGraph, Enum.GetValues(typeof(MovementType)).Length);
            stateAnimationMixer.ConnectInput((int)ActionState.Moving, movementAnimationMixer, 0);
        }


        /// <summary>
        /// Setup attack mixer for attack animations
        /// Inputs: forwardTilt, upTilt, downTilt, forwardAir, upAir, downAir
        /// </summary>
        /// <param name="attackCollection"></param>
        private void SetupAttackMixer()
        {
            attackAnimationMixer = AnimationMixerPlayable.Create(animationGraph, Enum.GetValues(typeof(AttackType)).Length);
            stateAnimationMixer.ConnectInput((int)ActionState.Attacking, attackAnimationMixer, 0);
        }


        /// <summary>
        /// Setup hit mixer for hit animations
        /// Input: 
        /// </summary>
        private void SetupHitStunMixer()
        {
            hitAnimationMixer = AnimationMixerPlayable.Create(animationGraph, 1);
            stateAnimationMixer.ConnectInput((int)ActionState.HitStun, hitAnimationMixer, 0);
        }

        #endregion


        #region Mixer Animations

        /// <summary>
        /// Set idle animations to use
        /// </summary>
        /// <param name="groundIdleAnimation"></param>
        /// <param name="airIdleAnimation"></param>
        public void SetIdleAnimations(AnimationClip groundIdleAnimation, AnimationClip airIdleAnimation)
        {
            AnimationClipPlayable groundIdle = AnimationClipPlayable.Create(animationGraph, groundIdleAnimation);
            AnimationClipPlayable airIdle = AnimationClipPlayable.Create(animationGraph, airIdleAnimation);

            idleAnimationMixer.ConnectInput(0, groundIdle, 0);
            idleAnimationMixer.ConnectInput(1, airIdle, 0);
        }


        /// <summary>
        /// Set movement animations to use
        /// </summary>
        /// <param name="moveCollection"></param>
        public void SetMovementAnimations(MovementCollection moveCollection)
        {
            foreach (MovementType moveType in Enum.GetValues(typeof(MovementType)))
            {
                if (moveCollection.TryGetMovementByType(moveType, out MovementData movementData))
                {
                    //if (movementData is MoveData moveData)
                    //{
                    //    //TODO: use to build move mixer
                    //}

                    AnimationClipPlayable movePlayable = AnimationClipPlayable.Create(animationGraph, movementData.Animation);
                    movementAnimationMixer.ConnectInput((int)moveType, movePlayable, 0);
                }
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
                    AnimationClipPlayable attackPlayable = AnimationClipPlayable.Create(animationGraph, attackData.Animation);
                    attackAnimationMixer.ConnectInput((int)attackType, attackPlayable, 0);
                }
            }
        }


        /// <summary>
        /// Set hitstun animations to use
        /// </summary>
        public void SetHitStunAnimations(AnimationClip hitStunAnimation)
        {
            AnimationClipPlayable hitstunPlayable = AnimationClipPlayable.Create(animationGraph, hitStunAnimation);
            hitAnimationMixer.ConnectInput(0, hitstunPlayable, 0);

            hitAnimationMixer.SetInputWeight(0, 1);
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
        public void ResetToIdle(GroundedState groundState)
        {
            ResetInputWeights(stateAnimationMixer);
            ResetInputWeights(idleAnimationMixer);
            ResetInputWeights(movementAnimationMixer);
            ResetInputWeights(attackAnimationMixer);
            //ResetInputWeights(hitAnimationMixer);

            stateAnimationMixer.SetInputWeight(0, 1);

            if (groundState == GroundedState.Airborn)
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
        public void ChangeGroundedStateInput(GroundedState groundedState)
        {
            ResetInputWeights(idleAnimationMixer);

            if (groundedState == GroundedState.Airborn)
                idleAnimationMixer.SetInputWeight(1, 1);
            else
                idleAnimationMixer.SetInputWeight(0, 1);
        }


        /// <summary>
        /// Change movement mixer to prioritize current move state
        /// </summary>
        /// <param name="moveState"></param>
        public void ChangeMovementStateInput(MovementType moveState, float speedMultiplier)
        {
            //reset animation clip
            AnimationClipPlayable clipPlayable = (AnimationClipPlayable)movementAnimationMixer.GetInput((int)moveState);
            clipPlayable.SetTime(0);
            PlayableExtensions.SetSpeed(clipPlayable, speedMultiplier);            

            ResetInputWeights(movementAnimationMixer);
            movementAnimationMixer.SetInputWeight((int)moveState, 1);
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
            animationGraph.Destroy();
        }
    }
}
