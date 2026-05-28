using System;
using UnityEngine;

[CreateAssetMenu(fileName = "SceneObjectBase", menuName = "ScriptableObjects/SceneObject/BaseData")]

public class SceneObjectData : ScriptableObject
{
    [Header("Mass")]
    [Tooltip("The maximum amount of mass a sceneObject can have.\n" +
        "NOTE: The ratio of currentMass over MaxMass impacts all movement options.")]
    public float MaxMass;
    [Tooltip("The minimum amount of mass a sceneObject can have, also the starting mass.\n" +
        "NOTE: The ratio of currentMass over MaxMass impacts all movement options.")]
    public float MinMass;

    [Header("Grounded Movement")]
    [Tooltip("This is the highest value a sceneObjects ground speed can be set to.\n" +
        "NOTE: The ground speed used is based on the sceneObejcts MassRatio")]
    public float GroundedMaxVelocityMax;
    [Tooltip("This is the lowest value a sceneObjects ground speed can be set to.\n" +
        "NOTE: The ground speed used is based on the sceneObejcts MassRatio")]
    public float GroundedMaxVelocityMin;
    [Tooltip("Rate at which the sceneObject will slow down")]
    public float GroundedDecceleration;

    [Header("Aerial Movement")]
    [Tooltip("This is the highest value a sceneObjects horizontal air speed can be set to.\n" +
        "NOTE: The horizontal air speed used is based on the sceneObejcts MassRatio")]
    public float AerialMaxXVelocityMax;
    [Tooltip("This is the lowest value a sceneObjects horizontal air speed can be set to.\n" +
        "NOTE: The horizontal air speed used is based on the sceneObejcts MassRatio")]
    public float AerialMaxXVelocityMin;
    [Tooltip("Rate at which the sceneObject will slow down horizontally in the air")]
    public float AerialXDecceleration;
    [Tooltip("This is the highest value a sceneObjects vertical air speed can be set to while rising.\n" +
        "NOTE: The vertical air speed used is based on the sceneObejcts MassRatio")]
    public float AerialMaxRisingVelocityMax;
    [Tooltip("This is the lowest value a sceneObjects vertical air speed can be set to while rising.\n" +
        "NOTE: The vertical air speed used is based on the sceneObejcts MassRatio")]
    public float AerialMaxRisingVelocityMin;
    [Tooltip("Rate at which the sceneObject will slow down vertically in the air while rising")]
    public float AerialRisingDecceleration;
    [Tooltip("This is the highest value a sceneObjects vertical air speed can be set to while falling.\n" +
        "NOTE: The vertical air speed used is based on the sceneObejcts MassRatio")]
    public float AerialMaxFallVelocityMax;
    [Tooltip("This is the lowest value a sceneObjects vertical air speed can be set to while falling.\n" +
        "NOTE: The vertical air speed used is based on the sceneObejcts MassRatio")]
    public float AerialMaxFallVelocityMin;

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
        if (MaxMass <= 0 || MinMass <= 0 || MinMass > MaxMass)
            return false;

        // SceneObject must have be able to move (NOTE: This can be when getting hit)
        if (GroundedMaxVelocityMin <= 0 || GroundedMaxVelocityMax <= 0 || GroundedMaxVelocityMin > GroundedMaxVelocityMax )
            return false;

        // SceneObject must be able to move in the air (NOTE: This can be when getting hit)
        if (AerialMaxXVelocityMin <= 0 || AerialMaxXVelocityMax <= 0 || AerialMaxXVelocityMin > AerialMaxXVelocityMax ||
            AerialMaxRisingVelocityMin <= 0 || AerialMaxRisingVelocityMax <= 0 || AerialMaxRisingVelocityMin > AerialMaxRisingVelocityMax)
            return false;

        // SceneObject must be able to stop moving
        if (GroundedDecceleration <= 0 || AerialXDecceleration <=0)
            return false;

        if (GravityRaising == 0 || GravityFalling == 0 || GravityFastFalling == 0 || GravityHitStunTravel == 0 || GravityHitStunRecovery == 0) //NOTE: This would mean there is no gravity
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
