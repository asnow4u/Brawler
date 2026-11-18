using UnityEngine;

public partial class EffectsHandler
{
    private const string LaunchGroupName = "Launch";

    private LaunchEffectData launchData;
    private ParticleSystem launchTrail;


    /// <summary>Creates the launch trail under Effects/Launch when there is launch data, an ISOHurtBoxHandler and a Rigidbody.</summary>
    private void SetupLaunch()
    {
        LaunchEffectData data = effectsData.Launch;
        if (data == null || data.Prefab == null || soHurtBoxHandler == null || rigidbody == null)
            return;

        launchData = data;
        launchTrail = InstantiateEffect(data.Prefab, GetEffectGroup(LaunchGroupName));

        soHurtBoxHandler.HitStunStateChangedEvent += OnHitStunStateChanged;

        if (soHurtBoxHandler.CurHitStunState == HitStunState.Launch)
            launchTrail.Play();
    }

    private void TeardownLaunch()
    {
        if (launchData == null)
            return;

        soHurtBoxHandler.HitStunStateChangedEvent -= OnHitStunStateChanged;
        DestroyInstance(launchTrail);

        launchTrail = null;
        launchData = null;
    }

    private void OnHitStunStateChanged(HitStunState state)
    {
        if (state == HitStunState.Launch)
            launchTrail.Play();
        else
            launchTrail.Stop();
    }

    /// <summary>Scales the launch trail emission and lifetime with the rigidbody speed and points it against the velocity.</summary>
    private void UpdateLaunch()
    {
        if (launchTrail == null || !launchTrail.isPlaying)
            return;

        Vector3 velocity = rigidbody.linearVelocity;
        float speed = velocity.magnitude;

        ParticleSystem.EmissionModule emission = launchTrail.emission;

        if (speed < launchData.MinTrailVelocity)
        {
            emission.rateOverTime = 0;
            return;
        }

        float normalizedSpeed = Mathf.Clamp01(speed / launchData.MaxLaunchSpeed);

        ParticleSystem.VelocityOverLifetimeModule velocityOverTime = launchTrail.velocityOverLifetime;
        velocityOverTime.x = -velocity.x * launchData.VelocityScale;
        velocityOverTime.y = -velocity.y * launchData.VelocityScale;

        emission.rateOverTime = Mathf.Lerp(launchData.MinRate, launchData.MaxRate, normalizedSpeed);

        ParticleSystem.MainModule main = launchTrail.main;
        main.startLifetime = Mathf.Lerp(launchData.MinLifetimeStart, launchData.MaxLifetimeStart, normalizedSpeed);

        launchTrail.transform.rotation = Quaternion.LookRotation(Vector3.forward, velocity);
    }
}
