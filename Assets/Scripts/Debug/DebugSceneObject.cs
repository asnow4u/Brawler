using Game.SceneObjects;
using Game.SceneObjects.ActionStates;
using RayAssets;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
public static class DebugSceneObject
{
    [MenuItem("Debug/Spawn/Base SceneObject/Player")]
    public static void DebugSpawnBasePlayer()
    {
        GameObject player = CreateSceneObject("Player");
        GameObject mesh = CreateMeshObject(player.transform);
        GameObject inventory = CreateInventoryObject(player.transform);

        GameObject eventCollider = new GameObject("EventCollider");
        eventCollider.transform.SetParent(player.transform);
        eventCollider.AddComponent<BoxCollider>();
    }


    [MenuItem("Debug/Spawn/Base SceneObject/Enemy")]
    public static void DebugSpawnBaseEnemy()
    {
        GameObject enemy = CreateSceneObject("Enemy");
        GameObject mesh = CreateMeshObject(enemy.transform);
        GameObject inventory = CreateInventoryObject(enemy.transform);
    }


    [MenuItem("Debug/Spawn/Base SceneObject/Environment")]
    public static void DebugSpawnBaseEnvironment()
    {
        GameObject environmentObj = CreateSceneObject("Environment");
        GameObject meshObj = CreateMeshObject(environmentObj.transform);        
    }


    private static GameObject CreateSceneObject(string name)
    {
        GameObject obj = new GameObject(name);
        obj.AddComponent<EnvironmentObject>();
        obj.AddComponent<CapsuleCollider>();
        return obj;
    }


    private static GameObject CreateMeshObject(Transform parent)
    {
        GameObject mesh = new GameObject("Mesh");
        mesh.transform.SetParent(parent);
        mesh.AddComponent<ActionStateHandler>();

        return mesh;
    }


    private static GameObject CreateInventoryObject(Transform parent)
    {
        GameObject inventory = new GameObject("Inventory");
        inventory.transform.SetParent(parent);        

        return inventory;
    }
}
#endif
