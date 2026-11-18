using System.Collections.Generic;
using UnityEngine;

public partial class EffectsHandler
{
    private const string CounterGroupName = "Counter";

    private CounterBlastEffectData counterData;
    private CounterBlastRig counterRig;
    private readonly List<LineRenderer> counterRingLayers = new List<LineRenderer>();
    private Vector3[] counterRingPositions;
    private int counterActiveRingLayers;
    private bool isCounterEffectActive;
    private Vector3 counterCenter;
    private float counterAge;
    private float counterRingFadeTimer;
    private float counterRingRadius;
    private float counterRingProgress;


    #region Setup

    /// <summary>Creates the counter blast under Effects/Counter when there is counter data and an IGuard.</summary>
    private void SetupCounterBlast()
    {
        CounterBlastEffectData data = effectsData.CounterBlast;
        if (data == null || data.Prefab == null || guard == null)
            return;

        counterData = data;
        counterRig = InstantiateEffect(data.Prefab, GetEffectGroup(CounterGroupName));

        if (counterRig.RingLayerTemplate != null)
            counterRig.RingLayerTemplate.enabled = false;

        StopCounterBlast();
        guard.GuardStateChangedEvent += OnGuardStateChangedCounter;
    }

    private void TeardownCounterBlast()
    {
        if (counterData == null)
            return;

        guard.GuardStateChangedEvent -= OnGuardStateChangedCounter;
        DestroyInstance(counterRig);

        counterData = null;
        counterRig = null;
        counterRingLayers.Clear();
        counterActiveRingLayers = 0;
        isCounterEffectActive = false;
    }

    private LineRenderer GetCounterRingLayer(int index)
    {
        while (counterRingLayers.Count <= index)
        {
            LineRenderer layer = Instantiate(counterRig.RingLayerTemplate, counterRig.RingLayerTemplate.transform.parent);
            layer.name = $"{counterRig.RingLayerTemplate.name}_{counterRingLayers.Count}";
            counterRingLayers.Add(layer);
        }

        return counterRingLayers[index];
    }

    #endregion


    #region State

    private void OnGuardStateChangedCounter(GuardState state)
    {
        if (state == GuardState.Counter)
            StartCounterBlast();
    }

    /// <summary>Starts the ring, beams and flash at the blast hitbox center.</summary>
    private void StartCounterBlast()
    {
        counterCenter = guard.CounterBlastCenter;
        counterAge = 0f;
        counterRingFadeTimer = 0f;
        counterRingRadius = guard.CounterBlastRadius;
        counterRingProgress = guard.CounterBlastProgress;
        isCounterEffectActive = true;

        SetWorldPose(counterRig.transform, counterCenter, Vector3.one);

        StartCounterRing();
        StartCounterBeams();

        if (counterRig.Flash != null)
        {
            counterRig.Flash.transform.localScale = Vector3.zero;
            counterRig.Flash.enabled = true;
        }

        if (counterRig.Halo != null)
        {
            counterRig.Halo.transform.localScale = Vector3.zero;
            counterRig.Halo.enabled = true;
        }
    }

    private void StartCounterRing()
    {
        if (counterRig.Ring == null)
            return;

        int segments = Mathf.Clamp(counterData.RingSegments, 8, 256);
        if (counterRingPositions == null || counterRingPositions.Length != segments)
            counterRingPositions = new Vector3[segments];

        float intensity = counterData.Intensity;
        SetRendererBaseColor(counterRig.Ring, new Color(intensity, intensity, intensity, 1f));
        counterRig.Ring.useWorldSpace = true;
        counterRig.Ring.loop = true;
        counterRig.Ring.positionCount = segments;
        counterRig.Ring.enabled = true;

        foreach (LineRenderer layer in counterRingLayers)
            layer.enabled = false;

        counterActiveRingLayers = counterRig.RingLayerTemplate != null && counterData.RingLayers != null ? counterData.RingLayers.Length : 0;
        for (int i = 0; i < counterActiveRingLayers; i++)
        {
            LineRenderer layer = GetCounterRingLayer(i);
            layer.useWorldSpace = true;
            layer.loop = true;
            layer.positionCount = segments;
            layer.enabled = true;
        }
    }

    /// <summary>Plays the beam and beam glow systems with a shared random seed and a random rotation.</summary>
    private void StartCounterBeams()
    {
        uint seed = (uint)Random.Range(1, int.MaxValue);
        Quaternion rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

        if (counterRig.Beams != null)
        {
            float intensity = counterData.Intensity;
            SetRendererBaseColor(counterRig.Beams.GetComponent<ParticleSystemRenderer>(), new Color(intensity, intensity, intensity, 1f));
        }

        PlayCounterBeamSystem(counterRig.Beams, seed, rotation);
        PlayCounterBeamSystem(counterRig.BeamGlow, seed, rotation);
    }

    private static void PlayCounterBeamSystem(ParticleSystem system, uint seed, Quaternion rotation)
    {
        if (system == null)
            return;

        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        system.useAutoRandomSeed = false;
        system.randomSeed = seed;
        system.transform.localRotation = rotation;
        system.Play(true);
    }

    /// <summary>Hides the ring and flash and clears the beams.</summary>
    private void StopCounterBlast()
    {
        isCounterEffectActive = false;

        if (counterRig == null)
            return;

        if (counterRig.Ring != null)
            counterRig.Ring.enabled = false;

        foreach (LineRenderer layer in counterRingLayers)
            layer.enabled = false;

        if (counterRig.Flash != null)
            counterRig.Flash.enabled = false;

        if (counterRig.Halo != null)
            counterRig.Halo.enabled = false;

        if (counterRig.Beams != null)
            counterRig.Beams.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (counterRig.BeamGlow != null)
            counterRig.BeamGlow.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    #endregion


    #region Update

    private void UpdateCounterBlast(float deltaTime)
    {
        if (counterData == null || !isCounterEffectActive)
            return;

        counterAge += deltaTime;
        SetWorldPose(counterRig.transform, counterCenter, Vector3.one);

        bool ringDone = UpdateCounterRing(deltaTime);
        bool beamsDone = !IsAlive(counterRig.Beams) && !IsAlive(counterRig.BeamGlow);
        bool flashDone = UpdateCounterFlash();

        if (ringDone && beamsDone && flashDone)
            StopCounterBlast();
    }

    private static bool IsAlive(ParticleSystem system)
    {
        return system != null && system.IsAlive(true);
    }

    /// <summary>Draws the ring at the blast hitbox radius, then fades it at its last radius once the hitbox ends. Returns true when finished.</summary>
    private bool UpdateCounterRing(float deltaTime)
    {
        if (counterRig.Ring == null || !counterRig.Ring.enabled)
            return true;

        if (guard.IsCounterBlastActive)
        {
            counterRingRadius = guard.CounterBlastRadius;
            counterRingProgress = guard.CounterBlastProgress;
        }
        else
            counterRingFadeTimer += deltaTime;

        float fadeT = counterData.RingFadeTime > 0f
            ? Mathf.Clamp01(counterRingFadeTimer / counterData.RingFadeTime)
            : (guard.IsCounterBlastActive ? 0f : 1f);

        if (fadeT >= 1f)
        {
            counterRig.Ring.enabled = false;
            for (int i = 0; i < counterActiveRingLayers; i++)
                counterRingLayers[i].enabled = false;
            return true;
        }

        float width = counterData.RingWidth * Mathf.Lerp(1f, counterData.RingEndWidthScale, fadeT);
        Color color = counterData.Color.Evaluate(Mathf.Lerp(0f, 0.5f, counterRingProgress) + 0.5f * fadeT);
        float ringAlpha = color.a * (1f - fadeT);
        color.a = ringAlpha;
        DrawCounterRing(counterRig.Ring, counterRingRadius, width, color);

        for (int i = 0; i < counterActiveRingLayers; i++)
        {
            CounterRingLayer layer = counterData.RingLayers[i];
            Color layerColor = layer.Color;
            layerColor.a *= ringAlpha;
            DrawCounterRing(counterRingLayers[i], counterRingRadius * layer.RadiusScale, width * layer.WidthScale, layerColor);
        }

        return false;
    }

    private void DrawCounterRing(LineRenderer line, float radius, float width, Color color)
    {
        int segments = counterRingPositions.Length;
        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            counterRingPositions[i] = counterCenter + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
        }

        line.SetPositions(counterRingPositions);
        line.startColor = color;
        line.endColor = color;
        line.widthMultiplier = width;
    }

    /// <summary>Grows and fades the center flash and halo. Returns true when finished.</summary>
    private bool UpdateCounterFlash()
    {
        MeshRenderer flash = counterRig.Flash;
        MeshRenderer halo = counterRig.Halo;
        if ((flash == null || !flash.enabled) && (halo == null || !halo.enabled))
            return true;

        float t = counterData.FlashTime > 0f ? Mathf.Clamp01(counterAge / counterData.FlashTime) : 1f;
        if (t >= 1f)
        {
            if (flash != null) flash.enabled = false;
            if (halo != null) halo.enabled = false;
            return true;
        }

        float eased = 1f - (1f - t) * (1f - t);
        float size = counterData.FlashSize * Mathf.Lerp(0.5f, 1f, eased);

        if (flash != null)
        {
            flash.transform.localScale = Vector3.one * size;
            Color color = counterData.Color.Evaluate(t * 0.5f) * counterData.FlashColor;
            float brightness = counterData.FlashIntensity * counterData.Intensity;
            SetRendererBaseColor(flash, new Color(color.r * brightness, color.g * brightness, color.b * brightness, color.a * (1f - t)));
        }

        if (halo != null)
        {
            halo.transform.localScale = Vector3.one * (size * counterData.FlashHaloScale);
            Color haloColor = counterData.FlashHaloColor;
            haloColor.a *= 1f - t;
            SetRendererBaseColor(halo, haloColor);
        }

        return false;
    }

    #endregion
}
