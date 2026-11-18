using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class EffectsHandler
{
    private const string WeaponTrailGroupName = "WeaponTrail";
    private const int TrailBankCount = 2;
    private const int MaxSpikesPerHitBox = 128;
    private const float SpikeParticleLifetime = 1f;

    private WeaponTrailEffectData trailSettings;
    private WeaponTrailRig weaponTrailRig;
    private ParticleSystem fireWispSystem;
    private ParticleSystem sparkSystem;

    private readonly List<HitBoxTrail> hitBoxTrails = new List<HitBoxTrail>();

    private bool trailsArmed;
    private int activeTrailBank;

    private class HitBoxTrail
    {
        public IHitBox HitBox;
        public TrailLane[] SpiritLanes;

        // [0] = older sample, [1] = newest sample
        public readonly Vector3[] Starts = new Vector3[2];
        public readonly Vector3[] Ends = new Vector3[2];
        public int HistoryCount;

        public float Strength;
        public float WispCarry;
        public float SparkCarry;

        public ParticleSystem SpikeSystem;
        public LineRenderer EdgeLine;
        public bool SpikesLive;
        public int SpikeCount;
        public readonly float[] SpikeU = new float[MaxSpikesPerHitBox];
        public readonly float[] SpikeWidths = new float[MaxSpikesPerHitBox];
        public readonly ParticleSystem.Particle[] SpikeBuffer = new ParticleSystem.Particle[MaxSpikesPerHitBox];
        public Vector3[] EdgePositions;
    }

    private class TrailLane
    {
        public float U;
        public float Variation;
        public ParticleSystem[] Banks;
    }


    #region Setup

    /// <summary>Creates the weapon trail rig under Effects/WeaponTrail when there is trail data, an IAttackHitBoxHandler and an IActionState.</summary>
    private void SetupWeaponTrails()
    {
        WeaponTrailEffectData data = effectsData.WeaponTrail;
        if (data == null || data.Prefab == null || attackHitBoxHandler == null || actionState == null)
            return;

        trailSettings = data;
        weaponTrailRig = InstantiateEffect(data.Prefab, GetEffectGroup(WeaponTrailGroupName));

        SetTemplateActive(weaponTrailRig.SpiritLaneTemplate, false);
        SetTemplateActive(weaponTrailRig.SpikeTemplate, false);
        SetTemplateActive(weaponTrailRig.EdgeLineTemplate, false);

        fireWispSystem = weaponTrailRig.Wisps;
        if (fireWispSystem != null)
        {
            PrepareTrailSystem(fireWispSystem);
            SetGlow(fireWispSystem.GetComponent<ParticleSystemRenderer>(), trailSettings.Fire.WispGlow);
        }

        sparkSystem = weaponTrailRig.Sparks;
        if (sparkSystem != null)
        {
            PrepareTrailSystem(sparkSystem);
            SetGlow(sparkSystem.GetComponent<ParticleSystemRenderer>(), trailSettings.Sparks.Glow);
        }

        attackHitBoxHandler.HitBoxsChangedEvent += RebuildWeaponTrails;
    }

    private void TeardownWeaponTrails()
    {
        if (trailSettings == null)
            return;

        attackHitBoxHandler.HitBoxsChangedEvent -= RebuildWeaponTrails;
        DestroyTrailLanes();
        DestroyInstance(weaponTrailRig);

        trailSettings = null;
        weaponTrailRig = null;
        fireWispSystem = null;
        sparkSystem = null;
        trailsArmed = false;
    }

    /// <summary>Builds the per-hitbox trails once the hitboxes are available.</summary>
    private void StartWeaponTrails()
    {
        if (hasStarted && trailSettings != null)
            RebuildWeaponTrails();
    }

    /// <summary>Recreates the per-hitbox trails for the current weapon hitboxes and reapplies the fire and spark settings.</summary>
    private void RebuildWeaponTrails()
    {
        DestroyTrailLanes();
        trailsArmed = false;

        IReadOnlyList<IHitBox> hitBoxs = attackHitBoxHandler.HitBoxs;
        for (int i = 0; i < hitBoxs.Count; i++)
        {
            HitBoxTrail trail = new HitBoxTrail { HitBox = hitBoxs[i] };
            trail.SpiritLanes = CreateTrailLanes($"Spirit_{i}", trailSettings.SpiritArc);
            trail.SpikeSystem = CreateSpikeSystem($"Spikes_{i}");
            trail.EdgeLine = CreateEdgeLine($"EdgeLine_{i}");
            trail.EdgePositions = new Vector3[Mathf.Clamp(trailSettings.Fire.EdgeLinePoints, 2, 32)];
            hitBoxTrails.Add(trail);
        }

        ConfigureFireSystem();
        ConfigureSparkSystem();
    }

    private TrailLane[] CreateTrailLanes(string name, ArcLayerSettings layer)
    {
        if (weaponTrailRig.SpiritLaneTemplate == null)
            return null;

        int count = Mathf.Max(1, layer.LanesPerHitBox);
        TrailLane[] lanes = new TrailLane[count];

        for (int k = 0; k < count; k++)
        {
            TrailLane lane = new TrailLane
            {
                U = Mathf.Clamp01((k + 0.5f + layer.LaneShift * 0.5f) / count),
                Variation = Random.Range(0.6f, 1f),
                Banks = new ParticleSystem[TrailBankCount]
            };

            for (int b = 0; b < TrailBankCount; b++)
                lane.Banks[b] = CreateRibbonSystem($"{name}_Lane{k}_Bank{b}", layer);

            lanes[k] = lane;
        }

        return lanes;
    }

    private void DestroyTrailLanes()
    {
        foreach (HitBoxTrail trail in hitBoxTrails)
        {
            DestroyLanes(trail.SpiritLanes);
            DestroyInstance(trail.SpikeSystem);
            DestroyInstance(trail.EdgeLine);
        }

        hitBoxTrails.Clear();
    }

    private static void DestroyLanes(TrailLane[] lanes)
    {
        if (lanes == null)
            return;

        foreach (TrailLane lane in lanes)
        {
            foreach (ParticleSystem system in lane.Banks)
                DestroyInstance(system);
        }
    }

    private static void SetTemplateActive(Component template, bool active)
    {
        if (template != null)
            template.gameObject.SetActive(active);
    }

    /// <summary>Clones an inactive template under the rig and activates the clone.</summary>
    private T CloneTemplate<T>(T template, string name) where T : Component
    {
        T clone = Instantiate(template, weaponTrailRig.transform);
        clone.name = name;
        clone.gameObject.SetActive(true);
        return clone;
    }

    /// <summary>Sets the renderer's _BaseColor to a grey HDR brightness multiplier.</summary>
    private void SetGlow(Renderer target, float glow)
    {
        if (target != null)
            SetRendererBaseColor(target, new Color(glow, glow, glow, 1f));
    }

    #endregion


    #region Particle Systems

    /// <summary>Script-driven emission: no shape, no rate emission, world simulation.</summary>
    private static void PrepareTrailSystem(ParticleSystem system)
    {
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = system.main;
        main.playOnAwake = false;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startSpeed = 0f;
        main.gravityModifier = 0f;

        ParticleSystem.EmissionModule emission = system.emission;
        emission.enabled = false;

        ParticleSystem.ShapeModule shape = system.shape;
        shape.enabled = false;

        ParticleSystemRenderer renderer = system.GetComponent<ParticleSystemRenderer>();
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private ParticleSystem CreateRibbonSystem(string name, ArcLayerSettings layer)
    {
        ParticleSystem system = CloneTemplate(weaponTrailRig.SpiritLaneTemplate, name);
        PrepareTrailSystem(system);

        ParticleSystem.MainModule main = system.main;
        main.maxParticles = 512;

        ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(layer.Color);

        ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, layer.WidthOverLifetime);

        ParticleSystem.NoiseModule noise = system.noise;
        noise.enabled = layer.NoiseStrength > 0f;
        noise.strength = layer.NoiseStrength;
        noise.frequency = layer.NoiseFrequency;
        noise.scrollSpeed = 1f;
        noise.damping = true;
        noise.quality = ParticleSystemNoiseQuality.Medium;

        ParticleSystem.TrailModule trails = system.trails;
        trails.enabled = true;
        trails.mode = ParticleSystemTrailMode.Ribbon;
        trails.ribbonCount = 1;
        trails.lifetime = 1f;
        trails.textureMode = ParticleSystemTrailTextureMode.Stretch;
        trails.sizeAffectsWidth = true;
        trails.inheritParticleColor = true;
        trails.dieWithParticles = true;
        trails.widthOverTrail = 1f;

        SetGlow(system.GetComponent<ParticleSystemRenderer>(), layer.Glow);

        system.Play();
        return system;
    }

    // Each spike is a particle held on the leading edge; its per-particle trail draws the tapered spike behind it.
    private ParticleSystem CreateSpikeSystem(string name)
    {
        if (weaponTrailRig.SpikeTemplate == null)
            return null;

        FireLayerSettings fire = trailSettings.Fire;
        ParticleSystem system = CloneTemplate(weaponTrailRig.SpikeTemplate, name);
        PrepareTrailSystem(system);

        ParticleSystem.MainModule main = system.main;
        main.maxParticles = MaxSpikesPerHitBox * 2;

        ParticleSystem.TrailModule trails = system.trails;
        trails.enabled = true;
        trails.mode = ParticleSystemTrailMode.PerParticle;
        trails.ratio = 1f;
        trails.lifetime = new ParticleSystem.MinMaxCurve(
            Mathf.Max(0.001f, fire.SpikeLength.x) / SpikeParticleLifetime,
            Mathf.Max(0.001f, fire.SpikeLength.y) / SpikeParticleLifetime);
        trails.minVertexDistance = 0.01f;
        trails.worldSpace = true;
        trails.dieWithParticles = false;
        trails.textureMode = ParticleSystemTrailTextureMode.Stretch;
        trails.sizeAffectsWidth = true;
        trails.inheritParticleColor = false;
        trails.colorOverTrail = new ParticleSystem.MinMaxGradient(fire.SpikeColor);
        trails.widthOverTrail = new ParticleSystem.MinMaxCurve(1f, fire.SpikeTaper);

        SetGlow(system.GetComponent<ParticleSystemRenderer>(), fire.SpikeGlow);

        system.Play();
        return system;
    }

    private LineRenderer CreateEdgeLine(string name)
    {
        if (weaponTrailRig.EdgeLineTemplate == null)
            return null;

        LineRenderer line = CloneTemplate(weaponTrailRig.EdgeLineTemplate, name);
        line.useWorldSpace = true;
        line.alignment = LineAlignment.View;
        line.textureMode = LineTextureMode.Stretch;
        line.numCapVertices = 2;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.positionCount = 0;
        line.enabled = false;

        SetGlow(line, trailSettings.Fire.EdgeLineGlow);
        return line;
    }

    private void ConfigureFireSystem()
    {
        FireLayerSettings fire = trailSettings.Fire;

        if (fireWispSystem != null)
        {
            ParticleSystem.MainModule main = fireWispSystem.main;
            main.maxParticles = 800;

            ParticleSystem.ColorOverLifetimeModule color = fireWispSystem.colorOverLifetime;
            color.enabled = true;
            color.color = new ParticleSystem.MinMaxGradient(fire.WispColor);

            ParticleSystem.SizeOverLifetimeModule size = fireWispSystem.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.3f));

            ParticleSystem.NoiseModule noise = fireWispSystem.noise;
            noise.enabled = fire.WispNoiseStrength > 0f;
            noise.strength = fire.WispNoiseStrength;
            noise.frequency = fire.WispNoiseFrequency;
            noise.scrollSpeed = 2f;
            noise.damping = true;
            noise.quality = ParticleSystemNoiseQuality.Medium;

            ParticleSystem.TrailModule trails = fireWispSystem.trails;
            trails.enabled = true;
            trails.mode = ParticleSystemTrailMode.PerParticle;
            trails.ratio = 1f;
            trails.lifetime = fire.WispTrailLength;
            trails.minVertexDistance = 0.02f;
            trails.worldSpace = true;
            trails.dieWithParticles = true;
            trails.textureMode = ParticleSystemTrailTextureMode.Stretch;
            trails.sizeAffectsWidth = true;
            trails.inheritParticleColor = true;
            trails.widthOverTrail = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

            if (!fireWispSystem.isPlaying)
                fireWispSystem.Play();
        }
    }

    private void ConfigureSparkSystem()
    {
        if (sparkSystem == null)
            return;

        SparkLayerSettings sparks = trailSettings.Sparks;

        ParticleSystem.MainModule main = sparkSystem.main;
        main.maxParticles = 500;
        main.gravityModifier = sparks.Gravity;

        ParticleSystem.ColorOverLifetimeModule color = sparkSystem.colorOverLifetime;
        color.enabled = true;
        color.color = new ParticleSystem.MinMaxGradient(sparks.Color);

        ParticleSystem.SizeOverLifetimeModule size = sparkSystem.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

        ParticleSystemRenderer renderer = sparkSystem.GetComponent<ParticleSystemRenderer>();
        renderer.velocityScale = sparks.Stretch;
        renderer.lengthScale = 1f;

        if (!sparkSystem.isPlaying)
            sparkSystem.Play();
    }

    #endregion


    #region Update

    private void UpdateWeaponTrails(float deltaTime)
    {
        if (trailSettings == null || deltaTime <= 0f || hitBoxTrails.Count == 0)
            return;

        bool armed = actionState.CurActionState == ActionState.Attacking;
        if (armed != trailsArmed)
        {
            trailsArmed = armed;
            if (armed)
            {
                activeTrailBank = (activeTrailBank + 1) % TrailBankCount;
                foreach (HitBoxTrail trail in hitBoxTrails)
                {
                    trail.HistoryCount = 0;
                    trail.Strength = 0f;
                    trail.WispCarry = 0f;
                    trail.SparkCarry = 0f;
                }
            }
            else
            {
                foreach (HitBoxTrail trail in hitBoxTrails)
                    ReleaseSpikes(trail);
            }
        }

        if (!armed)
            return;

        if (!TryGetWeaponSpan(out Vector3 spanStart, out Vector3 spanAxis, out float spanLength))
            return;

        foreach (HitBoxTrail trail in hitBoxTrails)
            UpdateHitBoxTrail(trail, deltaTime, spanStart, spanAxis, spanLength);
    }

    // Measures the combined extent of all hitboxes along the weapon's length axis.
    private bool TryGetWeaponSpan(out Vector3 spanStart, out Vector3 spanAxis, out float spanLength)
    {
        spanStart = Vector3.zero;
        spanAxis = Vector3.zero;
        spanLength = 0f;

        bool hasAxis = false;
        Vector3 origin = Vector3.zero;
        float min = float.MaxValue;
        float max = float.MinValue;

        foreach (HitBoxTrail trail in hitBoxTrails)
        {
            if (!trail.HitBox.TryGetLengthSegment(out Vector3 start, out Vector3 end, out _))
                continue;

            if (!hasAxis)
            {
                Vector3 direction = end - start;
                if (direction.sqrMagnitude < 1e-8f)
                    continue;

                spanAxis = direction.normalized;
                origin = start;
                hasAxis = true;
            }

            float a = Vector3.Dot(start - origin, spanAxis);
            float b = Vector3.Dot(end - origin, spanAxis);
            min = Mathf.Min(min, Mathf.Min(a, b));
            max = Mathf.Max(max, Mathf.Max(a, b));
        }

        if (!hasAxis)
            return false;

        spanStart = origin + spanAxis * min;
        spanLength = Mathf.Max(max - min, 1e-4f);
        return true;
    }

    private void UpdateHitBoxTrail(HitBoxTrail trail, float deltaTime, Vector3 spanStart, Vector3 spanAxis, float spanLength)
    {
        if (!trail.HitBox.TryGetLengthSegment(out Vector3 start, out Vector3 end, out float width))
            return;

        if (trail.HistoryCount == 0)
        {
            PushTrailHistory(trail, start, end);
            return;
        }

        Vector3 prevStart = trail.Starts[1];
        Vector3 prevEnd = trail.Ends[1];
        Vector3 startVelocity = (start - prevStart) / deltaTime;
        Vector3 endVelocity = (end - prevEnd) / deltaTime;

        float travel = Mathf.Max((start - prevStart).magnitude, (end - prevEnd).magnitude);
        float speed = travel / deltaTime;

        float prevStrength = trail.Strength;
        float strength = Mathf.InverseLerp(trailSettings.MinSpeed, trailSettings.FullSpeed, speed) * trailSettings.Intensity;
        trail.Strength = strength;

        if (travel > 1e-4f)
        {
            int points = Mathf.Clamp(Mathf.CeilToInt(travel * trailSettings.PointsPerMeter), 1, Mathf.Max(1, trailSettings.MaxPointsPerFrame));
            Vector3 olderStart = trail.Starts[0];
            Vector3 olderEnd = trail.Ends[0];
            Vector3 nextStart = start + (start - prevStart);
            Vector3 nextEnd = end + (end - prevEnd);

            for (int p = 1; p <= points; p++)
            {
                float t = (float)p / points;
                Vector3 pointStart = CatmullRom(olderStart, prevStart, start, nextStart, t);
                Vector3 pointEnd = CatmullRom(olderEnd, prevEnd, end, nextEnd, t);
                float pointStrength = Mathf.Lerp(prevStrength, strength, t);

                EmitArcLanes(trail.SpiritLanes, trailSettings.SpiritArc, pointStart, pointEnd, pointStrength, spanStart, spanAxis, spanLength);
            }
        }

        UpdateFire(trail, start, end, width, startVelocity, endVelocity, strength, deltaTime);

        EmitSparks(trail, end, endVelocity, strength, deltaTime, spanStart, spanAxis, spanLength);

        PushTrailHistory(trail, start, end);
    }

    private static void PushTrailHistory(HitBoxTrail trail, Vector3 start, Vector3 end)
    {
        if (trail.HistoryCount == 0)
        {
            trail.Starts[0] = start;
            trail.Ends[0] = end;
        }
        else
        {
            trail.Starts[0] = trail.Starts[1];
            trail.Ends[0] = trail.Ends[1];
        }

        trail.Starts[1] = start;
        trail.Ends[1] = end;
        trail.HistoryCount = Mathf.Min(trail.HistoryCount + 1, 2);
    }

    private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * (2f * p1
            + (p2 - p0) * t
            + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2
            + (3f * p1 - p0 - 3f * p2 + p3) * t3);
    }

    #endregion


    #region Emission

    private void EmitArcLanes(TrailLane[] lanes, ArcLayerSettings layer, Vector3 start, Vector3 end, float strength,
        Vector3 spanStart, Vector3 spanAxis, float spanLength)
    {
        if (!layer.Enabled || lanes == null)
            return;

        ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams
        {
            applyShapeToPosition = false,
            velocity = Vector3.zero
        };

        float sharpness = Mathf.Max(0.01f, layer.FocusSharpness);

        foreach (TrailLane lane in lanes)
        {
            Vector3 position = Vector3.Lerp(start, end, lane.U);
            float weaponU = Mathf.Clamp01(Vector3.Dot(position - spanStart, spanAxis) / spanLength);
            float focus = Mathf.Pow(1f - Mathf.Abs(weaponU - layer.Focus), sharpness);
            float laneStrength = Mathf.Lerp(layer.MinLaneStrength, 1f, focus) * lane.Variation;

            emit.position = position;
            emit.startSize = layer.Width * laneStrength * Mathf.Lerp(0.35f, 1f, strength);
            emit.startLifetime = layer.Lifetime * Mathf.Lerp(0.5f, 1f, strength);
            emit.startColor = new Color(1f, 1f, 1f, Mathf.Clamp01(laneStrength * strength));

            lane.Banks[activeTrailBank].Emit(emit, 1);
        }
    }

    // Runs the spikes, edge line and wisps while the hitbox is active; releases the spikes otherwise.
    private void UpdateFire(HitBoxTrail trail, Vector3 start, Vector3 end, float width,
        Vector3 startVelocity, Vector3 endVelocity, float strength, float deltaTime)
    {
        FireLayerSettings fire = trailSettings.Fire;
        if (!fire.Enabled || !trail.HitBox.IsActive)
        {
            ReleaseSpikes(trail);
            return;
        }

        float scale = Mathf.Lerp(fire.MinRateFraction, 1f, strength) * trailSettings.Intensity;

        Vector3 side = Vector3.Cross(end - start, Vector3.forward);
        side = side.sqrMagnitude > 1e-8f ? side.normalized : Vector3.up;

        if (!trail.SpikesLive && trail.SpikeSystem != null)
            StartSpikes(trail, start, end);

        if (trail.SpikesLive)
            UpdateSpikes(trail, start, end, width, side, startVelocity, endVelocity, scale);

        UpdateEdgeLine(trail, start, end, width, side, startVelocity, endVelocity, scale);
        EmitWisps(trail, start, end, width, side, startVelocity, endVelocity, scale, deltaTime);
    }

    private void StartSpikes(HitBoxTrail trail, Vector3 start, Vector3 end)
    {
        FireLayerSettings fire = trailSettings.Fire;
        float length = (end - start).magnitude;
        int count = Mathf.Clamp(Mathf.CeilToInt(length / Mathf.Max(0.01f, fire.SpikeSpacing)) + 1, 2, MaxSpikesPerHitBox);
        float jitter = fire.SpikeJitter * 0.5f / (count - 1);

        ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams
        {
            applyShapeToPosition = false,
            velocity = Vector3.zero,
            startLifetime = SpikeParticleLifetime,
            startSize = 0f,
            startColor = Color.white
        };

        for (int k = 0; k < count; k++)
        {
            trail.SpikeU[k] = Mathf.Clamp01((float)k / (count - 1) + Random.Range(-jitter, jitter));
            trail.SpikeWidths[k] = Random.Range(fire.SpikeWidth.x, fire.SpikeWidth.y);

            emit.position = Vector3.Lerp(start, end, trail.SpikeU[k]);
            trail.SpikeSystem.Emit(emit, 1);
        }

        trail.SpikeCount = count;
        trail.SpikesLive = true;
    }

    // Holds every spike particle on the leading edge and keeps it alive.
    private void UpdateSpikes(HitBoxTrail trail, Vector3 start, Vector3 end, float width, Vector3 side,
        Vector3 startVelocity, Vector3 endVelocity, float scale)
    {
        FireLayerSettings fire = trailSettings.Fire;
        int count = trail.SpikeSystem.GetParticles(trail.SpikeBuffer, trail.SpikeCount);
        float time = Time.time;

        for (int k = 0; k < count; k++)
        {
            float u = trail.SpikeU[k];
            Vector3 velocity = Vector3.Lerp(startVelocity, endVelocity, u);
            float flicker = 1f + (Mathf.PerlinNoise(time * 12f, k * 1.37f) - 0.5f) * 2f * fire.SpikeFlicker;

            trail.SpikeBuffer[k].position = LeadingEdgePoint(start, end, u, side, width, velocity, fire.SpikeInset);
            trail.SpikeBuffer[k].velocity = Vector3.zero;
            trail.SpikeBuffer[k].startSize = trail.SpikeWidths[k] * scale * flicker;
            trail.SpikeBuffer[k].remainingLifetime = SpikeParticleLifetime;
        }

        trail.SpikeSystem.SetParticles(trail.SpikeBuffer, count);
    }

    // Ends the spike particles; their trails stay and fade out.
    private void ReleaseSpikes(HitBoxTrail trail)
    {
        if (trail.EdgeLine != null)
            trail.EdgeLine.enabled = false;

        if (!trail.SpikesLive)
            return;

        trail.SpikesLive = false;
        if (trail.SpikeSystem == null)
            return;

        int count = trail.SpikeSystem.GetParticles(trail.SpikeBuffer, MaxSpikesPerHitBox);
        for (int k = 0; k < count; k++)
            trail.SpikeBuffer[k].remainingLifetime = 0.0001f;

        trail.SpikeSystem.SetParticles(trail.SpikeBuffer, count);
    }

    private void UpdateEdgeLine(HitBoxTrail trail, Vector3 start, Vector3 end, float width, Vector3 side,
        Vector3 startVelocity, Vector3 endVelocity, float scale)
    {
        FireLayerSettings fire = trailSettings.Fire;
        LineRenderer line = trail.EdgeLine;
        if (line == null)
            return;

        if (!fire.EdgeLineEnabled)
        {
            line.enabled = false;
            return;
        }

        int points = trail.EdgePositions.Length;
        for (int i = 0; i < points; i++)
        {
            float u = (float)i / (points - 1);
            Vector3 velocity = Vector3.Lerp(startVelocity, endVelocity, u);
            trail.EdgePositions[i] = LeadingEdgePoint(start, end, u, side, width, velocity, fire.SpikeInset);
        }

        line.positionCount = points;
        line.SetPositions(trail.EdgePositions);
        line.widthMultiplier = fire.EdgeLineWidth * scale;
        line.startColor = fire.EdgeLineColor;
        line.endColor = fire.EdgeLineColor;
        line.enabled = true;
    }

    private void EmitWisps(HitBoxTrail trail, Vector3 start, Vector3 end, float width, Vector3 side,
        Vector3 startVelocity, Vector3 endVelocity, float scale, float deltaTime)
    {
        FireLayerSettings fire = trailSettings.Fire;

        ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams
        {
            applyShapeToPosition = false,
            startColor = Color.white
        };

        if (fireWispSystem != null)
        {
            trail.WispCarry += fire.WispRate * scale * deltaTime;
            int count = Mathf.FloorToInt(trail.WispCarry);
            trail.WispCarry -= count;

            for (int i = 0; i < count; i++)
            {
                float u = RandomFireU(fire.WispTipBias);
                Vector3 hitBoxVelocity = Vector3.Lerp(startVelocity, endVelocity, u);

                emit.position = LeadingEdgePoint(start, end, u, side, width, hitBoxVelocity, Random.value * fire.WispInset);
                emit.velocity = hitBoxVelocity * Random.Range(fire.WispFollow.x, fire.WispFollow.y)
                    + Vector3.up * fire.WispRise
                    + Random.insideUnitSphere * 0.3f;
                emit.startSize = Random.Range(fire.WispWidth.x, fire.WispWidth.y);
                emit.startLifetime = Random.Range(fire.WispLifetime.x, fire.WispLifetime.y);

                fireWispSystem.Emit(emit, 1);
            }
        }
    }

    // Point at u along the hitbox, on the side its velocity moves toward, moved inward by inset (fraction of width).
    // Velocity mostly along the hitbox length blends the point toward the centerline.
    private static Vector3 LeadingEdgePoint(Vector3 start, Vector3 end, float u, Vector3 side, float width, Vector3 velocity, float inset)
    {
        float speed = velocity.magnitude;
        float facing = speed > 1e-4f ? Mathf.Clamp(Vector3.Dot(velocity, side) / (speed * 0.3f), -1f, 1f) : 0f;
        float offset = facing * (0.5f - inset) * width;
        return Vector3.Lerp(start, end, u) + side * offset;
    }

    // Random position along a hitbox, 0 = -Y end, 1 = +Y end, pulled toward 1 by tipBias.
    private static float RandomFireU(float tipBias)
    {
        return 1f - Mathf.Pow(Random.value, 1f + tipBias * 3f);
    }

    private void EmitSparks(HitBoxTrail trail, Vector3 end, Vector3 endVelocity, float strength, float deltaTime,
        Vector3 spanStart, Vector3 spanAxis, float spanLength)
    {
        SparkLayerSettings sparks = trailSettings.Sparks;
        if (!sparks.Enabled || sparkSystem == null || strength <= sparks.Threshold)
            return;

        // Only the hitbox that reaches the weapon tip throws sparks.
        if (Vector3.Dot(end - spanStart, spanAxis) < spanLength * 0.98f)
            return;

        float amount = Mathf.InverseLerp(sparks.Threshold, 1f, strength);
        trail.SparkCarry += sparks.Rate * amount * deltaTime;
        int count = Mathf.FloorToInt(trail.SparkCarry);
        trail.SparkCarry -= count;
        if (count <= 0)
            return;

        ParticleSystem.EmitParams emit = new ParticleSystem.EmitParams
        {
            applyShapeToPosition = false,
            position = end,
            startColor = Color.white
        };

        for (int i = 0; i < count; i++)
        {
            Vector3 velocity = endVelocity * Random.Range(sparks.InheritVelocity.x, sparks.InheritVelocity.y)
                + Random.insideUnitSphere * Random.Range(sparks.SpraySpeed.x, sparks.SpraySpeed.y);
            velocity.z *= 0.3f;

            emit.velocity = velocity;
            emit.startSize = Random.Range(sparks.Size.x, sparks.Size.y);
            emit.startLifetime = Random.Range(sparks.Lifetime.x, sparks.Lifetime.y);

            sparkSystem.Emit(emit, 1);
        }
    }

    #endregion
}
