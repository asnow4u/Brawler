using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HurtEffect", menuName = "ScriptableObjects/SceneObject/Effects")]
public class SceneObjectEffects : ScriptableObject
{
    [Header("Hurt")]
    [Tooltip("Effects based on when getting hit. (Order based on AttackData.AttackType)")]
    public List<ParticleSystem> HitEffects;

    [Header("Launch")]
    public ParticleSystem LaunchEffect;

}
