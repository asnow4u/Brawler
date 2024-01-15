using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainFactory : MonoBehaviour
{
    [Serializable]
    public class EnvironmentObjectData
    {
        public GameObject EnvironmentObj;

        public int MinSpawn;
        public int MaxSpawn;
    }
    
    public static TerrainFactory Instance;
    public List<EnvironmentObjectData> envObjects;

    public GameObject TerrainObj;

    public bool DebugStart;


    void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    private void Update()
    {
        if (DebugStart)
        {
            LoadEnvironment(TerrainObj);
            DebugStart = false;
        }
    }



    public void LoadEnvironment(GameObject terrainObj)
    {
        for (int i = 0; i < envObjects.Count; i++)
        {
            int spawnNum = UnityEngine.Random.Range(envObjects[i].MinSpawn, envObjects[i].MaxSpawn);
            MeshCollider collider = terrainObj.GetComponent<MeshCollider>();
            Bounds bound = collider.bounds;

            for (int j = 0; j < spawnNum; j++)
            {
                float randX = UnityEngine.Random.Range(bound.min.x, bound.max.x);
                float randZ = UnityEngine.Random.Range(bound.min.z, bound.max.z);

                Vector3 rayOrigin = new Vector3(randX, terrainObj.transform.position.y + 20, randZ);

                if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 100, LayerMask.GetMask("Environment")))
               {
                    SpawnEnvironmentalObject(hit, envObjects[i]);
               }
            }
        }
    }


    private void SpawnEnvironmentalObject(RaycastHit hit, EnvironmentObjectData envObject)
    {
        Renderer render = envObject.EnvironmentObj.GetComponent<Renderer>();
        Bounds heightBound = render.bounds;
        float envHeight = heightBound.size.y / 2;
        float originDiff = envObject.EnvironmentObj.transform.position.y - heightBound.center.y;
        
        Vector3 spawnPos = new Vector3(hit.point.x, hit.point.y + originDiff + envHeight, hit.point.z);       

        Instantiate(envObject.EnvironmentObj, spawnPos, Quaternion.identity);
    }
}
