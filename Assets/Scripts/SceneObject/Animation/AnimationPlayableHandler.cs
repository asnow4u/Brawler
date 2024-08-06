using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class AnimationPlayableHandler : MonoBehaviour
{
    private Animator animator;

    [SerializeField] private AnimationClip groundIdleAnimation;
    [SerializeField] private AnimationClip airIdleAnimation;

    [SerializeField] private string curPlayingAnimation;

    //Playables
    private PlayableGraph animationGraph;
    private AnimationMixerPlayable stateAnimationMixer;
    private AnimationMixerPlayable idleAnimationMixer;
    private AnimationMixerPlayable movementAnimationMixer;
    private AnimationMixerPlayable attackAnimationMixer;
    private AnimationMixerPlayable hitAnimationMixer;

    //Coroutines
    private Coroutine animationEventCorutine;

    //Events
    public event Action<string, AnimationTrigger.Type> OnAnimationUpdateEvent;


    public void Initialize()
    {
        //Animator
        animator = GetComponentInChildren<Animator>();
        Debug.Assert(animator != null, "Animator is Null!", gameObject);
        Debug.Assert(groundIdleAnimation != null, "Ground Idle Animation not set!", gameObject);
        Debug.Assert(airIdleAnimation != null, "Air Idle Animation not set!", gameObject);

        //Graph
        animationGraph = PlayableGraph.Create("AnimationSystem");
        AnimationPlayableOutput output = AnimationPlayableOutput.Create(animationGraph, "Animation", animator);

        //State Mixer
        stateAnimationMixer = AnimationMixerPlayable.Create(animationGraph, 4);
        output.SetSourcePlayable(stateAnimationMixer);

        SetUpIdleMixer();
        SetUpMovementMixer();
        SetUpAttackMixer();
        SetUpHitMixer();

        //Set idle state to 1
        stateAnimationMixer.SetInputWeight(0, 1);    

        animationGraph.Play();

        SetUpEvents();
    }

    #region Mixers

    private void SetUpIdleMixer()
    {
        idleAnimationMixer = AnimationMixerPlayable.Create(animationGraph, 2);
        stateAnimationMixer.ConnectInput(0, idleAnimationMixer, 0);

        AnimationClipPlayable groundIdle = AnimationClipPlayable.Create(animationGraph, groundIdleAnimation);
        AnimationClipPlayable airIdle = AnimationClipPlayable.Create(animationGraph, airIdleAnimation);

        idleAnimationMixer.ConnectInput(0, groundIdle, 0);
        idleAnimationMixer.ConnectInput(1, airIdle, 0);
    }


    private void SetUpMovementMixer()
    {
        if (TryGetComponent(out MovementInputHandler movementHandler))
        {           
            movementAnimationMixer = AnimationMixerPlayable.Create(animationGraph, 4);
            stateAnimationMixer.ConnectInput(1, movementAnimationMixer, 0);

            //Move
            if (movementHandler.CurMovementCollection.TryGetMovementByType(MovementType.Move, out MovementData moveData))
            {
                AnimationMixerPlayable walkMixer = AnimationMixerPlayable.Create(animationGraph, 2);
                movementAnimationMixer.ConnectInput(0, walkMixer, 0);
        
                //AnimationClipPlayable walk = AnimationClipPlayable.Create(animationGraph, );
                //AnimationClipPlayable run = AnimationClipPlayable.Create(animationGraph, );

                //movementAnimationMixer.ConnectInput(0, walk);
                //movementAnimationMixer.ConnectInput(0, run);
            }

            //Jump
            if (movementHandler.CurMovementCollection.TryGetMovementByType(MovementType.Jump, out MovementData jumpData))
            {
                AnimationClipPlayable jump = AnimationClipPlayable.Create(animationGraph, jumpData.Animation);
                movementAnimationMixer.ConnectInput(1, jump, 0);
            }

            //Air Jump
            if (movementHandler.CurMovementCollection.TryGetMovementByType(MovementType.AirJump, out MovementData airJumpData))
            {
                AnimationClipPlayable airJump = AnimationClipPlayable.Create(animationGraph, airJumpData.Animation);
                movementAnimationMixer.ConnectInput(2, airJump, 0);
            }

            //Land
            if (movementHandler.CurMovementCollection.TryGetMovementByType(MovementType.Landing, out MovementData landData))
            {
                AnimationClipPlayable land = AnimationClipPlayable.Create(animationGraph, landData.Animation);
                movementAnimationMixer.ConnectInput(3, land, 0);
            }
        }
    }


    private void SetUpAttackMixer()
    {
        if (TryGetComponent(out AttackInputHandler attackHandler))
        {
            attackAnimationMixer = AnimationMixerPlayable.Create(animationGraph, 2);
            stateAnimationMixer.ConnectInput(2, attackAnimationMixer, 0);

            //Down Tilt
            if (attackHandler.CurAttackCollection.TryGetAttackByType(AttackType.DownTilt, out AttackData downTiltData))
            {
                AnimationClipPlayable downTilt = AnimationClipPlayable.Create(animationGraph,downTiltData.AttackAnimation);
                attackAnimationMixer.ConnectInput(0, downTilt, 0);
            }

            //Forward Tilt
            if (attackHandler.CurAttackCollection.TryGetAttackByType(AttackType.ForwardTilt, out AttackData forwardTiltData))
            {
                AnimationClipPlayable forwardTilt = AnimationClipPlayable.Create(animationGraph, forwardTiltData.AttackAnimation);
                attackAnimationMixer.ConnectInput(0, forwardTilt, 0);
            }

            //Up Tilt
            if (attackHandler.CurAttackCollection.TryGetAttackByType(AttackType.UpTilt, out AttackData upTiltData))
            {
                AnimationClipPlayable upTilt = AnimationClipPlayable.Create(animationGraph, upTiltData.AttackAnimation);
                attackAnimationMixer.ConnectInput(0, upTilt, 0);
            }

            //Down Air
            if (attackHandler.CurAttackCollection.TryGetAttackByType(AttackType.DownAir, out AttackData downAirData))
            {
                AnimationClipPlayable downAir = AnimationClipPlayable.Create(animationGraph, downAirData.AttackAnimation);
                attackAnimationMixer.ConnectInput(0, downAir, 0);
            }

            //Forward Air
            if (attackHandler.CurAttackCollection.TryGetAttackByType(AttackType.ForwardAir, out AttackData forwardAirData))
            {
                AnimationClipPlayable forwardAir = AnimationClipPlayable.Create(animationGraph, forwardAirData.AttackAnimation);
                attackAnimationMixer.ConnectInput(0, forwardAir, 0);
            }

            //Up Air
            if (attackHandler.CurAttackCollection.TryGetAttackByType(AttackType.UpAir, out AttackData upAirData))
            {
                AnimationClipPlayable upAir = AnimationClipPlayable.Create(animationGraph, upAirData.AttackAnimation);
                attackAnimationMixer.ConnectInput(0, upAir, 0);
            }
        }
    }


    private void SetUpHitMixer()
    {
        hitAnimationMixer = AnimationMixerPlayable.Create(animationGraph, 2);
        stateAnimationMixer.ConnectInput(3, hitAnimationMixer, 0);
    }


    #endregion

    private void SetUpEvents()
    {
        if (TryGetComponent(out ActionStateHandler actionStateHandler))
            actionStateHandler.ActionStateChangedEvent += OnActionStateChanged;

        SceneObject sceneObject = GetComponent<SceneObject>();
        sceneObject.GroundedStateChangeEvent += OnGroundedStateChanged;

        if (TryGetComponent(out MovementInputHandler movementHandler))            
            movementHandler.MoveStateChangedEvent += OnMovementStateChanged;

        if (TryGetComponent(out AttackInputHandler attackHandler))
            attackHandler.AttackStateChangedEvent += OnAttackStateChanged;
    }


    #region Events

    /// <summary>
    /// Change stateMixer to prioritize current actionState
    /// </summary>
    /// <param name="actionState"></param>
    private void OnActionStateChanged(ActionState actionState)
    {
        ResetInputWeights(stateAnimationMixer);

        switch(actionState)
        {
            case ActionState.Moving:
                stateAnimationMixer.SetInputWeight(0, 1);
                break;

            case ActionState.Attacking:
                stateAnimationMixer.SetInputWeight(1, 1);
                break;

            case ActionState.HitStun:
                stateAnimationMixer.SetInputWeight(2, 1);
                break;

            default:
                stateAnimationMixer.SetInputWeight(3, 1);
                break;
        }
    }


    private void OnGroundedStateChanged(GroundedState groundedState)
    {
        ResetInputWeights(idleAnimationMixer);

        if (groundedState == GroundedState.Airborn)
            idleAnimationMixer.SetInputWeight(1, 1);        
        else
            idleAnimationMixer.SetInputWeight(0, 1);        
    }


    private void OnMovementStateChanged(MovementType moveState)
    {
        ResetInputWeights(movementAnimationMixer);

        switch (moveState)
        {
            case MovementType.Move:
                movementAnimationMixer.SetInputWeight(0, 1);
                break;

            case MovementType.Jump:
                movementAnimationMixer.SetInputWeight(1, 1);
                break;

            case MovementType.AirJump:
                movementAnimationMixer.SetInputWeight(2, 1);
                break;

            case MovementType.Landing:
                movementAnimationMixer.SetInputWeight(3, 1);
                break;
        }
    }


    private void OnAttackStateChanged(AttackType attackState)
    {
        ResetInputWeights(attackAnimationMixer);

        switch (attackState)
        {
            case AttackType.DownTilt:
                attackAnimationMixer.SetInputWeight(0, 1);
                break;

            case AttackType.ForwardTilt:
                attackAnimationMixer.SetInputWeight(1, 1);
                break;

            case AttackType.UpTilt:
                attackAnimationMixer.SetInputWeight(2, 1);
                break;

            case AttackType.DownAir:
                attackAnimationMixer.SetInputWeight(3, 1);
                break;

            case AttackType.ForwardAir:
                attackAnimationMixer.SetInputWeight(4, 1);
                break;

            case AttackType.UpAir:
                attackAnimationMixer.SetInputWeight(5, 1);
                break;
        }
    }

    #endregion


    private void ResetInputWeights(AnimationMixerPlayable mixer)
    {
        for (int i=0; i < mixer.GetInputCount(); i++)
        {
            mixer.SetInputWeight(i, 0);
        }
    }


    private void OnDestroy()
    {
        animationGraph.Destroy();
    }


    #region Getters

    /// <summary>
    /// Return the current running animation clip info
    /// </summary>
    /// <returns></returns>
    private AnimationClip GetCurrentPlayingAnimation()
    {
        AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);

        if (clipInfo.Length > 0)
            return clipInfo[0].clip;

        return null;
    }


    /// <summary>
    /// Returns the normalized percentage of the animation playtime (0-1)
    /// </summary>
    /// <returns></returns>
    private float GetCurAnimationNormalizedTime()
    {
        AnimatorStateInfo animationInfo = animator.GetCurrentAnimatorStateInfo(0);
        return animationInfo.normalizedTime;
    }


    /// <summary>
    /// Return if this current animation loops
    /// </summary>
    /// <returns></returns>
    private bool IsCurAnimationLooping()
    {
        AnimatorStateInfo animationInfo = animator.GetCurrentAnimatorStateInfo(0);
        return animationInfo.loop;
    }


    /// <summary>
    /// Returns the current frame of the playing animation
    /// </summary>
    /// <returns></returns>
    public int GetCurrentFrameOfCurAnimation()
    {
        AnimatorStateInfo animationInfo = animator.GetCurrentAnimatorStateInfo(0);
        return Mathf.RoundToInt(animationInfo.normalizedTime * GetCurrentPlayingAnimation().frameRate);
    }


    #endregion


    #region Play Animation

    //TODO: Will need to determine weapon
    //TODO: Debate on using layers for different weapon animations or states (Will need to look into adjusting priority)
    private void PlayIdleAnimation()
    {
        //if (sceneObject.GroundedState == GroundedState.Airborn)
        //    PlayAnimation(new AnimationStateData("BaseAirIdle", ActionState.Idle, null));

        //else
        //    PlayAnimation(new AnimationStateData("BaseIdle", ActionState.Idle, null));
    }


    /// <summary>
    /// Update the current ActionState <br/>
    /// Start playing animation.
    /// </summary>
    /// <param name="animationName"></param>
    /// <param name="animationTriggers"></param>
    public void PlayAnimation(AnimationStateData animationData)
    {
        if (animationData != null)
        {           
            if (animationData.ClipName != curPlayingAnimation)
            {
                Debug.Log("ANIMATION: Start " + animationData.ClipName);
                animator.Play("Base Layer." + animationData.ClipName);
                StartCoroutine(WaitForAnimationStart(animationData.ClipName, animationData.Triggers));
            }
        }
    }


    /// <summary>
    /// Check for animation equal to or below the provided state
    /// Reset action state and play idle animation
    /// </summary>
    public void EndCurrentAnimation(ActionState priorityState)
    {
        //if (IsStatePossible(priorityState))
        //{
        //    Debug.Log("ANIMATION: End " + curPlayingAnimation);
        //    ResetState();
        //    PlayIdleAnimation();
        //}
    }


    /// <summary>
    /// Wait till the animation begins playing. 
    /// </summary>
    /// <param name="waitingAnimation"></param>
    /// <param name="animationTriggers"></param>
    /// <returns></returns>
    private IEnumerator WaitForAnimationStart(string waitingAnimation, AnimationTrigger[] animationTriggers)
    {
        int waitingHashID = Animator.StringToHash(waitingAnimation);

        while (waitingHashID != animator.GetCurrentAnimatorStateInfo(0).shortNameHash)
        {
            yield return null;
        }

        InitializeAnimation(waitingAnimation, animationTriggers);
    }


    /// <summary>
    /// Update the currentPlayingAnimation. <br/> 
    /// Send End AnimationTrigger event if previous animation was active. <br/>
    /// Set up coroutine for trigger events
    /// </summary>
    /// <param name="animationName"></param>
    /// <param name="animationTriggers"></param>
    private void InitializeAnimation(string animationName, AnimationTrigger[] animationTriggers)
    {
        //Stop previous animation events
        if (animationEventCorutine != null)
            StopCoroutine(animationEventCorutine);

        //Send end event for previous animation
        if (curPlayingAnimation != null && curPlayingAnimation != animationName)
        {
            Debug.Log("ANIMATION: End " + curPlayingAnimation);
            OnAnimationUpdateEvent?.Invoke(curPlayingAnimation, AnimationTrigger.Type.End);
        }

        curPlayingAnimation = animationName;

        //Reset Animation Triggers
        ResetAnimationTriggers(animationTriggers);

        //Start animation events
        animationEventCorutine = StartCoroutine(CheckAnimationTriggerEvents(animationTriggers));
        OnAnimationUpdateEvent?.Invoke(curPlayingAnimation, AnimationTrigger.Type.Start);
    }


    /// <summary>
    /// Check each frame for any animationTrigger events that need to fire
    /// </summary>
    /// <param name="animationTriggers"></param>
    /// <returns></returns>
    private IEnumerator CheckAnimationTriggerEvents(AnimationTrigger[] animationTriggers)
    {
        //Looping animation
        if (IsCurAnimationLooping())
        {
            while (IsCurAnimationLooping())
            {
                CheckForAnimationTrigger(animationTriggers);

                if (GetCurAnimationNormalizedTime() == 1)
                    ResetAnimationTriggers(animationTriggers);

                yield return null;
            }
        }

        //Loop while animation is running
        while (GetCurAnimationNormalizedTime() < 1)
        {
            CheckForAnimationTrigger(animationTriggers);

            yield return null;
        }

        //End Animation
        //EndCurrentAnimation(ActionState.Admin);
    }


    private void CheckForAnimationTrigger(AnimationTrigger[] animationTriggers)
    {
        if (animationTriggers != null)
        {
            float curFrame = GetCurrentFrameOfCurAnimation();

            foreach (AnimationTrigger trigger in animationTriggers)
            {
                if (!trigger.WasTriggered && curFrame >= trigger.TriggerFrame)
                {
                    OnAnimationUpdateEvent?.Invoke(curPlayingAnimation, trigger.TriggerType);
                    trigger.WasTriggered = true;

                    //End Animation                    
                    if (trigger.TriggerType == AnimationTrigger.Type.End)
                    {
                        //EndCurrentAnimation(ActionState.Admin);
                    }
                }
            }
        }
    }


    private void ResetAnimationTriggers(AnimationTrigger[] animationTriggers)
    {
        if (animationTriggers != null)
        {
            foreach (AnimationTrigger trigger in animationTriggers)
                trigger.Reset();
        }
    }

    #endregion



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
