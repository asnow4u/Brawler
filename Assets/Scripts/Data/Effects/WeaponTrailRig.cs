using UnityEngine;

/// <summary>References to the parts of a weapon trail prefab.</summary>
public class WeaponTrailRig : MonoBehaviour
{
    [Tooltip("Fire wisp ribbons shared by every hitbox.")]
    public ParticleSystem Wisps;
    [Tooltip("Sparks shared by every hitbox.")]
    public ParticleSystem Sparks;
    [Tooltip("Cloned per spirit lane and trail bank. Kept inactive.")]
    public ParticleSystem SpiritLaneTemplate;
    [Tooltip("Cloned per hitbox for the fire spikes. Kept inactive.")]
    public ParticleSystem SpikeTemplate;
    [Tooltip("Cloned per hitbox for the fire edge line. Kept inactive.")]
    public LineRenderer EdgeLineTemplate;
}
