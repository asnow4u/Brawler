using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class DebugSphereFactory
{
    
    private const string path = "Debug/DebugSphere";


    public static void SpawnDebugSphere(Vector3 pos, float radius = 0.2f)
    {
        GameObject spherePrefab = Resources.Load(path) as GameObject;
        GameObject debugSpehre = GameObject.Instantiate(spherePrefab);
        debugSpehre.transform.position = pos;
        debugSpehre.transform.localScale = Vector3.one * radius;
    }
}
