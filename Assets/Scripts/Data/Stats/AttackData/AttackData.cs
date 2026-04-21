using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AttackData", menuName = "ScriptableObjects/SceneObject/Attack/Attack")]
public class AttackData : ScriptableObject
{
    [Header("Animation")]
    public AnimationClip Animation;

    [Header("Attack Details")]    
    [Tooltip("Influence defines the amount of knockback velocity that will be applied when the attack makes contact." +
        "\n 0 is no additional knockback," +
        "\n 1 is full knockback")]
    [Range(0, 1)]
    public float Influence;
    
    [Tooltip("Normalized scale of damage based on animation length.")]
    public AnimationCurve DamageCurve;

    [Tooltip("Launch angle that the target will be launched at when hit." +
        "\n 0 is the forward," +
        "\n 90 is the up," +
        "\n 180 is backwards," +
        "\n 270 is down")]
    public float LaunchAngle;

    [Tooltip("Amount of time(Sec) that attacker and target are stunned when hit." +
        "\nThis helps add enphisis and weight to the attack.")]
    public float HitStunTime = 0.1f;
}
