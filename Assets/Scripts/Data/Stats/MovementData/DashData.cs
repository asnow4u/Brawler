using UnityEngine;

[CreateAssetMenu(fileName = "Dash", menuName = "ScriptableObjects/SceneObject/Movement/Dash")]
public class DashData : BaseMovementData
{
    [Header("Wave Land [Happens upon landing]")]
    public float WaveLandVelocity;
    public float WaveLandDuration;

    [Header("Inital Dash [Start moving from standstill]")]
    public float InitialDashVelocity;
    public float InitalDashDuration;

    [Header("Input Dash [Action]")]
    public float InputDashVelocity;
    public float InputDashDuration;
    public float HorizontalEndDashVelocity;
    public float VerticalEndDashVelocity;

    [Header("Shared [Applies to all dashes]")]
    [Tooltip("Fraction of the dash duration held at full speed before decaying toward the end velocity.")]
    [Range(0f, 1f)]
    public float DashSpeedHoldPercentage = 0.7f;

    public override bool IsValid()
    {
        return AnimationData.Animation != null &&
                WaveLandVelocity > 0 &&
                WaveLandDuration > 0 &&
                InitialDashVelocity > 0 &&
                InitalDashDuration > 0 &&
                InputDashVelocity > 0 &&
                InputDashDuration > 0 &&
                HorizontalEndDashVelocity >= 0 &&
                VerticalEndDashVelocity >= 0;
    }
}
