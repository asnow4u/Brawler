using System;
using UnityEngine;

/// <summary>A cooldown with a random extra added each time it is triggered.</summary>
[Serializable]
internal class Cooldown
{
    [Tooltip("Base time between uses, in seconds.")]
    [SerializeField] private float duration;
    [Tooltip("Random extra time added to each cooldown, in seconds.")]
    [SerializeField] private float randomness;

    private float readyTime;

    public bool IsReady => Time.time >= readyTime;


    public Cooldown() { }

    public Cooldown(float duration, float randomness)
    {
        this.duration = duration;
        this.randomness = randomness;
    }

    /// <summary>Starts the cooldown.</summary>
    public void Trigger()
    {
        readyTime = Time.time + duration + UnityEngine.Random.Range(0f, randomness);
    }

    /// <summary>Delays the first use by a random amount up to the randomness.</summary>
    public void Stagger()
    {
        readyTime = Time.time + UnityEngine.Random.Range(0f, randomness);
    }
}
