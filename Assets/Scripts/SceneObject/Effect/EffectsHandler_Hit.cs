using System.Collections.Generic;
using UnityEngine;

public partial class EffectsHandler
{
    private const string HitGroupName = "Hit";

    private HitEffectData hitData;
    private readonly List<ParticleSystem> hitEffects = new List<ParticleSystem>();


    /// <summary>Creates the hit particles under Effects/Hit when there is hit data and an IHurtBoxHandler.</summary>
    private void SetupHit()
    {
        HitEffectData data = effectsData.Hit;
        if (data == null || data.HitEffects == null || data.HitEffects.Count == 0 || hurtBoxHandler == null)
            return;

        hitData = data;
        Transform group = GetEffectGroup(HitGroupName);

        foreach (ParticleSystem prefab in data.HitEffects)
        {
            if (prefab != null)
                hitEffects.Add(InstantiateEffect(prefab, group));
        }

        hurtBoxHandler.OnHitEvent += OnHit;
    }

    private void TeardownHit()
    {
        if (hitData == null)
            return;

        hurtBoxHandler.OnHitEvent -= OnHit;

        foreach (ParticleSystem particle in hitEffects)
            DestroyInstance(particle);

        hitEffects.Clear();
        hitData = null;
    }

    /// <summary>Plays the first hit particle at the hit point.</summary>
    private void OnHit(KnockBackHitData hitData)
    {
        if (hitEffects.Count == 0)
            return;

        ParticleSystem particle = hitEffects[0];
        particle.transform.position = hitData.HitPoint;
        particle.Play();
    }
}
