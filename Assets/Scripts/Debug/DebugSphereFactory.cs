using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class DebugSphereFactory
{
    
    private const string path = "Debug/DebugSphere";


    public static void SpawnDebugSphere(Vector3 pos, string name = "DebugSphere", float radius = 0.2f)
    {
        GameObject spherePrefab = Resources.Load(path) as GameObject;
        GameObject debugSpehre = GameObject.Instantiate(spherePrefab);
        debugSpehre.name = name;
        debugSpehre.transform.position = pos;
        debugSpehre.transform.localScale = Vector3.one * radius;

    }
}
