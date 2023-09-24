using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class DebugWeaponSpawn
{

#if UNITY_EDITOR

    [MenuItem("Debug/Spawn/Weapon/Sword")]
    public static void DebugSpawnSword()
    {
        WeaponFactory.instance.SpawnSword();
    }

#endif

}
