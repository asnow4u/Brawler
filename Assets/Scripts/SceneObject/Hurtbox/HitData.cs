using System;
using UnityEngine;

public class HitData
{    
    public float Influence;
    public float LauchAngle;
    public float Damage;
    public float StunTime;
    public Vector3 HitPoint;
    public int EffectIndex; // Defines what effects get played

    public HitData(float influence, float lauchAngle, float damage, float stunTime, Vector3 hitPoint, int effectIndex)
    {
        Influence = influence;
        LauchAngle = lauchAngle;
        Damage = damage;
        StunTime = stunTime;
        HitPoint = hitPoint;
        EffectIndex = effectIndex;
    }

    public override string ToString()
    {
        string str = "HitData:" +
            //"\nSceneObjectID: " + SceneObjectID +
            "\nInfluence: " + Influence +
            "\nLaunch Angle: " + LauchAngle +
            "\nDamage: " + Damage +
            "\nStunTime: " + StunTime +
            "\nAttackType: " + EffectIndex;

        return str;
    }
}

public class SceneObjectHitData : HitData
{
    //ID of sceneObject that is attacking
    public Guid SceneObjectAttackerID;

    public SceneObjectHitData(Guid sceneObjectAttackerID, HitData hitData) : base(hitData.Influence, hitData.LauchAngle, hitData.Damage, hitData.StunTime, hitData.HitPoint, hitData.EffectIndex)
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


public class KnockBackHitData : HitData
{
    public Vector3 KnockBackVelocity;

    public KnockBackHitData(Vector3 knockBackVelocity, HitData hitData) : base(hitData.Influence, hitData.LauchAngle, hitData.Damage, hitData.StunTime, hitData.HitPoint, hitData.EffectIndex)
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
