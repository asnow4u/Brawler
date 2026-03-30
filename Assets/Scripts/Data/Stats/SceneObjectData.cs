using UnityEngine;

[CreateAssetMenu(fileName = "SceneObjectBase", menuName = "ScriptableObjects/SceneObject/BaseData")]

public class SceneObjectData : ScriptableObject
{
    public float MaxMass;
    public float MinMass;

    public float GroundedMaxVelocityMax;
    public float GroundedMaxVelocityMin;
    public float GroundedDecceleration;

    public float AerialMaxXVelocityMax;
    public float AerialMaxXVelocityMin;
    public float AerialXDecceleration;

    public float AerialMaxYVelocityMax;
    public float AerialMaxYVelocityMin;
    public float AerialYDecceleration;
    public float GravityMultiplier = 1;
    
    public BaseAnimationCollection AnimationCollection;

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
            AerialMaxYVelocityMin <= 0 || AerialMaxYVelocityMax <= 0 || AerialMaxYVelocityMin > AerialMaxYVelocityMax)
            return false;

        // SceneObject must be able to stop moving
        if (GroundedDecceleration <= 0 || AerialXDecceleration <=0)
            return false;

        if (GravityMultiplier == 0) //NOTE: This would mean there is no gravity
            return false;

        if (AnimationCollection == null) 
            return false;

        if (MovementCollection == null) 
            return false;

        return true;
    }
}
