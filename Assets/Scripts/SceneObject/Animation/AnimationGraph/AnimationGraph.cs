using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.UIElements;

internal class AnimationGraph : IDisposable
{
    // Playables
    private PlayableGraph graph;
    private AnimationMixerPlayable stateMixer;
    private const int LayerCount = 6;
    private AnimatorControllerPlayable[] controllers = new AnimatorControllerPlayable[LayerCount];

    private class ControllerBinding 
    {
        public AnimatorOverrideController OverrideController;
        public Dictionary<Enum, AnimationClip> Clips;
        public Dictionary<Enum, float> Speeds;

        public ControllerBinding(AnimatorOverrideController controller, Dictionary<Enum, AnimationClip> clips)
        {
            OverrideController = controller;
            Clips = clips;
            Speeds = new Dictionary<Enum, float>();
        }
    }

    private ControllerBinding[] controllerBindings = new ControllerBinding[LayerCount];

    // Blending state
    private int currentIndex = -1;
    private int targetIndex = -1;
    private float blendTimer = 0f;
    private float pauseTimer = 0f;
    private const float BlendDuration = 0.1f;

    public AnimationGraph(Animator animator, 
        RuntimeAnimatorController idleControllerAsset, 
        RuntimeAnimatorController moveControllerAsset, 
        RuntimeAnimatorController guardControllerAsset, 
        RuntimeAnimatorController attackControllerAsset, 
        RuntimeAnimatorController staggerControllerAsset, 
        RuntimeAnimatorController hitstunControllerAsset)
    {
        // Initialize Graph
        graph = PlayableGraph.Create("AnimationGraph");        
        AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "Animation", animator);

        // State Mixer: layer index matches the ActionState value. 0 = Idle, 1 = Movement, 2 = Guard, 3 = Attack, 4 = Stagger, 5 = Hitstun
        stateMixer = AnimationMixerPlayable.Create(graph, LayerCount);
        output.SetSourcePlayable(stateMixer);

        // Setup individual layers
        SetupControllerLayer(0, idleControllerAsset);
        SetupControllerLayer(1, moveControllerAsset);
        SetupControllerLayer(2, guardControllerAsset);
        SetupControllerLayer(3, attackControllerAsset);
        SetupControllerLayer(4, staggerControllerAsset);
        SetupControllerLayer(5, hitstunControllerAsset);

        graph.Play();
    }

    private void SetupControllerLayer(int index, RuntimeAnimatorController asset)
    {
        if (asset == null) return;

        AnimatorOverrideController overrideController = new AnimatorOverrideController(asset);
        Dictionary<Enum, AnimationClip> keyedAnimations = GetKeyedAnimations(index, overrideController);

        ControllerBinding controllerBinding = new ControllerBinding(overrideController, keyedAnimations);
        controllerBindings[index] = controllerBinding;

        var controllerPlayable = AnimatorControllerPlayable.Create(graph, controllerBinding.OverrideController);
        controllers[index] = controllerPlayable;

        stateMixer.ConnectInput(index, controllerPlayable, 0);
        stateMixer.SetInputWeight(index, 0f);
    }

    private Dictionary<Enum, AnimationClip> GetKeyedAnimations(int index, AnimatorOverrideController overrideController)
    {
        List<KeyValuePair<AnimationClip, AnimationClip>> animationOverrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        overrideController.GetOverrides(animationOverrides);

        Dictionary<Enum, AnimationClip> keyedAnimations = new Dictionary<Enum, AnimationClip>();
        foreach (var kvp in animationOverrides)
        {
            Enum state = GetAnimationEnumState(index, kvp.Key);
            if (state == null)
            {
                Debug.LogError("Animation: Unable to find base animation for " +  kvp.Key.name);
                continue;
            }

            keyedAnimations.Add(state, kvp.Key);
        }

        return keyedAnimations;
    }

    private Enum GetAnimationEnumState(int index, AnimationClip animationClip)
    {
        switch (index)
        {
            case 0:

                foreach (IdleState state in Enum.GetValues(typeof(IdleState)))
                {
                    if (animationClip.name.Contains(state.ToString()))
                        return state;
                }

                break;
                
            case 1:

                foreach (MovementState state in Enum.GetValues(typeof(MovementState)))
                {
                    if (animationClip.name.Contains(state.ToString()))
                        return state;
                }

                break;

            case 2:

                foreach (GuardState state in Enum.GetValues(typeof(GuardState)))
                {
                    if (animationClip.name.Contains(state.ToString()))
                        return state;
                }

                break;

            case 3:

                foreach (AttackState state in Enum.GetValues(typeof(AttackState)))
                {
                    if (animationClip.name.Contains(state.ToString()))
                        return state;
                }

                break;

            case 4:

                foreach (StaggerState state in Enum.GetValues(typeof(StaggerState)))
                {
                    if (animationClip.name.Contains(state.ToString()))
                        return state;
                }

                break;

            case 5:

                foreach (HitStunState state in Enum.GetValues(typeof(HitStunState)))
                {
                    if (animationClip.name.Contains(state.ToString()))
                        return state;
                }

                break;
        }

        return null;
    }

    /// <summary>
    /// Updates the smooth blending between action states.
    /// Should be called every frame from the owner.
    /// </summary>
    public void Update()
    {
        if (pauseTimer > 0)
        {
            pauseTimer -= Time.deltaTime;
            if (pauseTimer <= 0)
            {
                stateMixer.SetSpeed(1f);
            }
            else
            {
                return;
            }
        }

        if (targetIndex == -1) return;

        // Handle blending transitions
        if (currentIndex != targetIndex)
        {
            blendTimer += Time.deltaTime;
            float t = Mathf.Clamp01(blendTimer / BlendDuration);

            // Fade out current
            if (currentIndex != -1)
                stateMixer.SetInputWeight(currentIndex, 1f - t);
            
            // Fade in target
            stateMixer.SetInputWeight(targetIndex, t);

            // End transition
            if (t >= 1f)
            {
                if (currentIndex != -1 && currentIndex != targetIndex)
                    stateMixer.SetInputWeight(currentIndex, 0f);
                
                currentIndex = targetIndex;
            }
        }
        else
        {
            // Ensure target has full weight if not blending
            stateMixer.SetInputWeight(targetIndex, 1f);
        }
    }

    public void OnActionStateChanged(ActionState newState)
    {
        if (newState == ActionState.Null) return;

        Resume();
        
        int newIndex = (int)newState;
        
        if (newIndex < 0 || newIndex >= LayerCount || controllers[newIndex].IsNull()) return;
        if (newIndex == targetIndex) return;

        if (currentIndex != -1 && targetIndex != -1 && currentIndex != targetIndex)
            currentIndex = GetHighestWeightIndex();

        targetIndex = newIndex;
        blendTimer = 0f;

        //Reset other weights
        for (int i = 0; i < LayerCount; i++)
        {
            if (i != currentIndex && i != targetIndex)
                stateMixer.SetInputWeight(i, 0f);
        }

        // Immediate switch if this is the first state
        if (currentIndex == -1)
        {
            currentIndex = targetIndex;
            stateMixer.SetInputWeight(currentIndex, 1f);
        }
    }

    private int GetHighestWeightIndex()
    {
        int best = 0;
        float bestWeight = stateMixer.GetInputWeight(0);

        for (int i = 1; i < LayerCount; i++)
        {
            float w = stateMixer.GetInputWeight(i);
            if (w > bestWeight)
            {
                bestWeight = w;
                best = i;
            }
        }

        return best;
    }

    #region Substate Forwarding (Pass-Through)

    public void ChangeIdleStateInput(IdleState state) => ForwardToController(0, state);
    public void ChangeMovementStateInput(MovementState state) => ForwardToController(1, state);
    public void ChangeGuardStateInput(GuardState state) => ForwardToController(2, state);
    public void ChangeAttackStateInput(AttackState state) => ForwardToController(3, state);
    public void ChangeStaggerStateInput(StaggerState state) => ForwardToController(4, state);
    public void ChangeHitStunStateInput(HitStunState state) => ForwardToController(5, state); 

    private void ForwardToController(int layerIndex, Enum stateValue)
    {
        if (layerIndex >= 0 && layerIndex < LayerCount && !controllers[layerIndex].IsNull())
        {
            Resume();

            float speed = 1f;
            ControllerBinding binding = controllerBindings[layerIndex];
            if (binding != null && binding.Speeds.TryGetValue(stateValue, out float storedSpeed))
                speed = storedSpeed;
            
            controllers[layerIndex].SetFloat("SpeedMultiplier", speed);
            controllers[layerIndex].SetInteger("State", Convert.ToInt32(stateValue));
            controllers[layerIndex].Play(stateValue.ToString(), 0, 0);
        }
    }

    public void Pause(float seconds)
    {
        if (seconds <= 0) return;
        pauseTimer = seconds;
        stateMixer.SetSpeed(0f);
    }

    private void Resume()
    {
        pauseTimer = 0f;
        if (stateMixer.IsValid())
            stateMixer.SetSpeed(1f);
    }

    #endregion


    #region Dynamic Animation Updates

    public void SetIdleAnimations(AnimationStatData.AnimationData groundIdleAnimation, AnimationStatData.AnimationData airIdleAnimation)
    {
        ApplyOverride(0, IdleState.GroundIdle, groundIdleAnimation);
        ApplyOverride(0, IdleState.AirIdle, airIdleAnimation);
    }    

    public void SetMovementAnimations(Dictionary<int, AnimationStatData.AnimationData> movementAnimations)
    {
        if (movementAnimations == null) return;
        foreach (var kvp in movementAnimations)
            ApplyOverride(1, (MovementState)kvp.Key, kvp.Value);
    }

    public void SetAttackAnimations(Dictionary<int, AnimationStatData.AnimationData> attackAnimations)
    {
        if (attackAnimations == null) return;
        foreach (var kvp in attackAnimations)
            ApplyOverride(3, (AttackState)kvp.Key, kvp.Value);
    }

    private void ApplyOverride(int layerIndex, Enum state, AnimationStatData.AnimationData animationData)
    {
        if (animationData == null || animationData.Animation == null || controllerBindings[layerIndex] == null) return;

        ControllerBinding binding = controllerBindings[layerIndex];
        if (!binding.Clips.TryGetValue(state, out AnimationClip originalClip))
            return;

        binding.OverrideController[originalClip] = animationData.Animation;
        binding.Speeds[state] = animationData.AnimationSpeed;
        controllers[layerIndex].SetTime(0);
    }

    public void SetHitStunAnimations(AnimationStatData.AnimationData hitStunAnimation)
    {
        ApplyOverride(5, HitStunState.Launch, hitStunAnimation);
    }

    /// <summary>Applies one clip to every guard state except Counter.</summary>
    public void SetGuardAnimation(AnimationStatData.AnimationData guardAnimation)
    {
        foreach (GuardState state in Enum.GetValues(typeof(GuardState)))
        {
            if (state != GuardState.Null && state != GuardState.Counter)
                ApplyOverride(2, state, guardAnimation);
        }
    }

    public void SetCounterAnimation(AnimationStatData.AnimationData counterAnimation)
    {
        ApplyOverride(2, GuardState.Counter, counterAnimation);
    }

    public void SetStaggerAnimation(AnimationStatData.AnimationData staggerAnimation)
    {
        ApplyOverride(4, StaggerState.Stagger, staggerAnimation);
    }

    #endregion

    /// <summary>Destroys the graph, then the override controllers it was using.</summary>
    public void Dispose()
    {
        if (graph.IsValid())
            graph.Destroy();

        for (int i = 0; i < controllerBindings.Length; i++)
        {
            if (controllerBindings[i]?.OverrideController != null)
                UnityEngine.Object.Destroy(controllerBindings[i].OverrideController);

            controllerBindings[i] = null;
        }
    }
}

