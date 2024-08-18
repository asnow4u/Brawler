using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class AnimationGraph
{
    private Animator animator;

    //Playables
    private PlayableGraph animationGraph;
    private AnimationMixerPlayable stateAnimationMixer;
    private AnimationMixerPlayable idleAnimationMixer;
    private AnimationMixerPlayable movementAnimationMixer;
    private AnimationMixerPlayable attackAnimationMixer;
    private AnimationMixerPlayable hitAnimationMixer;


    public AnimationGraph(Animator animator)
    {
        this.animator = animator;

        //Graph
        animationGraph = PlayableGraph.Create("AnimationGraph");
        AnimationPlayableOutput output = AnimationPlayableOutput.Create(animationGraph, "Animation", animator);

        //State Mixer
        stateAnimationMixer = AnimationMixerPlayable.Create(animationGraph, Enum.GetValues(typeof(ActionState)).Length);
        output.SetSourcePlayable(stateAnimationMixer);

        SetupMixers();
            
        stateAnimationMixer.SetInputWeight(0, 1);
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
        movementAnimationMixer = AnimationMixerPlayable.Create(animationGraph, 4);
        stateAnimationMixer.ConnectInput((int)ActionState.Moving, movementAnimationMixer, 0);        
    }


    /// <summary>
    /// Setup attack mixer for attack animations
    /// Inputs: forwardTilt, upTilt, downTilt, forwardAir, upAir, downAir
    /// </summary>
    /// <param name="attackCollection"></param>
    private void SetupAttackMixer()
    {
        attackAnimationMixer = AnimationMixerPlayable.Create(animationGraph, 6);
        stateAnimationMixer.ConnectInput((int)ActionState.Attacking, attackAnimationMixer, 0);
    }


    /// <summary>
    /// Setup hit mixer for hit animations
    /// Input: 
    /// </summary>
    private void SetupHitStunMixer()
    {
        hitAnimationMixer = AnimationMixerPlayable.Create(animationGraph, 2);
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
        //Move
        if (moveCollection.TryGetMovementByType(MovementType.Move, out MovementData moveData))
        {
            //AnimationMixerPlayable walkMixer = AnimationMixerPlayable.Create(animationGraph, 2);
            //movementAnimationMixer.ConnectInput(0, walkMixer, 0);

            //AnimationClipPlayable walk = AnimationClipPlayable.Create(animationGraph, );
            //AnimationClipPlayable run = AnimationClipPlayable.Create(animationGraph, );

            //movementAnimationMixer.ConnectInput(0, walk);
            //movementAnimationMixer.ConnectInput(0, run);
        }

        //Jump
        if (moveCollection.TryGetMovementByType(MovementType.Jump, out MovementData jumpData))
        {
            AnimationClipPlayable jump = AnimationClipPlayable.Create(animationGraph, jumpData.Animation);
            movementAnimationMixer.ConnectInput((int)MovementType.Jump, jump, 0);
        }

        //Air Jump
        if (moveCollection.TryGetMovementByType(MovementType.AirJump, out MovementData airJumpData))
        {
            AnimationClipPlayable airJump = AnimationClipPlayable.Create(animationGraph, airJumpData.Animation);
            movementAnimationMixer.ConnectInput((int)MovementType.AirJump, airJump, 0);
        }

        //Land
        if (moveCollection.TryGetMovementByType(MovementType.Landing, out MovementData landData))
        {
            AnimationClipPlayable land = AnimationClipPlayable.Create(animationGraph, landData.Animation);
            movementAnimationMixer.ConnectInput((int)MovementType.Landing, land, 0);
        }
    }


    /// <summary>
    /// Set attack animations to use
    /// </summary>
    /// <param name="attackCollection"></param>
    public void SetAttackAnimations(AttackCollection attackCollection)
    {   
        //Down Tilt
        if (attackCollection.TryGetAttackByType(AttackType.DownTilt, out AttackData downTiltData))
        {
            AnimationClipPlayable downTilt = AnimationClipPlayable.Create(animationGraph,downTiltData.AttackAnimation);
            attackAnimationMixer.ConnectInput((int)AttackType.DownTilt, downTilt, 0);
        }

        //Forward Tilt
        if (attackCollection.TryGetAttackByType(AttackType.ForwardTilt, out AttackData forwardTiltData))
        {
            AnimationClipPlayable forwardTilt = AnimationClipPlayable.Create(animationGraph, forwardTiltData.AttackAnimation);
            attackAnimationMixer.ConnectInput((int)AttackType.ForwardTilt, forwardTilt, 0);
        }

        //Up Tilt
        if (attackCollection.TryGetAttackByType(AttackType.UpTilt, out AttackData upTiltData))
        {
            AnimationClipPlayable upTilt = AnimationClipPlayable.Create(animationGraph, upTiltData.AttackAnimation);
            attackAnimationMixer.ConnectInput((int)AttackType.UpTilt, upTilt, 0);
        }

        //Down Air
        if (attackCollection.TryGetAttackByType(AttackType.DownAir, out AttackData downAirData))
        {
            AnimationClipPlayable downAir = AnimationClipPlayable.Create(animationGraph, downAirData.AttackAnimation);
            attackAnimationMixer.ConnectInput((int)AttackType.DownAir, downAir, 0);
        }

        //Forward Air
        if (attackCollection.TryGetAttackByType(AttackType.ForwardAir, out AttackData forwardAirData))
        {
            AnimationClipPlayable forwardAir = AnimationClipPlayable.Create(animationGraph, forwardAirData.AttackAnimation);
            attackAnimationMixer.ConnectInput((int)AttackType.ForwardAir, forwardAir, 0);
        }

        //Up Air
        if (attackCollection.TryGetAttackByType(AttackType.UpAir, out AttackData upAirData))
        {
            AnimationClipPlayable upAir = AnimationClipPlayable.Create(animationGraph, upAirData.AttackAnimation);
            attackAnimationMixer.ConnectInput((int)AttackType.UpAir, upAir, 0);
        }
        
    }


    /// <summary>
    /// Set hitstun animations to use
    /// </summary>
    public void SetHitStunAnimations()
    {
        throw new NotImplementedException();
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
    public void ChangeMovementStateInput(MovementType moveState)
    {
        ResetInputWeights(movementAnimationMixer);

        movementAnimationMixer.SetInputWeight((int)moveState, 1);        
    }


    /// <summary>
    /// Change attack mixer to prioritize current attack state
    /// </summary>
    /// <param name="attackState"></param>
    public void ChangeAttackStateInput(AttackType attackState)
    {
        ResetInputWeights(attackAnimationMixer);

        attackAnimationMixer.SetInputWeight((int)attackState, 1);        
    }

    #endregion


    private void OnDestroy()
    {
        animationGraph.Destroy();
    }










    #region Perameter Setting

    public void SetFloatPerameter(string name, float value)
    {
        animator.SetFloat(name, value);
    }

    #endregion


    //DEBUG
    [ContextMenu("SetToIdle")]
    public void SetToIdle()
    {
        ResetInputWeights(stateAnimationMixer);
        stateAnimationMixer.SetInputWeight(0, 1);
    }

    [ContextMenu("SetToMovement")]
    public void SetToMovement()
    {
        ResetInputWeights(stateAnimationMixer);
        stateAnimationMixer.SetInputWeight(1, 1);
    }

    [ContextMenu("SetToAttack")]
    public void SetToAttack()
    {
        ResetInputWeights(stateAnimationMixer);
        stateAnimationMixer.SetInputWeight(2, 1);
    }
}
