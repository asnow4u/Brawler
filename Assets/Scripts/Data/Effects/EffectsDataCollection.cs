using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Effects", menuName = "ScriptableObjects/SceneObject/Effects/Collection")]
public class EffectsDataCollection : ScriptableObject
{
    [Tooltip("Shield drawn while guarding. Requires an IGuard.")]
    public GuardShieldEffectData GuardShield;
    [Tooltip("Ring, beams and flash drawn on Counter. Requires an IGuard.")]
    public CounterBlastEffectData CounterBlast;
    [Tooltip("Trails drawn behind weapon hitboxes while attacking. Requires an IAttackHitBoxHandler and an IActionState.")]
    public WeaponTrailEffectData WeaponTrail;
    [Tooltip("Particles played at the hit point when hit. Requires an IHurtBoxHandler.")]
    public HitEffectData Hit;
    [Tooltip("Particles trailing the object while launched. Requires an ISOHurtBoxHandler and a Rigidbody.")]
    public LaunchEffectData Launch;

    #region Editor Updating

    public event Action OnChangedEvent;

    #if UNITY_EDITOR

        private void OnValidate()
        {
            if (Application.isPlaying)
                OnChangedEvent?.Invoke();
        }

    #endif

    #endregion
}
