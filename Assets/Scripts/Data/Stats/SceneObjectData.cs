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
    [Tooltip("This is the highest value a sceneObjects vertical air speed can be set to.\n" +
        "NOTE: The vertical air speed used is based on the sceneObejcts MassRatio")]
    public float AerialMaxYVelocityMax;
    [Tooltip("This is the lowest value a sceneObjects vertical air speed can be set to.\n" +
        "NOTE: The vertical air speed used is based on the sceneObejcts MassRatio")]
    public float AerialMaxYVelocityMin;
    [Tooltip("Rate at which the sceneObject will slow down vertically in the air")]
    public float AerialYDecceleration;
    [Tooltip("Multiplier for the gravity applied to the sceneObject.\n" +
        "NOTE: This is multiplied by the global gravity value, so a value of 1 means normal gravity, 0.5 means half gravity, and 2 means double gravity.")]
    public float GravityMultiplier = 1;

    [Header("Base Animations")]
    public BaseAnimationCollection AnimationCollection;

    [Header("Starting Movement Collection - Not Required")]
    public MovementDataCollection MovementCollection;

    public event Action OnChangedEvent;

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
            AerialMaxYVelocityMin <= 0 || AerialMaxYVelocityMax <= 0 || AerialMaxYVelocityMin > AerialMaxYVelocityMax)
            return false;

        // SceneObject must be able to stop moving
        if (GroundedDecceleration <= 0 || AerialXDecceleration <=0)
            return false;

        if (GravityMultiplier == 0) //NOTE: This would mean there is no gravity
            return false;

        if (AnimationCollection == null) 
            return false;

        return true;
    }


#if UNITY_EDITOR

    private void OnEnable()
    {
        RegisterToChangeEvents();
    }

    private void OnDisable()
    {
        UnregisterToChangeEvents();
    }

    private void RegisterToChangeEvents()
    {
        if (MovementCollection != null)
            MovementCollection.OnChangedEvent += DataChanged;
    }

    private void UnregisterToChangeEvents()
    {
        if (MovementCollection != null)
            MovementCollection.OnChangedEvent -= DataChanged;
    }

    private void DataChanged()
    {
        OnChangedEvent?.Invoke();
    }

    private void OnValidate()
    {
        DataChanged();

        UnregisterToChangeEvents();
        RegisterToChangeEvents();
    }

#endif
}
