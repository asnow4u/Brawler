using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

internal class AnimationGraph : IDisposable
{
    //Playables
    private PlayableGraph graph;

    private AnimationMixerPlayable stateAnimationMixer;
    private AnimationMixerPlayable idleAnimationMixer;
    private AnimationMixerPlayable movementAnimationMixer;
    private AnimationMixerPlayable attackAnimationMixer;
    private AnimationMixerPlayable hitAnimationMixer;

    public AnimationGraph(Animator animator)
    {
        //Graph
        graph = PlayableGraph.Create("AnimationGraph");        
        AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "Animation", animator);

        //State Mixer
        stateAnimationMixer = AnimationMixerPlayable.Create(graph, Enum.GetValues(typeof(ActionState)).Length - 1);
        output.SetSourcePlayable(stateAnimationMixer);

        //Idle Mixer
        idleAnimationMixer = AnimationMixerPlayable.Create(graph, Enum.GetValues(typeof(IdleState)).Length - 1); //Subtract 1 to account for null state
        stateAnimationMixer.ConnectInput((int)ActionState.Idle - 1, idleAnimationMixer, 0);

        //Movement Mixer
        movementAnimationMixer = AnimationMixerPlayable.Create(graph, Enum.GetValues(typeof(MovementState)).Length - 1); //Subtract 1 to account for null state
        stateAnimationMixer.ConnectInput((int)ActionState.Moving - 1, movementAnimationMixer, 0);

        //Attack Mixer
        attackAnimationMixer = AnimationMixerPlayable.Create(graph, Enum.GetValues(typeof(AttackState)).Length - 1); //Subtract 1 to account for null state 
        stateAnimationMixer.ConnectInput((int)ActionState.Attacking - 1, attackAnimationMixer, 0);

        //Hitstun Mixer
        hitAnimationMixer = AnimationMixerPlayable.Create(graph, 1);
        stateAnimationMixer.ConnectInput((int)ActionState.HitStun - 1, hitAnimationMixer, 0);

        animator.Rebind();
        animator.Update(0);

        graph.Play();
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

    public void SetIdleAnimations(AnimationClip groundIdleAnimation, AnimationClip airIdleAnimation, AnimationClip climbIdleAnimation)
    {
        if (groundIdleAnimation == null || airIdleAnimation == null) return;

        int activeInput = ResetInputs(idleAnimationMixer);

        //Grounded
        if (groundIdleAnimation != null)
        {
            AnimationClipPlayable groundIdle = AnimationClipPlayable.Create(graph, groundIdleAnimation);
            idleAnimationMixer.ConnectInput(0, groundIdle, 0);
        }

        //Areial
        if (airIdleAnimation != null)
        {
            AnimationClipPlayable airIdle = AnimationClipPlayable.Create(graph, airIdleAnimation);
            idleAnimationMixer.ConnectInput(1, airIdle, 0);
        }

        //Climb
        if (climbIdleAnimation != null)
        {
            AnimationClipPlayable climbIdle = AnimationClipPlayable.Create(graph, climbIdleAnimation);
            idleAnimationMixer.ConnectInput(2, climbIdle, 0);
        }

        ResetInputWeights(idleAnimationMixer);
        
        if (!idleAnimationMixer.GetInput(activeInput).IsNull())
            idleAnimationMixer.SetInputWeight(activeInput, 1);
    }
    
    public void SetHitStunAnimations(AnimationClip hitStunAnimation)
    {
        if (hitStunAnimation == null) return;

        ResetInputs(hitAnimationMixer);

        AnimationClipPlayable hitstunPlayable = AnimationClipPlayable.Create(graph, hitStunAnimation);
        hitAnimationMixer.ConnectInput(0, hitstunPlayable, 0);

        hitAnimationMixer.SetInputWeight(0, 1);
    }

    public void SetMovementAnimations(AnimationClip[] movementAnimations)
    {
        if (movementAnimations == null) return;
        
        //Account for switching animations mid movement by keeping the active input
        int activeIndex = ResetInputs(movementAnimationMixer);
       
        for (int i = 0; i < movementAnimations.Length; i++)
        {
            if (movementAnimations[i] == null) continue;
            AnimationClipPlayable movePlayable = AnimationClipPlayable.Create(graph, movementAnimations[i]);
            movementAnimationMixer.ConnectInput(i, movePlayable, 0);
        }       

        //Set active input
        if (activeIndex > -1)
            movementAnimationMixer.SetInputWeight(activeIndex, 1);
    }

    /// <summary>
    /// Set attack animations to use
    /// </summary>
    public void SetAttackAnimations(AnimationClip[] attackAnimations)
    {
        if (attackAnimations == null) return;

        ResetInputs(attackAnimationMixer);

        for (int i = 0; i < attackAnimations.Length; i++)
        {
            if (attackAnimations[i] == null) continue;
            AnimationClipPlayable attackPlayable = AnimationClipPlayable.Create(graph, attackAnimations[i]);            
            attackAnimationMixer.ConnectInput(i, attackPlayable, 0);
        }
    }

    #endregion


    #region Input Changes

    /// <summary>
    /// Disconnects and destroys inputs.
    /// </summary>
    /// <returns> Which input was active. </returns>
    private int ResetInputs(AnimationMixerPlayable mixer)
    {
        int activeIndex = -1;

        for (int i = 0; i < mixer.GetInputCount(); i++)
        {
            if (mixer.GetInputWeight(i) > 0)
                activeIndex = i;

            Playable animationPlayable = mixer.GetInput(i);

            if (!animationPlayable.IsNull())
                animationPlayable.Destroy();
            
            mixer.DisconnectInput(i);
        }

        return activeIndex;
    }

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
        graph.Destroy();
    }
}
