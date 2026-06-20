using UnityEngine;

[CreateAssetMenu(fileName = "Dash", menuName = "ScriptableObjects/SceneObject/Movement/Dash")]
public class DashData : BaseMovementData
{
    [Header("Wave Land")]
    [Tooltip("How much of max grounded velocity should be applied")]
    public float WaveLandVelocityScaler;
    public float WaveLandDuration;

    [Header("Inital Dash")]
    public float InitialDashVelocityScaler;
    public float InitalDashDuration;

    [Header("Horizontal Dash")]
    public float HorizontalDashVelocity;
    public float HorizontalDashDuration;

    public override bool IsValid()
    {
        // NOTE: HorizontalDash fields are intentionally not validated yet - that feature is
        // an unimplemented stub. Gating dash validity on it would disable wavelanding and the
        // initial run dash, which are fully implemented. Add those checks when the feature exists.
        return AnimationData.Animation != null &&
                WaveLandVelocityScaler > 0 &&
                WaveLandDuration > 0 &&
                InitialDashVelocityScaler > 0 &&
                InitalDashDuration > 0;
    }
}
