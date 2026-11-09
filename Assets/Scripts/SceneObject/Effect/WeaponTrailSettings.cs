using System;
using UnityEngine;

[Serializable]
public class WeaponTrailSettings
{
    public bool Enabled = true;

    [Tooltip("Hitbox speed at which the trails start to show.")]
    public float MinSpeed = 4f;
    [Tooltip("Hitbox speed at which the trails reach full strength.")]
    public float FullSpeed = 22f;
    [Tooltip("Scales width, opacity and emission of every layer.")]
    [Range(0f, 1f)] public float Intensity = 1f;
    [Tooltip("Ribbon points added per meter of hitbox travel.")]
    public float PointsPerMeter = 10f;
    [Tooltip("Maximum ribbon points added per frame for each hitbox.")]
    public int MaxPointsPerFrame = 16;

    [Header("Layers")]
    public FireLayerSettings Fire = new FireLayerSettings();
    public ArcLayerSettings SpiritArc = ArcLayerSettings.CreateSpirit();
    public SparkLayerSettings Sparks = new SparkLayerSettings();


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
public class ArcLayerSettings
{
    public bool Enabled = true;

    [Tooltip("Ribbons laid along each hitbox.")]
    [Range(1, 10)] public int LanesPerHitBox = 6;
    [Tooltip("Point along the whole weapon where ribbons are strongest. 0 = handle end, 1 = tip end.")]
    [Range(0f, 1f)] public float Focus = 1f;
    [Tooltip("How quickly ribbons weaken away from the focus point.")]
    public float FocusSharpness = 2f;
    [Tooltip("Strength of the weakest ribbons, far from the focus point.")]
    [Range(0f, 1f)] public float MinLaneStrength = 0.15f;
    [Tooltip("Shifts ribbons toward the tip by up to half a ribbon spacing.")]
    [Range(0f, 1f)] public float LaneShift = 0f;

    public float Lifetime = 0.32f;
    [Tooltip("Ribbon width at full strength.")]
    public float Width = 0.4f;
    public AnimationCurve WidthOverLifetime = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [Tooltip("Wobble applied to ribbon points as they age.")]
    public float NoiseStrength = 0.15f;
    public float NoiseFrequency = 1.5f;

    [Tooltip("HDR brightness multiplier of the generated material.")]
    public float Glow = 2.5f;
    public Gradient Color;

    [Tooltip("Replaces the generated streak texture.")]
    public Texture2D TextureOverride;
    [Tooltip("Replaces the generated material.")]
    public Material MaterialOverride;

    public static ArcLayerSettings CreateSpirit()
    {
        return new ArcLayerSettings
        {
            LanesPerHitBox = 5,
            Focus = 0.55f,
            FocusSharpness = 2.5f,
            MinLaneStrength = 0.2f,
            LaneShift = 1f,
            Lifetime = 0.45f,
            Width = 0.3f,
            NoiseStrength = 0.25f,
            Glow = 2f,
            Color = WeaponTrailSettings.CreateGradient(
                new[] { new Color(0.75f, 0.85f, 1f), new Color(0.3f, 0.5f, 1f), new Color(0.55f, 0.3f, 1f), new Color(0.6f, 0.15f, 0.8f) },
                new[] { 0f, 0.25f, 0.6f, 1f },
                new[] { 0.9f, 0.6f, 0f },
                new[] { 0f, 0.6f, 1f })
        };
    }
}

[Serializable]
public class FireLayerSettings
{
    public bool Enabled = true;

    [Tooltip("Fraction of full width and emission used while the hitbox is active but moving below the minimum speed.")]
    [Range(0f, 1f)] public float MinRateFraction = 0.35f;
    [Tooltip("Additive blending for the spikes and edge line. Overlaps brighten toward white; off keeps their colors.")]
    public bool AdditiveBlend = false;

    [Header("Spikes")]
    [Tooltip("Distance between spikes along the hitbox's leading edge.")]
    public float SpikeSpacing = 0.12f;
    [Tooltip("Random shift of each spike along the edge, as a fraction of the spacing.")]
    [Range(0f, 1f)] public float SpikeJitter = 0.3f;
    [Tooltip("Width of each spike at its base.")]
    public Vector2 SpikeWidth = new Vector2(0.12f, 0.22f);
    [Tooltip("Seconds of swing each spike trails behind. Spike length = swing speed x this.")]
    public Vector2 SpikeLength = new Vector2(0.04f, 0.08f);
    [Tooltip("How much spike widths flicker.")]
    [Range(0f, 1f)] public float SpikeFlicker = 0.25f;
    [Tooltip("How far spike bases sit inward from the leading edge, as a fraction of the hitbox width.")]
    [Range(0f, 1f)] public float SpikeInset = 0.05f;
    public AnimationCurve SpikeTaper = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.5f, 0.55f), new Keyframe(1f, 0f));
    [Tooltip("HDR brightness multiplier of the generated spike material.")]
    public float SpikeGlow = 1.3f;
    [Tooltip("Color from spike base (left) to spike tip (right).")]
    public Gradient SpikeColor = WeaponTrailSettings.CreateGradient(
        new[] { new Color(1f, 0.45f, 0.08f), new Color(1f, 0.7f, 0.2f), new Color(1f, 0.92f, 0.45f) },
        new[] { 0f, 0.45f, 1f },
        new[] { 1f, 0.85f, 0f },
        new[] { 0f, 0.6f, 1f });

    [Header("Edge Line")]
    [Tooltip("Draws a ribbon along the hitbox's leading edge, joining the spike bases.")]
    public bool EdgeLineEnabled = true;
    public float EdgeLineWidth = 0.06f;
    [Range(2, 32)] public int EdgeLinePoints = 12;
    [Tooltip("HDR brightness multiplier of the generated edge line material.")]
    public float EdgeLineGlow = 1.5f;
    public Color EdgeLineColor = new Color(1f, 0.5f, 0.12f, 1f);

    [Header("Wisps")]
    [Tooltip("Wisp ribbons per second from each active hitbox at full strength.")]
    public float WispRate = 70f;
    [Tooltip("Pushes wisp emission toward the hitbox tip end.")]
    [Range(0f, 1f)] public float WispTipBias = 0.5f;
    [Tooltip("How far wisps scatter inward from the leading edge, as a fraction of the hitbox width.")]
    [Range(0f, 1f)] public float WispInset = 0.5f;
    public Vector2 WispLifetime = new Vector2(0.12f, 0.25f);
    public Vector2 WispWidth = new Vector2(0.03f, 0.08f);
    [Tooltip("Fraction of the hitbox velocity wisps keep. Lower values leave the wisps further behind the swing.")]
    public Vector2 WispFollow = new Vector2(0.3f, 0.6f);
    [Tooltip("Ribbon length as a fraction of the wisp lifetime.")]
    [Range(0f, 1f)] public float WispTrailLength = 0.6f;
    [Tooltip("Upward speed given to wisps.")]
    public float WispRise = 0.5f;
    public float WispNoiseStrength = 1.2f;
    public float WispNoiseFrequency = 3f;
    [Tooltip("HDR brightness multiplier of the generated wisp material.")]
    public float WispGlow = 2f;
    public Gradient WispColor = WeaponTrailSettings.CreateGradient(
        new[] { new Color(1f, 0.95f, 0.6f), new Color(1f, 0.6f, 0.15f), new Color(0.8f, 0.15f, 0.05f) },
        new[] { 0f, 0.4f, 1f },
        new[] { 1f, 0.7f, 0f },
        new[] { 0f, 0.5f, 1f });

    [Tooltip("Replaces the generated spike material.")]
    public Material SpikeMaterialOverride;
    [Tooltip("Replaces the generated edge line material.")]
    public Material EdgeLineMaterialOverride;
    [Tooltip("Replaces the generated wisp material.")]
    public Material WispMaterialOverride;
}

[Serializable]
public class SparkLayerSettings
{
    public bool Enabled = true;

    [Tooltip("Trail strength above which the weapon tip throws sparks.")]
    [Range(0f, 1f)] public float Threshold = 0.6f;
    [Tooltip("Sparks per second at full strength.")]
    public float Rate = 60f;
    [Tooltip("Fraction of the tip velocity carried by sparks.")]
    public Vector2 InheritVelocity = new Vector2(0.2f, 0.6f);
    [Tooltip("Random spray speed added to sparks.")]
    public Vector2 SpraySpeed = new Vector2(2f, 6f);
    public Vector2 Lifetime = new Vector2(0.15f, 0.35f);
    public Vector2 Size = new Vector2(0.03f, 0.07f);
    public float Gravity = 1f;
    [Tooltip("Stretch of sparks along their velocity.")]
    public float Stretch = 0.08f;

    [Tooltip("HDR brightness multiplier of the generated material.")]
    public float Glow = 3f;
    public Gradient Color = WeaponTrailSettings.CreateGradient(
        new[] { new Color(1f, 1f, 1f), new Color(1f, 0.85f, 0.4f), new Color(1f, 0.5f, 0.1f) },
        new[] { 0f, 0.4f, 1f },
        new[] { 1f, 0f },
        new[] { 0.6f, 1f });

    [Tooltip("Replaces the generated material.")]
    public Material MaterialOverride;
}
