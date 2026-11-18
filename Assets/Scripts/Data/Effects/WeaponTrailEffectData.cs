using UnityEngine;

[CreateAssetMenu(fileName = "WeaponTrail", menuName = "ScriptableObjects/SceneObject/Effects/Weapon Trail")]
public class WeaponTrailEffectData : EffectData
{
    [Tooltip("Weapon trail prefab. Holds the wisp and spark systems and the per-hitbox templates with their materials.")]
    public WeaponTrailRig Prefab;

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
}
