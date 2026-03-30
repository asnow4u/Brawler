using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

internal class AnimationGraph : IDisposable
{
    //Playables
    private PlayableGraph Graph;

    private AnimationMixerPlayable stateAnimationMixer;
    private AnimationMixerPlayable idleAnimationMixer;
    private AnimationMixerPlayable movementAnimationMixer;
    private AnimationMixerPlayable attackAnimationMixer;
    private AnimationMixerPlayable hitAnimationMixer;

    public AnimationGraph(Animator animator)
    {
        //Graph
        Graph = PlayableGraph.Create("AnimationGraph");
        AnimationPlayableOutput output = AnimationPlayableOutput.Create(Graph, "Animation", animator);

        //State Mixer
        stateAnimationMixer = AnimationMixerPlayable.Create(Graph, Enum.GetValues(typeof(ActionState)).Length - 1);
        output.SetSourcePlayable(stateAnimationMixer);

        //Idle Mixer
        idleAnimationMixer = AnimationMixerPlayable.Create(Graph, Enum.GetValues(typeof(IdleState)).Length - 1); //Subtract 1 to account for null state
        stateAnimationMixer.ConnectInput((int)ActionState.Idle - 1, idleAnimationMixer, 0);

        //Movement Mixer
        movementAnimationMixer = AnimationMixerPlayable.Create(Graph, Enum.GetValues(typeof(MovementState)).Length - 1); //Subtract 1 to account for null state
        stateAnimationMixer.ConnectInput((int)ActionState.Moving - 1, movementAnimationMixer, 0);

        //Attack Mixer
        attackAnimationMixer = AnimationMixerPlayable.Create(Graph, Enum.GetValues(typeof(AttackState)).Length - 1); //Subtract 1 to account for null state 
        stateAnimationMixer.ConnectInput((int)ActionState.Attacking - 1, attackAnimationMixer, 0);

        //Hitstun Mixer
        hitAnimationMixer = AnimationMixerPlayable.Create(Graph, 1);
        stateAnimationMixer.ConnectInput((int)ActionState.HitStun - 1, hitAnimationMixer, 0);

        animator.Rebind();
        animator.Update(0);

        Graph.Play();
    }


    #region Getters

    /// <summary>
    /// Get the current animationClipPlayable that is currently playing
    /// </summary>
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
    public void SetMovementAnimations(AnimationClip[] movementAnimations)
    {
        if (movementAnimations == null) return;

        //Account for switching animations mid movement by keeping the active input
        int activeInputIndex = -1;
        for (int i= 0; i < movementAnimationMixer.GetInputCount(); i++)
        {
            if (movementAnimationMixer.GetInputWeight(i) > 0)
            {
                activeInputIndex = i;
                break;
            }
        }
       
        for (int i = 0; i < movementAnimations.Length; i++)
        {
            movementAnimationMixer.DisconnectInput(i);

            if (movementAnimations[i] == null) continue;
            AnimationClipPlayable movePlayable = AnimationClipPlayable.Create(Graph, movementAnimations[i]);
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
    public void SetAttackAnimations(AnimationClip[] attackAnimations)
    {
        if (attackAnimations == null) return;

        for (int i = 0; i < attackAnimations.Length; i++)
        {
            attackAnimationMixer.DisconnectInput(i);

            if (attackAnimations[i] == null) continue;
            AnimationClipPlayable attackPlayable = AnimationClipPlayable.Create(Graph, attackAnimations[i]);
            attackAnimationMixer.ConnectInput(i, attackPlayable, 0);
        }
    }

    #endregion


    #region Input Changes

    /// <summary>
    /// Reset all inputs for given mixer
    /// </summary>
    private void ResetInputWeights(AnimationMixerPlayable mixer)
    {
        for (int i = 0; i < mixer.GetInputCount(); i++)
        {
            mixer.SetInputWeight(i, 0);
        }
    }

    /// <summary>
    /// Change stateMixer to prioritize current actionState
    /// </summary>
    public void ChangeActionStateInput(ActionState actionState)
    {
        ResetInputWeights(stateAnimationMixer);
        stateAnimationMixer.SetInputWeight((int)actionState - 1, 1); //Subtract 1 to account for null state
    }

    /// <summary>
    /// Change any mixers affected by grounded state to prioritize current grounded state
    /// </summary>
    public void ChangeIdleStateInput(IdleState idleState)
    {
        ResetInputWeights(idleAnimationMixer);
        idleAnimationMixer.SetInputWeight((int)idleState - 1, 1); //Subtract 1 to account for null state
    }

    /// <summary>
    /// Change movement mixer to prioritize current move state
    /// </summary>
    public void ChangeMovementStateInput(MovementState moveState)
    {
        ResetInputWeights(movementAnimationMixer);

        //Reset animation clip
        AnimationClipPlayable clipPlayable = (AnimationClipPlayable)attackAnimationMixer.GetInput((int)moveState - 1); //Subtract 1 to account for null state
        if (!clipPlayable.IsNull())
            clipPlayable.SetTime(0);

        movementAnimationMixer.SetInputWeight((int)moveState - 1, 1); //Subtract 1 to account for null state
    }

    /// <summary>
    /// Change attack mixer to prioritize current attack state
    /// </summary>
    public void ChangeAttackStateInput(AttackState attackState)
    {        
        ResetInputWeights(attackAnimationMixer);

        //Reset animation clip
        AnimationClipPlayable clipPlayable = (AnimationClipPlayable)attackAnimationMixer.GetInput((int)attackState - 1); //Subtract 1 to account for null state
        if (!clipPlayable.IsNull())
            clipPlayable.SetTime(0);

        attackAnimationMixer.SetInputWeight((int)attackState - 1, 1); //Subtract 1 to account for null state
    }

    #endregion

    public void Dispose()
    {
        Graph.Destroy();
    }
}
