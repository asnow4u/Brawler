using UnityEngine;

[CreateAssetMenu(fileName = "GuardShield", menuName = "ScriptableObjects/SceneObject/Effects/Guard Shield")]
public class GuardShieldEffectData : EffectData
{
    [Tooltip("Shield prefab. Its material holds the look; _Fade is set per renderer at runtime.")]
    public MeshRenderer Prefab;

    [Header("Size")]
    [Tooltip("World units added around the larger of the Bounds width and height before the oval proportions apply.")]
    public float Padding = 0.25f;
    [Tooltip("Shield diameter per axis as a multiple of the padded Bounds size.")]
    public Vector3 OvalProportions = new Vector3(1.1f, 0.85f, 0.7f);
    [Tooltip("Seconds for the shield to catch up to the Bounds center.")]
    public float FollowSmoothTime = 0.03f;

    [Header("Timing")]
    [Tooltip("Seconds to fade in on Raise.")]
    public float FadeInTime = 0.06f;
    [Tooltip("Seconds to fade out when the guard ends without a counter.")]
    public float FadeOutTime = 0.12f;
    [Tooltip("Seconds before the guard duration runs out in which the shield flickers.")]
    public float FlickerDuration = 0.3f;
    [Tooltip("Flickers per second.")]
    public float FlickerRate = 22f;
    [Tooltip("Opacity multiplier during the dim half of a flicker.")]
    [Range(0f, 1f)] public float FlickerMinAlpha = 0.2f;

    [Header("Counter Pop")]
    [Tooltip("Shield scale multiplier reached at the end of the pop.")]
    public float PopScale = 1.35f;
    [Tooltip("Seconds for the shield to expand and fade on Counter.")]
    public float PopTime = 0.08f;
}
