using UnityEngine;

[CreateAssetMenu(fileName = "Launch", menuName = "ScriptableObjects/SceneObject/Effects/Launch")]
public class LaunchEffectData : EffectData
{
    [Tooltip("Particle prefab played while in the Launch hitstun state.")]
    public ParticleSystem Prefab;

    [Tooltip("Fraction of the rigidbody velocity applied, reversed, to the particles.")]
    [Range(0f, 1f)] public float VelocityScale = 1f;
    [Tooltip("Emission rate at Min Trail Velocity.")]
    public float MinRate = 50f;
    [Tooltip("Emission rate at Max Launch Speed.")]
    public float MaxRate = 200f;
    [Tooltip("Particle start lifetime at Min Trail Velocity.")]
    public float MinLifetimeStart = 0.15f;
    [Tooltip("Particle start lifetime at Max Launch Speed.")]
    public float MaxLifetimeStart = 0.5f;
    [Tooltip("Rigidbody speed at which rate and lifetime reach their maximum.")]
    public float MaxLaunchSpeed = 20f;
    [Tooltip("Minimum rigidbody speed required for the launch trail to emit.")]
    public float MinTrailVelocity = 15f;
}
