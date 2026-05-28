using System;
using UnityEngine;

[CreateAssetMenu(fileName = "SceneObjectBase", menuName = "ScriptableObjects/SceneObject/BaseData")]

public class SceneObjectData : ScriptableObject
{
    [Header("Mass")]
    [Tooltip("The maximum amount of mass a sceneObject can have.")]
    public float Mass;

    [Header("Grounded Movement")]
    [Tooltip("This is the highest value a sceneObjects ground speed can be set to.")]
    public float GroundedMaxVelocity;
    [Tooltip("Rate at which the sceneObject will slow down")]
    public float GroundedDecceleration;

    [Header("Aerial Movement")]
    [Tooltip("This is the highest value a sceneObjects horizontal air speed can be set to.")]
    public float AerialMaxXVelocity;
    [Tooltip("Rate at which the sceneObject will slow down horizontally in the air")]
    public float AerialXDecceleration;
    [Tooltip("This is the highest value a sceneObjects vertical air speed can be set to while rising.")]
    public float AerialMaxRisingVelocity;
    [Tooltip("Rate at which the sceneObject will slow down vertically in the air while rising")]
    public float AerialRisingDecceleration;
    [Tooltip("This is the highest value a sceneObjects vertical air speed can be set to while falling.")]
    public float AerialMaxFallVelocity;

    [Header("Gravity")]
    [Tooltip("The gravity force applied while moving up")]
    public float GravityRaising;
    [Tooltip("The gravity force applied while moving down")]
    public float GravityFalling;
    [Tooltip("The gravity force applied while fast falling")]
    public float GravityFastFalling;
    [Tooltip("The gravity force used while in hitstun during the travel state")]
    public float GravityHitStunTravel;
    [Tooltip("The gravity force used while in hitstun during the recovery state" +
    "\nNote: this is the gravity force at the apex of the trajectory. Should be slow")]
    public float GravityHitStunRecovery;

    [Header("Base Animations")]
    public BaseAnimationCollection AnimationCollection;

    [Header("Starting Movement Collection - Not Required")]
    public MovementDataCollection MovementCollection;

    public bool IsValid()
    {
        // SceneObject must have mass
        if (Mass <= 0)
            return false;

        // SceneObject must have be able to move (NOTE: This can be when getting hit)
        if (GroundedMaxVelocity <= 0)
            return false;

        // SceneObject must be able to move in the air (NOTE: This can be when getting hit)
        if (AerialMaxXVelocity <= 0 || AerialMaxRisingVelocity <= 0)
            return false;

        // SceneObject must be able to stop moving
        if (GroundedDecceleration <= 0 || AerialXDecceleration <=0)
            return false;

        if (GravityRaising <= 0 || GravityFalling <= 0 || GravityFastFalling <= 0 || GravityHitStunTravel <= 0 || GravityHitStunRecovery <= 0)
            return false;

        if (AnimationCollection == null) 
            return false;

        return true;
    }

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
