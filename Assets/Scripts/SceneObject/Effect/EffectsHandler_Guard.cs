using UnityEngine;

public partial class EffectsHandler
{
    private const string GuardGroupName = "Guard";

    private static readonly int FadeId = Shader.PropertyToID("_Fade");

    private enum ShieldPhase { Hidden, Up, FadingOut, Popping }

    private GuardShieldEffectData shieldData;
    private MeshRenderer shieldRenderer;
    private Transform shieldTransform;
    private ShieldPhase shieldPhase = ShieldPhase.Hidden;
    private GuardState shieldLastGuardState = GuardState.Null;
    private Vector3 shieldBaseScale = Vector3.one;
    private Vector3 shieldScale = Vector3.one;
    private Vector3 shieldFollowVelocity;
    private float shieldAlpha;
    private float shieldPopTimer;
    private float shieldPopStartAlpha;


    #region Setup

    /// <summary>Creates the shield under Effects/Guard when there is shield data and an IGuard.</summary>
    private void SetupGuardShield()
    {
        GuardShieldEffectData data = effectsData.GuardShield;
        if (data == null || data.Prefab == null || guard == null || sceneObject == null)
            return;

        shieldData = data;
        shieldRenderer = InstantiateEffect(data.Prefab, GetEffectGroup(GuardGroupName));
        shieldTransform = shieldRenderer.transform;
        shieldRenderer.enabled = false;
        shieldPhase = ShieldPhase.Hidden;
        shieldAlpha = 0f;
        shieldLastGuardState = guard.CurGuardState;

        guard.GuardStateChangedEvent += OnGuardStateChangedShield;
    }

    private void TeardownGuardShield()
    {
        if (shieldData == null)
            return;

        guard.GuardStateChangedEvent -= OnGuardStateChangedShield;
        DestroyInstance(shieldRenderer);

        shieldData = null;
        shieldRenderer = null;
        shieldTransform = null;
        shieldPhase = ShieldPhase.Hidden;
        shieldAlpha = 0f;
    }

    /// <summary>Hides the shield immediately.</summary>
    private void HideGuardShield()
    {
        shieldPhase = ShieldPhase.Hidden;
        shieldAlpha = 0f;

        if (shieldRenderer != null)
            shieldRenderer.enabled = false;
    }

    #endregion


    #region State

    /// <summary>Fades the shield in on Raise, pops it on Counter, and fades it out when the guard ends otherwise.</summary>
    private void OnGuardStateChangedShield(GuardState state)
    {
        GuardState previous = shieldLastGuardState;
        shieldLastGuardState = state;

        switch (state)
        {
            case GuardState.Raise:
                ShowShield();
                break;

            case GuardState.Drop:
                FadeOutShield();
                break;

            case GuardState.Counter:
                PopShield();
                break;

            case GuardState.Null:
                if (previous != GuardState.Counter)
                    FadeOutShield();
                break;
        }
    }

    private void ShowShield()
    {
        Bounds bounds = sceneObject.Bounds;
        float diameter = Mathf.Max(bounds.size.x, bounds.size.y) + shieldData.Padding * 2f;
        shieldBaseScale = shieldData.OvalProportions * diameter;
        shieldScale = shieldBaseScale;

        if (shieldPhase == ShieldPhase.Hidden)
        {
            shieldAlpha = 0f;
            shieldFollowVelocity = Vector3.zero;
            SetWorldPose(shieldTransform, bounds.center, shieldScale);
        }

        shieldPhase = ShieldPhase.Up;
        SetShieldFade(0f);
        shieldRenderer.enabled = true;
    }

    private void FadeOutShield()
    {
        if (shieldPhase == ShieldPhase.Up)
            shieldPhase = ShieldPhase.FadingOut;
    }

    private void PopShield()
    {
        if (shieldPhase == ShieldPhase.Hidden)
            return;

        shieldPhase = ShieldPhase.Popping;
        shieldPopTimer = 0f;
        shieldPopStartAlpha = shieldAlpha;
    }

    #endregion


    #region Update

    private void UpdateGuardShield(float deltaTime)
    {
        if (shieldData == null || shieldPhase == ShieldPhase.Hidden)
            return;

        float flicker = 1f;
        Vector3 position = shieldTransform.position;

        switch (shieldPhase)
        {
            case ShieldPhase.Up:
                shieldAlpha = MoveAlpha(shieldAlpha, 1f, shieldData.FadeInTime, deltaTime);
                flicker = GetShieldFlicker();
                break;

            case ShieldPhase.FadingOut:
                shieldAlpha = MoveAlpha(shieldAlpha, 0f, shieldData.FadeOutTime, deltaTime);
                break;

            case ShieldPhase.Popping:
                shieldPopTimer += deltaTime;
                float t = shieldData.PopTime > 0f ? Mathf.Clamp01(shieldPopTimer / shieldData.PopTime) : 1f;
                float eased = 1f - (1f - t) * (1f - t);
                shieldScale = shieldBaseScale * Mathf.Lerp(1f, shieldData.PopScale, eased);
                shieldAlpha = shieldPopStartAlpha * (1f - t);
                break;
        }

        if (shieldPhase != ShieldPhase.Popping)
        {
            position = Vector3.SmoothDamp(position, sceneObject.Bounds.center, ref shieldFollowVelocity,
                Mathf.Max(0f, shieldData.FollowSmoothTime), Mathf.Infinity, deltaTime);
        }

        SetWorldPose(shieldTransform, position, shieldScale);
        SetShieldFade(shieldAlpha * flicker);

        if (shieldPhase != ShieldPhase.Up && shieldAlpha <= 0f)
            HideGuardShield();
    }

    /// <summary>Returns the flicker opacity multiplier for the last FlickerDuration seconds of the guard.</summary>
    private float GetShieldFlicker()
    {
        GuardState state = guard.CurGuardState;
        bool isGuardUp = state == GuardState.Raise || state == GuardState.Hold || state == GuardState.Blockstun;
        if (!isGuardUp || shieldData.FlickerDuration <= 0f)
            return 1f;

        float flickerStart = guard.GuardDuration - shieldData.FlickerDuration;
        if (guard.GuardTimer < flickerStart)
            return 1f;

        float phase = Mathf.Repeat((guard.GuardTimer - flickerStart) * shieldData.FlickerRate, 1f);
        return phase < 0.5f ? shieldData.FlickerMinAlpha : 1f;
    }

    private void SetShieldFade(float fade)
    {
        shieldRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(FadeId, fade);
        shieldRenderer.SetPropertyBlock(propertyBlock);
    }

    private static float MoveAlpha(float current, float target, float time, float deltaTime)
    {
        if (time <= 0f)
            return target;

        return Mathf.MoveTowards(current, target, deltaTime / time);
    }

    #endregion
}
