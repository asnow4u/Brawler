using UnityEngine;

[CreateAssetMenu(fileName = "Dash", menuName = "ScriptableObjects/SceneObject/Movement/Dash")]
public class DashData : BaseMovementData
{
    [Header("Wave Land [Happens upon landing]")]
    [Tooltip("How much of max grounded velocity should be applied")]
    public float WaveLandVelocityScaler;
    public float WaveLandDuration;

    [Header("Inital Dash [Start moving from standstill]")]
    public float InitialDashVelocityScaler;
    public float InitalDashDuration;

    [Header("Horizontal Dash [Action]")]
    public float HorizontalDashVelocity;
    public float HorizontalDashDuration;

    public override bool IsValid()
    {
        return AnimationData.Animation != null &&
                WaveLandVelocityScaler > 0 &&
                WaveLandDuration > 0 &&
                InitialDashVelocityScaler > 0 &&
                InitalDashDuration > 0;
    }
}
