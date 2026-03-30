using System;
using UnityEngine;

public class HitData
{
    //ID of sceneObject that is attacking
    public Guid SceneObjectID;
    public float Influence;
    public float LauchAngle;
    public float Damage;

    public HitData(Guid sceneObjectID, float influence, float lauchAngle, float damage)
    {
        SceneObjectID = sceneObjectID;
        Influence = influence;
        LauchAngle = lauchAngle;
        Damage = damage;
    }
}
