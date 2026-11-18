using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Hit", menuName = "ScriptableObjects/SceneObject/Effects/Hit")]
public class HitEffectData : EffectData
{
    [Tooltip("Particle prefabs played at the hit point. Order based on AttackData.AttackType. The first entry plays on every hit.")]
    public List<ParticleSystem> HitEffects = new List<ParticleSystem>();
}
