using System;
using UnityEngine;

[CreateAssetMenu(fileName = "CounterBlast", menuName = "ScriptableObjects/SceneObject/Effects/Counter Blast")]
public class CounterBlastEffectData : EffectData
{
    [Tooltip("Counter blast prefab. Beam count, speed, length and color are set on its Beams and Beam Glow particle systems.")]
    public CounterBlastRig Prefab;

    [Header("Color")]
    [Tooltip("Color over the ring and flash lifetimes. Each evaluates it from 0 to 1.")]
    [GradientUsage(true)]
    public Gradient Color = DefaultColor();
    [Tooltip("Brightness multiplier on the ring, beam cores and flash.")]
    public float Intensity = 2.5f;

    [Header("Ring")]
    [Tooltip("Ring line width in world units.")]
    public float RingWidth = 0.14f;
    [Tooltip("Ring width multiplier reached at the end of the fade.")]
    public float RingEndWidthScale = 0.3f;
    [Tooltip("Seconds the ring fades after the blast hitbox ends. The ring radius follows the blast hitbox.")]
    public float RingFadeTime = 0.1f;
    [Tooltip("Points around the ring.")]
    public int RingSegments = 64;
    [Tooltip("Colored rings drawn with the main ring. Alpha blended. They fade with the main ring.")]
    public CounterRingLayer[] RingLayers = DefaultRingLayers();

    [Header("Core Flash")]
    [Tooltip("Flash diameter in world units at its peak.")]
    public float FlashSize = 1.6f;
    [Tooltip("Seconds the flash lasts.")]
    public float FlashTime = 0.08f;
    [Tooltip("Brightness multiplier on the flash.")]
    public float FlashIntensity = 3f;
    [Tooltip("Tint multiplied into the flash core.")]
    public UnityEngine.Color FlashColor = new UnityEngine.Color(1f, 0.9f, 0.55f, 1f);
    [Tooltip("Color of the halo behind the flash. Alpha blended; alpha sets its peak opacity.")]
    public UnityEngine.Color FlashHaloColor = new UnityEngine.Color(0.2f, 0.45f, 1f, 0.7f);
    [Tooltip("Halo diameter as a multiple of the flash diameter.")]
    public float FlashHaloScale = 2f;

    private static CounterRingLayer[] DefaultRingLayers()
    {
        return new[]
        {
            new CounterRingLayer { Color = new UnityEngine.Color(1f, 0.8f, 0.2f, 0.9f), RadiusScale = 0.94f, WidthScale = 1.6f },
            new CounterRingLayer { Color = new UnityEngine.Color(0.25f, 0.55f, 1f, 0.85f), RadiusScale = 1.04f, WidthScale = 2f },
            new CounterRingLayer { Color = new UnityEngine.Color(0.06f, 0.16f, 0.62f, 0.45f), RadiusScale = 1.1f, WidthScale = 4f }
        };
    }

    private static Gradient DefaultColor()
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new UnityEngine.Color(1f, 1f, 1f), 0f),
                new GradientColorKey(new UnityEngine.Color(0.5f, 0.95f, 1f), 0.3f),
                new GradientColorKey(new UnityEngine.Color(0.06f, 0.16f, 0.62f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.85f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            });
        return gradient;
    }
}


[Serializable]
public class CounterRingLayer
{
    [Tooltip("Ring color. Alpha sets its peak opacity.")]
    public Color Color = Color.white;
    [Tooltip("Radius as a multiple of the blast radius.")]
    public float RadiusScale = 1f;
    [Tooltip("Width as a multiple of the main ring width.")]
    public float WidthScale = 1f;
}
