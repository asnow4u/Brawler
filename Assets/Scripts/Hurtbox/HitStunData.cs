using System;
using UnityEngine;

public class HitStunData
{
    public Guid SceneObjectID;
    public Vector3 LaunchVelocity;

    public HitStunData(Guid sceneObjectID, Vector3 launchVelocity)
    {
        SceneObjectID = sceneObjectID;
        LaunchVelocity = launchVelocity;
    }
}
