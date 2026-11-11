using System;
using UnityEngine;

/// <summary>Tuning for the kill effect played at the view edge when an object is killed.</summary>
[CreateAssetMenu(fileName = "KillEffectSettings", menuName = "ScriptableObjects/Kill Effect Settings")]
public class KillEffectSettings : ScriptableObject
{
    [Header("Placement")]
    [Tooltip("Distance the effect is moved from the Z = 0 plane toward the camera, in world units.")]
    public float ZOffset = 2f;
    [Tooltip("Distance the origin is moved inside the view from the edge, in world units.")]
    public float EdgeInset = 0f;
    [Tooltip("Blend of the aim direction from 'toward the view center' (0) to 'straight in from the edge' (1).")]
    [Range(0f, 1f)] public float InwardBias = 0.6f;

    [Header("Global")]
    [Tooltip("Scales particle counts and brightness of every layer.")]
    [Range(0f, 2f)] public float Intensity = 1f;
    [Tooltip("Scales sizes, speeds and spawn radii of every layer.")]
    public float Scale = 1f;
    [Tooltip("Plays on unscaled time, so the effect runs during hitstop and slow motion.")]
    public bool UseUnscaledTime = true;

    [Header("Palette")]
    [Tooltip("Color over lifetime of the spirit share of streaks and shards.")]
    public Gradient SpiritGradient = CreateGradient(
        new[] { Color.white, new Color(0.55f, 0.75f, 1f), new Color(0.3f, 0.5f, 1f), new Color(0.55f, 0.3f, 1f) },
        new[] { 0f, 0.15f, 0.45f, 1f },
        new[] { 1f, 0.9f, 0f },
        new[] { 0f, 0.5f, 1f });
    [Tooltip("Color over lifetime of the fire share of streaks and shards.")]
    public Gradient FireGradient = CreateGradient(
        new[] { Color.white, new Color(1f, 0.95f, 0.6f), new Color(1f, 0.6f, 0.15f), new Color(0.8f, 0.15f, 0.05f) },
        new[] { 0f, 0.15f, 0.45f, 1f },
        new[] { 1f, 0.9f, 0f },
        new[] { 0f, 0.5f, 1f });
    [Tooltip("Color over lifetime of the core flash.")]
    public Gradient FlashGradient = CreateGradient(
        new[] { Color.white, new Color(1f, 0.85f, 0.55f), new Color(0.6f, 0.45f, 1f) },
        new[] { 0f, 0.35f, 1f },
        new[] { 1f, 0.8f, 0f },
        new[] { 0f, 0.3f, 1f });

    [Header("Layers")]
    public FlashLayerSettings Flash = new FlashLayerSettings();
    public StreakLayerSettings Beams = StreakLayerSettings.CreateBeams();
    public StreakLayerSettings Rays = StreakLayerSettings.CreateRays();
    public StreakLayerSettings Sparks = StreakLayerSettings.CreateSparks();
    public ShardLayerSettings Shards = new ShardLayerSettings();
    public ShockwaveLayerSettings Shockwave = new ShockwaveLayerSettings();


    public static Gradient CreateGradient(Color[] colors, float[] colorTimes, float[] alphas, float[] alphaTimes)
    {
        GradientColorKey[] colorKeys = new GradientColorKey[colors.Length];
        for (int i = 0; i < colors.Length; i++)
            colorKeys[i] = new GradientColorKey(colors[i], colorTimes[i]);

        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[alphas.Length];
        for (int i = 0; i < alphas.Length; i++)
            alphaKeys[i] = new GradientAlphaKey(alphas[i], alphaTimes[i]);

        Gradient gradient = new Gradient();
        gradient.SetKeys(colorKeys, alphaKeys);
        return gradient;
    }
}

[Serializable]
public class FlashLayerSettings
{
    public bool Enabled = true;

    [Tooltip("Peak size of the soft outer glow, in world units.")]
    public float Size = 7f;
    [Tooltip("Peak size of the white-hot inner glow, in world units.")]
    public float CoreSize = 2.5f;
    public float Lifetime = 0.28f;
    [Tooltip("Lifetime of the inner glow as a fraction of Lifetime.")]
    [Range(0.1f, 1f)] public float CoreLifetimeFraction = 0.6f;
    [Tooltip("Size over lifetime, multiplied by Size and CoreSize.")]
    public AnimationCurve SizeOverLifetime = new AnimationCurve(
        new Keyframe(0f, 0.35f, 0f, 8f),
        new Keyframe(0.15f, 1f, 0f, 0f),
        new Keyframe(1f, 1.2f, 0f, 0f));

    [Tooltip("HDR brightness multiplier of the generated material.")]
    public float Glow = 4f;
    [Tooltip("Replaces the generated material.")]
    public Material MaterialOverride;
}

[Serializable]
public class StreakLayerSettings
{
    public bool Enabled = true;

    [Tooltip("Particles emitted per kill.")]
    public int Count = 40;
    [Tooltip("Share of particles that use the fire gradient. The rest use the spirit gradient.")]
    [Range(0f, 1f)] public float FireMix = 0.5f;

    [Tooltip("Full angle of the cone the particles fire into, in degrees.")]
    [Range(0f, 360f)] public float ConeAngle = 50f;
    [Tooltip("Pulls particles toward the cone's center line. 0 = even spread.")]
    [Range(0f, 1f)] public float CenterBias = 0.5f;
    [Tooltip("Radius around the origin particles start in, in world units.")]
    public float SpawnRadius = 0.3f;

    [Tooltip("Start speed range, in world units per second.")]
    public Vector2 Speed = new Vector2(25f, 70f);
    [Tooltip("Speed multiplier at the edge of the cone. 1 = same as the center line.")]
    [Range(0f, 1f)] public float EdgeSpeed = 0.45f;
    [Tooltip("Velocity lost per second, proportional to speed.")]
    public float Drag = 4f;
    public float Gravity = 0f;
    [Tooltip("Turbulence strength. 0 disables noise.")]
    public float Noise = 0f;
    public float NoiseFrequency = 1.5f;

    [Tooltip("Lifetime range, in seconds.")]
    public Vector2 Lifetime = new Vector2(0.35f, 0.7f);
    [Tooltip("Width range, in world units.")]
    public Vector2 Width = new Vector2(0.15f, 0.45f);
    public AnimationCurve WidthOverLifetime = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
    [Tooltip("Base length as a multiple of width.")]
    public float Length = 2f;
    [Tooltip("Extra length per unit of speed.")]
    public float Stretch = 0.08f;

    [Tooltip("HDR brightness multiplier of the generated material.")]
    public float Glow = 3f;
    [Tooltip("Replaces the generated material.")]
    public Material MaterialOverride;

    public static StreakLayerSettings CreateBeams()
    {
        return new StreakLayerSettings
        {
            Count = 5,
            FireMix = 0.4f,
            ConeAngle = 10f,
            CenterBias = 0.3f,
            SpawnRadius = 0.4f,
            Speed = new Vector2(60f, 95f),
            EdgeSpeed = 0.8f,
            Drag = 6f,
            Lifetime = new Vector2(0.25f, 0.4f),
            Width = new Vector2(0.7f, 1.1f),
            Length = 3f,
            Stretch = 0.12f,
            Glow = 4f
        };
    }

    public static StreakLayerSettings CreateRays()
    {
        return new StreakLayerSettings();
    }

    public static StreakLayerSettings CreateSparks()
    {
        return new StreakLayerSettings
        {
            Count = 60,
            FireMix = 0.6f,
            ConeAngle = 110f,
            CenterBias = 0.2f,
            SpawnRadius = 0.5f,
            Speed = new Vector2(30f, 90f),
            EdgeSpeed = 0.7f,
            Drag = 3f,
            Gravity = 0.5f,
            Noise = 2f,
            NoiseFrequency = 2f,
            Lifetime = new Vector2(0.2f, 0.6f),
            Width = new Vector2(0.05f, 0.15f),
            Length = 1f,
            Stretch = 0.05f,
            Glow = 3f
        };
    }
}

[Serializable]
public class ShardLayerSettings
{
    public bool Enabled = true;

    [Tooltip("Shards emitted per kill.")]
    public int Count = 20;
    [Tooltip("Share of shards that use the fire gradient. The rest use the spirit gradient.")]
    [Range(0f, 1f)] public float FireMix = 0.5f;

    [Tooltip("Full angle of the cone the shards fire into, in degrees.")]
    [Range(0f, 360f)] public float ConeAngle = 120f;
    [Tooltip("Pulls shards toward the cone's center line. 0 = even spread.")]
    [Range(0f, 1f)] public float CenterBias = 0.1f;
    [Tooltip("Radius around the origin shards start in, in world units.")]
    public float SpawnRadius = 0.5f;

    [Tooltip("Start speed range, in world units per second.")]
    public Vector2 Speed = new Vector2(15f, 45f);
    [Tooltip("Speed multiplier at the edge of the cone. 1 = same as the center line.")]
    [Range(0f, 1f)] public float EdgeSpeed = 0.7f;
    [Tooltip("Velocity lost per second, proportional to speed.")]
    public float Drag = 3f;
    public float Gravity = 0.3f;
    [Tooltip("Spin speed range, in degrees per second. Direction is random.")]
    public Vector2 Spin = new Vector2(360f, 1080f);

    [Tooltip("Lifetime range, in seconds.")]
    public Vector2 Lifetime = new Vector2(0.4f, 0.9f);
    [Tooltip("Size range, in world units.")]
    public Vector2 Size = new Vector2(0.25f, 0.65f);
    public AnimationCurve SizeOverLifetime = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [Tooltip("HDR brightness multiplier of the generated material.")]
    public float Glow = 2.5f;
    [Tooltip("Replaces the generated material.")]
    public Material MaterialOverride;
}

[Serializable]
public class ShockwaveLayerSettings
{
    public bool Enabled = true;

    [Tooltip("Ring diameter at spawn, in world units.")]
    public float StartSize = 1f;
    [Tooltip("Ring diameter at the end of its lifetime, in world units.")]
    public float EndSize = 14f;
    [Tooltip("Ring band width as a fraction of the ring radius.")]
    [Range(0.02f, 0.5f)] public float Thickness = 0.12f;
    public float Lifetime = 0.4f;
    public Gradient Color = KillEffectSettings.CreateGradient(
        new[] { UnityEngine.Color.white, new UnityEngine.Color(0.7f, 0.8f, 1f), new UnityEngine.Color(1f, 0.65f, 0.3f) },
        new[] { 0f, 0.4f, 1f },
        new[] { 1f, 0.6f, 0f },
        new[] { 0f, 0.4f, 1f });

    [Tooltip("HDR brightness multiplier of the generated material.")]
    public float Glow = 2.5f;
    [Tooltip("Replaces the generated material.")]
    public Material MaterialOverride;
}
