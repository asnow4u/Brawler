using UnityEngine;

/// <summary>References to the parts of a counter blast prefab.</summary>
public class CounterBlastRig : MonoBehaviour
{
    [Tooltip("Main ring. World-space LineRenderer drawn at the blast hitbox radius.")]
    public LineRenderer Ring;
    [Tooltip("Cloned once per ring layer. Kept disabled.")]
    public LineRenderer RingLayerTemplate;
    [Tooltip("Bright beam cores. Played with the same random seed as Beam Glow.")]
    public ParticleSystem Beams;
    [Tooltip("Colored glow behind the beams. Played with the same random seed as Beams.")]
    public ParticleSystem BeamGlow;
    [Tooltip("Additive core flash quad.")]
    public MeshRenderer Flash;
    [Tooltip("Alpha blended halo quad behind the flash.")]
    public MeshRenderer Halo;
}
