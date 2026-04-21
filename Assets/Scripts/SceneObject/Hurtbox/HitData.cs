using System;
using UnityEngine;

public class HitData
{
    //ID of sceneObject that is attacking
    public Guid SceneObjectID;
    public float Influence;
    public float LauchAngle;
    public float Damage;
    public float StunTime;
    public Vector3 HitPoint;

    public HitData(Guid sceneObjectID, float influence, float lauchAngle, float damage, float stunTime, Vector3 hitPoint)
    {
        SceneObjectID = sceneObjectID;
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
            "\nInfluence: " + Influence +
            "\nLaunch Angle: " + LauchAngle +
            "\nDamage: " + Damage +
            "\nStunTime: " + StunTime;

        return str;
    }
}
