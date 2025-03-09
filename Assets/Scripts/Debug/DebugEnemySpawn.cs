using Game.SceneObjects;
using Game.SceneObjects.Damage;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
public class DebugEnemySpawn : MonoBehaviour
{
    [Header("Spawner Properties")]
    [Tooltip("The prefab of the enemy to spawn")]
    [SerializeField] private GameObject EnemyPrefab;
    [Tooltip("The number of enemies to spawn")]
    [SerializeField] private int enemyCount = 0;
    [Tooltip("Determine if the camera should follow with the camera")]
    [SerializeField] private bool followCamera = false;

    [Header("SceneObject Properties")]
    [Tooltip("The starting percent of the enemy")]
    [SerializeField] private float startingPercent = 0;

    private List<GameObject> enemies = new List<GameObject>();


    private void Update()
    {
        UpdatePosition();
        CheckList();

        if (enemyCount > 0 && enemies.Count < enemyCount)
            SpawnEnemys();
    }


    /// <summary>
    /// Update the position of the spawner based on the camera view
    /// </summary>
    private void UpdatePosition()
    {
        if (followCamera)
        {
            Camera cam = Camera.main;
            Vector3 camPos = cam.transform.position;
            camPos.z = 0;
            transform.position = camPos;
        }
    }


    /// <summary>
    /// Check the list of enemies and remove any that are null
    /// </summary>
    private void CheckList()
    {
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] == null)
                enemies.RemoveAt(i);
        }
    }


    /// <summary>
    /// Spawn enemies based on the enemy count
    /// </summary>
    private void SpawnEnemys()
    {
        int numToSpawn = enemyCount - enemies.Count;

        for (int i=0; i < numToSpawn; i++)
        {
            GameObject enemy = Instantiate(EnemyPrefab);
            enemy.transform.position = transform.position;

            //Set starting percent
            DamageHandler damageHandler = enemy.GetComponent<DamageHandler>();
            damageHandler.AddDamage(startingPercent);

            enemies.Add(enemy);
        }
    }
}

#endif
