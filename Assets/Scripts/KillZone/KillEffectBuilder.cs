using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Builds one kill effect from a KillEffectSettings asset: a core flash, beams, rays, sparks, shards and a
/// shockwave ring, each a Particle System that emits a single burst. Owns the generated textures.
/// </summary>
public class KillEffectBuilder
{
    private const string ParticleShaderName = "Universal Render Pipeline/Particles/Unlit";
    private const float EndPadding = 0.1f;

    private readonly Transform parent;
    private readonly List<Texture2D> textures = new List<Texture2D>();

    private Texture2D glowTexture;
    private Texture2D streakTexture;
    private Texture2D shardTexture;
    private Texture2D ringTexture;
    private float ringTextureThickness = -1f;

    public KillEffectBuilder(Transform parent)
    {
        this.parent = parent;
    }


    #region Play

    /// <summary>Builds and plays the effect at origin, aimed along direction on the XY plane.</summary>
    public KillEffectInstance Play(KillEffectSettings settings, Vector3 origin, Vector2 direction)
    {
        EnsureTextures(settings);

        GameObject root = new GameObject("KillEffect");
        root.transform.SetParent(parent, false);
        root.transform.position = origin;

        KillEffectInstance instance = new KillEffectInstance(root, settings.UseUnscaledTime);

        float duration = 0f;
        duration = Mathf.Max(duration, PlayFlash(instance, settings, origin));
        duration = Mathf.Max(duration, PlayShockwave(instance, settings, origin));
        duration = Mathf.Max(duration, PlayStreaks(instance, "Beams", settings.Beams, settings, origin, direction));
        duration = Mathf.Max(duration, PlayStreaks(instance, "Rays", settings.Rays, settings, origin, direction));
        duration = Mathf.Max(duration, PlayStreaks(instance, "Sparks", settings.Sparks, settings, origin, direction));
        duration = Mathf.Max(duration, PlayShards(instance, settings, origin, direction));

        instance.EndTime = instance.Now + duration + EndPadding;
        return instance;
    }

    private float PlayFlash(KillEffectInstance instance, KillEffectSettings settings, Vector3 origin)
    {
        FlashLayerSettings layer = settings.Flash;
        if (!layer.Enabled || layer.Lifetime <= 0f)
            return 0f;

        Material material = GetMaterial(instance, "KillFlash", layer.MaterialOverride, glowTexture, layer.Glow * settings.Intensity);
        ParticleSystem system = CreateSystem(instance, "Flash", material, ParticleSystemRenderMode.Billboard, settings.UseUnscaledTime, 2);

        SetColorOverLifetime(system, settings.FlashGradient);
        SetSizeOverLifetime(system, layer.SizeOverLifetime);

        ParticleSystem.EmitParams emit = CreateEmitParams();
        emit.position = origin;
        emit.velocity = Vector3.zero;

        emit.startSize = layer.Size * settings.Scale;
        emit.startLifetime = layer.Lifetime;
        system.Emit(emit, 1);

        emit.startSize = layer.CoreSize * settings.Scale;
        emit.startLifetime = layer.Lifetime * layer.CoreLifetimeFraction;
        system.Emit(emit, 1);

        return layer.Lifetime;
    }

    private float PlayShockwave(KillEffectInstance instance, KillEffectSettings settings, Vector3 origin)
    {
        ShockwaveLayerSettings layer = settings.Shockwave;
        if (!layer.Enabled || layer.Lifetime <= 0f || layer.EndSize <= 0f)
            return 0f;

        Material material = GetMaterial(instance, "KillShockwave", layer.MaterialOverride, ringTexture, layer.Glow * settings.Intensity);
        ParticleSystem system = CreateSystem(instance, "Shockwave", material, ParticleSystemRenderMode.Billboard, settings.UseUnscaledTime, 1);

        SetColorOverLifetime(system, layer.Color);

        float startRatio = Mathf.Clamp01(layer.StartSize / layer.EndSize);
        AnimationCurve easeOut = new AnimationCurve(
            new Keyframe(0f, startRatio, 0f, 2f * (1f - startRatio)),
            new Keyframe(1f, 1f, 0f, 0f));
        SetSizeOverLifetime(system, easeOut);

        ParticleSystem.EmitParams emit = CreateEmitParams();
        emit.position = origin;
        emit.velocity = Vector3.zero;
        emit.startSize = layer.EndSize * settings.Scale;
        emit.startLifetime = layer.Lifetime;
        system.Emit(emit, 1);

        return layer.Lifetime;
    }

    private float PlayStreaks(KillEffectInstance instance, string name, StreakLayerSettings layer, KillEffectSettings settings,
        Vector3 origin, Vector2 direction)
    {
        if (!layer.Enabled)
            return 0f;

        int total = Mathf.Max(0, Mathf.RoundToInt(layer.Count * settings.Intensity));
        if (total == 0)
            return 0f;

        int fireCount = Mathf.RoundToInt(total * layer.FireMix);
        Material material = GetMaterial(instance, $"Kill{name}", layer.MaterialOverride, streakTexture, layer.Glow * settings.Intensity);

        EmitStreaks(CreateStreakSystem(instance, $"{name}_Spirit", material, layer, settings, settings.SpiritGradient, total - fireCount),
            total - fireCount, layer, settings.Scale, origin, direction);
        EmitStreaks(CreateStreakSystem(instance, $"{name}_Fire", material, layer, settings, settings.FireGradient, fireCount),
            fireCount, layer, settings.Scale, origin, direction);

        return Mathf.Max(layer.Lifetime.x, layer.Lifetime.y);
    }

    private float PlayShards(KillEffectInstance instance, KillEffectSettings settings, Vector3 origin, Vector2 direction)
    {
        ShardLayerSettings layer = settings.Shards;
        if (!layer.Enabled)
            return 0f;

        int total = Mathf.Max(0, Mathf.RoundToInt(layer.Count * settings.Intensity));
        if (total == 0)
            return 0f;

        int fireCount = Mathf.RoundToInt(total * layer.FireMix);
        Material material = GetMaterial(instance, "KillShards", layer.MaterialOverride, shardTexture, layer.Glow * settings.Intensity);

        EmitShards(CreateShardSystem(instance, "Shards_Spirit", material, layer, settings, settings.SpiritGradient, total - fireCount),
            total - fireCount, layer, settings.Scale, origin, direction);
        EmitShards(CreateShardSystem(instance, "Shards_Fire", material, layer, settings, settings.FireGradient, fireCount),
            fireCount, layer, settings.Scale, origin, direction);

        return Mathf.Max(layer.Lifetime.x, layer.Lifetime.y);
    }

    #endregion


    #region Emission

    private static void EmitStreaks(ParticleSystem system, int count, StreakLayerSettings layer, float scale, Vector3 origin, Vector2 direction)
    {
        if (system == null)
            return;

        ParticleSystem.EmitParams emit = CreateEmitParams();

        for (int i = 0; i < count; i++)
        {
            float speed = Random.Range(layer.Speed.x, layer.Speed.y) * scale;

            emit.position = origin + RandomInPlane(layer.SpawnRadius * scale);
            emit.velocity = ConeVelocity(direction, layer.ConeAngle, layer.CenterBias, speed, layer.EdgeSpeed);
            emit.startSize = Random.Range(layer.Width.x, layer.Width.y) * scale;
            emit.startLifetime = Random.Range(layer.Lifetime.x, layer.Lifetime.y);

            system.Emit(emit, 1);
        }
    }

    private static void EmitShards(ParticleSystem system, int count, ShardLayerSettings layer, float scale, Vector3 origin, Vector2 direction)
    {
        if (system == null)
            return;

        ParticleSystem.EmitParams emit = CreateEmitParams();

        for (int i = 0; i < count; i++)
        {
            float speed = Random.Range(layer.Speed.x, layer.Speed.y) * scale;
            float spin = Random.Range(layer.Spin.x, layer.Spin.y) * (Random.value < 0.5f ? -1f : 1f);

            emit.position = origin + RandomInPlane(layer.SpawnRadius * scale);
            emit.velocity = ConeVelocity(direction, layer.ConeAngle, layer.CenterBias, speed, layer.EdgeSpeed);
            emit.startSize = Random.Range(layer.Size.x, layer.Size.y) * scale;
            emit.startLifetime = Random.Range(layer.Lifetime.x, layer.Lifetime.y);
            emit.rotation = Random.Range(0f, 360f);
            emit.angularVelocity = spin;

            system.Emit(emit, 1);
        }
    }

    /// <summary>Velocity inside a cone around direction on the XY plane. Speed falls toward EdgeSpeed at the cone edge.</summary>
    private static Vector3 ConeVelocity(Vector2 direction, float coneAngle, float centerBias, float speed, float edgeSpeed)
    {
        float t = Mathf.Pow(Random.value, 1f + centerBias * 3f);
        float angle = t * coneAngle * 0.5f * (Random.value < 0.5f ? -1f : 1f) * Mathf.Deg2Rad;

        float cos = Mathf.Cos(angle);
        float sin = Mathf.Sin(angle);
        Vector2 rotated = new Vector2(direction.x * cos - direction.y * sin, direction.x * sin + direction.y * cos);

        return new Vector3(rotated.x, rotated.y, 0f) * (speed * Mathf.Lerp(1f, edgeSpeed, t));
    }

    private static Vector3 RandomInPlane(float radius)
    {
        Vector2 point = Random.insideUnitCircle * radius;
        return new Vector3(point.x, point.y, 0f);
    }

    private static ParticleSystem.EmitParams CreateEmitParams()
    {
        return new ParticleSystem.EmitParams
        {
            applyShapeToPosition = false,
            startColor = Color.white
        };
    }

    #endregion


    #region Particle Systems

    private ParticleSystem CreateSystem(KillEffectInstance instance, string name, Material material,
        ParticleSystemRenderMode renderMode, bool unscaledTime, int maxParticles)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(instance.Root.transform, false);

        ParticleSystem system = go.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = system.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 1f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.useUnscaledTime = unscaledTime;
        main.startSpeed = 0f;
        main.gravityModifier = 0f;
        main.maxParticles = Mathf.Max(1, maxParticles);

        ParticleSystem.EmissionModule emission = system.emission;
        emission.enabled = false;

        ParticleSystem.ShapeModule shape = system.shape;
        shape.enabled = false;

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = renderMode;
        renderer.material = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        system.Play();
        return system;
    }

    private ParticleSystem CreateStreakSystem(KillEffectInstance instance, string name, Material material,
        StreakLayerSettings layer, KillEffectSettings settings, Gradient gradient, int count)
    {
        if (count <= 0)
            return null;

        ParticleSystem system = CreateSystem(instance, name, material, ParticleSystemRenderMode.Stretch, settings.UseUnscaledTime, count);

        ParticleSystem.MainModule main = system.main;
        main.gravityModifier = layer.Gravity;

        SetColorOverLifetime(system, gradient);
        SetSizeOverLifetime(system, layer.WidthOverLifetime);
        SetDrag(system, layer.Drag);

        ParticleSystem.NoiseModule noise = system.noise;
        noise.enabled = layer.Noise > 0f;
        noise.strength = layer.Noise * settings.Scale;
        noise.frequency = layer.NoiseFrequency;
        noise.scrollSpeed = 1f;
        noise.damping = true;
        noise.quality = ParticleSystemNoiseQuality.Medium;

        ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
        renderer.lengthScale = layer.Length;
        renderer.velocityScale = layer.Stretch;
        renderer.cameraVelocityScale = 0f;

        return system;
    }

    private ParticleSystem CreateShardSystem(KillEffectInstance instance, string name, Material material,
        ShardLayerSettings layer, KillEffectSettings settings, Gradient gradient, int count)
    {
        if (count <= 0)
            return null;

        ParticleSystem system = CreateSystem(instance, name, material, ParticleSystemRenderMode.Billboard, settings.UseUnscaledTime, count);

        ParticleSystem.MainModule main = system.main;
        main.gravityModifier = layer.Gravity;

        SetColorOverLifetime(system, gradient);
        SetSizeOverLifetime(system, layer.SizeOverLifetime);
        SetDrag(system, layer.Drag);

        return system;
    }

    private static void SetColorOverLifetime(ParticleSystem system, Gradient gradient)
    {
        ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
        color.enabled = gradient != null;

        if (gradient != null)
            color.color = new ParticleSystem.MinMaxGradient(gradient);
    }

    private static void SetSizeOverLifetime(ParticleSystem system, AnimationCurve curve)
    {
        ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
        size.enabled = curve != null && curve.length > 0;

        if (size.enabled)
            size.size = new ParticleSystem.MinMaxCurve(1f, curve);
    }

    private static void SetDrag(ParticleSystem system, float drag)
    {
        ParticleSystem.LimitVelocityOverLifetimeModule limit = system.limitVelocityOverLifetime;
        limit.enabled = drag > 0f;

        if (!limit.enabled)
            return;

        limit.limit = 100000f;
        limit.dampen = 0f;
        limit.drag = drag;
        limit.multiplyDragByParticleSize = false;
        limit.multiplyDragByParticleVelocity = false;
    }

    #endregion


    #region Generated Assets

    private Material GetMaterial(KillEffectInstance instance, string name, Material materialOverride, Texture2D texture, float glow)
    {
        if (materialOverride != null)
            return materialOverride;

        Shader shader = Shader.Find(ParticleShaderName);
        if (shader == null)
        {
            Debug.LogError($"KillEffectBuilder: shader '{ParticleShaderName}' not found. Assign a material override for {name}.");
            return null;
        }

        Material material = new Material(shader) { name = name };
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 2f);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)BlendMode.One);
        material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        material.SetFloat("_DstBlendAlpha", (float)BlendMode.One);
        material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)RenderQueue.Transparent;
        material.SetTexture("_BaseMap", texture);
        material.SetColor("_BaseColor", new Color(glow, glow, glow, 1f));

        instance.Materials.Add(material);
        return material;
    }

    private void EnsureTextures(KillEffectSettings settings)
    {
        if (glowTexture == null)
            glowTexture = CreateGlowTexture("KillGlow", 64, 2f, 0.08f);

        if (streakTexture == null)
            streakTexture = CreateGlowTexture("KillStreak", 32, 3f, 0.04f);

        if (shardTexture == null)
            shardTexture = CreateShardTexture(32);

        float thickness = settings.Shockwave.Thickness;
        if (ringTexture == null || !Mathf.Approximately(ringTextureThickness, thickness))
        {
            ringTexture = CreateRingTexture(128, thickness);
            ringTextureThickness = thickness;
        }
    }

    /// <summary>Destroys every generated texture.</summary>
    public void Dispose()
    {
        foreach (Texture2D texture in textures)
        {
            if (texture != null)
                Object.Destroy(texture);
        }

        textures.Clear();
        glowTexture = null;
        streakTexture = null;
        shardTexture = null;
        ringTexture = null;
        ringTextureThickness = -1f;
    }

    private Texture2D CreateTexture(string name, int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = name,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        textures.Add(texture);
        return texture;
    }

    private static Color32 AlphaPixel(float alpha)
    {
        return new Color32(255, 255, 255, (byte)(Mathf.Clamp01(alpha) * 255f));
    }

    /// <summary>Fills a square texture from alpha(dx, dy), with dx and dy in -1..1.</summary>
    private Texture2D FillTexture(string name, int size, System.Func<float, float, float> alpha)
    {
        Texture2D texture = CreateTexture(name, size);
        Color32[] pixels = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f;
                float dy = (y + 0.5f) / size * 2f - 1f;
                pixels[y * size + x] = AlphaPixel(alpha(dx, dy));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return texture;
    }

    /// <summary>Radial falloff with a sharp hot core of the given radius.</summary>
    private Texture2D CreateGlowTexture(string name, int size, float power, float coreRadius)
    {
        return FillTexture(name, size, (dx, dy) =>
        {
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            float falloff = Mathf.Pow(Mathf.Clamp01(1f - r), power);
            float core = Mathf.Exp(-(r * r) / Mathf.Max(1e-4f, coreRadius));
            return falloff + core;
        });
    }

    /// <summary>Tall diamond with soft edges and a bright center.</summary>
    private Texture2D CreateShardTexture(int size)
    {
        return FillTexture("KillShard", size, (dx, dy) =>
        {
            float d = Mathf.Abs(dx) / 0.35f + Mathf.Abs(dy);
            float body = Mathf.Clamp01((1f - d) * 4f);
            float core = Mathf.Clamp01(1f - d) * 0.5f;
            return body * 0.75f + core;
        });
    }

    /// <summary>Ring whose outer edge touches the texture border. thickness is the band width as a fraction of the radius.</summary>
    private Texture2D CreateRingTexture(int size, float thickness)
    {
        float width = Mathf.Max(0.01f, thickness);
        float center = 1f - width;

        return FillTexture("KillRing", size, (dx, dy) =>
        {
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            float offset = (r - center) / (width * 0.5f);
            return Mathf.Exp(-offset * offset) * Mathf.Clamp01((1f - r) * 20f);
        });
    }

    #endregion
}

/// <summary>One playing kill effect: its root object, its generated materials and when it ends.</summary>
public class KillEffectInstance
{
    public readonly GameObject Root;
    public readonly bool UnscaledTime;
    public readonly List<Material> Materials = new List<Material>();
    public float EndTime;

    public KillEffectInstance(GameObject root, bool unscaledTime)
    {
        Root = root;
        UnscaledTime = unscaledTime;
    }

    public float Now => UnscaledTime ? Time.unscaledTime : Time.time;

    public bool IsFinished => Now >= EndTime;

    /// <summary>Destroys the root object and the generated materials.</summary>
    public void Destroy()
    {
        if (Root != null)
            Object.Destroy(Root);

        foreach (Material material in Materials)
        {
            if (material != null)
                Object.Destroy(material);
        }

        Materials.Clear();
    }
}
