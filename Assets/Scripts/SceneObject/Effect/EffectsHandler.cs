using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(HurtBoxHandler))]
[RequireComponent(typeof(Rigidbody))]
public class EffectsHandler : MonoBehaviour, IEffects
{
    IHurtBoxHandler hurtBoxHandler;
    private new Rigidbody rigidbody;

    [Header("Effects")]
    [SerializeField] private SceneObjectEffects effects;

    private ParticleSystem launchTrail;
    private List<ParticleSystem> hitEffects;

    [Range(0, 1f)]
    [SerializeField] private float velocityScale = 1f;
    [SerializeField] private float minRate = 10;
    [SerializeField] private float maxRate = 100;
    [SerializeField] private float minLifeTimeStart = 0.15f;
    [SerializeField] private float maxLifeTimeStart = 0.5f;
    [SerializeField] private float maxLaunchSpeed = 20;
    [Tooltip("Minimum rigidbody speed required for the launch trail to emit.")]
    [SerializeField] private float minTrailVelocity = 15f;

    private void Awake()
    {
        hurtBoxHandler = GetComponent<IHurtBoxHandler>();
        rigidbody = GetComponent<Rigidbody>();

        if (effects == null)
            Debug.LogError("Effects not setup for sceneObject", gameObject);
        else
            SetupParticleEffects();

        RegisterToEvents();
    }

    private void RegisterToEvents()
    {
        hurtBoxHandler.OnHitEvent += OnHit;

        if (hurtBoxHandler is ISOHurtBoxHandler soHurtBoxHandler)
            soHurtBoxHandler.HitStunStateChangedEvent += OnHitStunStateChanged;
    }

    private void OnDestroy()
    {
        UnregisterFromEvents();
        CleanUpParticles();
    }

    private void UnregisterFromEvents()
    {
        hurtBoxHandler.OnHitEvent -= OnHit;

        if (hurtBoxHandler is ISOHurtBoxHandler soHurtBoxHandler)
            soHurtBoxHandler.HitStunStateChangedEvent -= OnHitStunStateChanged;
    }


    #region  Particles

    private void SetupParticleEffects()
    {
        if (effects == null) return;

        GameObject effectsGO = new GameObject();
        effectsGO.name = "Effects";
        effectsGO.transform.SetParent(transform);
        effectsGO.transform.localPosition = Vector3.zero;
        effectsGO.transform.localRotation = Quaternion.identity;
        effectsGO.transform.localScale = Vector3.one;

        if (effects.LaunchEffect != null)
            launchTrail = Instantiate(effects.LaunchEffect, effectsGO.transform);

        if (effects.HitEffects != null && effects.HitEffects.Count > 0)
        {
            hitEffects = new List<ParticleSystem>();
            foreach (ParticleSystem particle in effects.HitEffects)
                hitEffects.Add(Instantiate(particle, effectsGO.transform));
        }
    }

    private void CleanUpParticles()
    {
        if (launchTrail != null)
            Destroy(launchTrail);

        if (hitEffects != null)
        {
            foreach (ParticleSystem particle in hitEffects)
                Destroy(particle);

            hitEffects.Clear();
        }
    }

    #endregion


    private void OnHit(KnockBackHitData hitData)
    {
        if (hitEffects == null || hitEffects.Count <= hitData.EffectIndex) return;
        
        ParticleSystem particle = hitEffects[hitData.EffectIndex];
        particle.transform.position = hitData.HitPoint;
        particle.Play();
    }

    private void OnHitStunStateChanged(HitStunState state)
    {
        if (launchTrail == null) return;

        if (state == HitStunState.Launch)
            launchTrail.Play();
        else
            launchTrail.Stop();
    }


    private void Update()
    {
        if (launchTrail != null && launchTrail.isPlaying)
            UpdateLaunchTrail();
    }

    private void UpdateLaunchTrail()
    {
        Vector3 velocity = rigidbody.linearVelocity;
        float speed = velocity.magnitude;

        var emission = launchTrail.emission;

        if (speed < minTrailVelocity)
        {
            emission.rateOverTime = 0;
            return;
        }

        float normalizedSpeed = Mathf.Clamp01(speed / maxLaunchSpeed);

        var velocityOverTime = launchTrail.velocityOverLifetime;
        velocityOverTime.x = -velocity.x * velocityScale;
        velocityOverTime.y = -velocity.y * velocityScale;

        emission.rateOverTime = Mathf.Lerp(minRate, maxRate, normalizedSpeed);

        var main = launchTrail.main;
        main.startLifetime = Mathf.Lerp(minLifeTimeStart, maxLifeTimeStart, normalizedSpeed);

        launchTrail.transform.rotation = Quaternion.LookRotation(Vector3.forward, velocity);
    }
}