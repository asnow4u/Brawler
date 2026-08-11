using System;
using UnityEngine;

public class HitData
{    
    public float BaseForce;
    public float Influence;
    public float LaunchAngle;
    public float Damage;
    public float StunTime;
    public Vector3 HitPoint;
    public int EffectIndex; // Defines what effects get played

    public HitData(float baseForce, float influence, float lauchAngle, float damage, float stunTime, Vector3 hitPoint, int effectIndex)
    {
        BaseForce = baseForce;
        Influence = influence;
        LaunchAngle = lauchAngle;
        Damage = damage;
        StunTime = stunTime;
        HitPoint = hitPoint;
        EffectIndex = effectIndex;
    }

    public bool IsValid()
    {
        return BaseForce >= 0f && Influence >= 0f && Damage >= 0f && StunTime >= 0f;
    }

    public override string ToString()
    {
        string str = "HitData:" +
            "\nBaseForce: " + BaseForce +
            "\nInfluence: " + Influence +
            "\nLaunch Angle: " + LaunchAngle +
            "\nDamage: " + Damage +
            "\nStunTime: " + StunTime +
            "\nEffectIndex: " + EffectIndex;

        return str;
    }
}

public class SceneObjectHitData : HitData
{
    //ID of sceneObject that is attacking
    public Guid SceneObjectAttackerID;

    public SceneObjectHitData(Guid sceneObjectAttackerID, HitData hitData) : base(hitData.BaseForce, hitData.Influence, hitData.LaunchAngle, hitData.Damage, hitData.StunTime, hitData.HitPoint, hitData.EffectIndex)
    {
        SceneObjectAttackerID = sceneObjectAttackerID;
    }

    public override string ToString()
    {
        string str = base.ToString() + 
            "\nSceneObjectID: " + SceneObjectAttackerID;
        
        return str;
    }
}


public class SceneObjectCollisionHitData : SceneObjectHitData
{
    public SceneObjectCollisionHitData(Guid sceneObjectAttackerID, float baseForce, HitData hitData) : base(sceneObjectAttackerID, hitData)
    {
        BaseForce = baseForce;
    }
}


public class KnockBackHitData : HitData
{
    public Vector3 KnockBackVelocity;

    public KnockBackHitData(Vector3 knockBackVelocity, HitData hitData) : base(hitData.BaseForce, hitData.Influence, hitData.LaunchAngle, hitData.Damage, hitData.StunTime, hitData.HitPoint, hitData.EffectIndex)
    {
        KnockBackVelocity = knockBackVelocity;
    }

    public override string ToString()
    {
        string str = base.ToString() +
            "\nKnockBack Velocity: " + KnockBackVelocity;

        return str;
    }
}
