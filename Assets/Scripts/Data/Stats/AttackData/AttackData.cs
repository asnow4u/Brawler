using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AttackData", menuName = "ScriptableObjects/SceneObject/Attack/Attack")]
public class AttackData : ScriptableObject
{
    [Header("Animation")]
    public AnimationClip Animation;

    [Header("Attack Details")]

    //NOTE: This is to help differentiate between attacks from different weapons. A attack from a dagger should not launch the same distance as a warhammers.
    //      Perhaps instead of each attack being defined, the influence could be defined based on the weapon type.
    [Tooltip("Influence defines the amount of knockback velocity that will be applied when the attack makes contact\n 0 is no additional knockback\n 1 is full knockback")]
    [Range(0, 1)]
    [SerializeField] public float Influence;

    [SerializeField] public AnimationCurve DamageCurve;

    [Tooltip("Launch angle is assuming that\n 0 is the forward\n 90 is the up\n 180 is backwards\n 270 is down")]
    [SerializeField] public float LaunchAngle;
}
