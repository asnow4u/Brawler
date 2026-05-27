using System;
using UnityEngine;

public class HitData
{
    //ID of sceneObject that is attacking
    public Guid SceneObjectID;
    public int AttackType;
    public float Influence;
    public float LauchAngle;
    public float Damage;
    public float StunTime;
    public Vector3 HitPoint;

    public HitData(Guid sceneObjectID, int attackType, float influence, float lauchAngle, float damage, float stunTime, Vector3 hitPoint)
    {
        SceneObjectID = sceneObjectID;
        AttackType = attackType;
        Influence = influence;
        LauchAngle = lauchAngle;
        Damage = damage;
        StunTime = stunTime;
        HitPoint = hitPoint;
    }

    public override string ToString()
    {
        string str = "HitData:" +
            "\nSceneObjectID: " + SceneObjectID +
            "\nAttackType: " + AttackType +
            "\nInfluence: " + Influence +
            "\nLaunch Angle: " + LauchAngle +
            "\nDamage: " + Damage +
            "\nStunTime: " + StunTime;

        return str;
    }
}


public class KnockBackHitData : HitData
{
    public Vector3 KnockBackVelocity;

    public KnockBackHitData(HitData hitData, Vector3 knockBackVelocity) : base(hitData.SceneObjectID, hitData.AttackType, hitData.Influence, hitData.LauchAngle, hitData.Damage, hitData.StunTime, hitData.HitPoint)
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
