using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainFactory : MonoBehaviour
{
    public static TerrainFactory Instance;

    public GameObject EnvironmentObj;
    public GameObject TerrainObj;

    public int MaxSpawn;
    public int MinSpawn;

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
        //TODO: Load environmentObjects randomly around terrainObj
        int spawnNum = Random.Range(MinSpawn, MaxSpawn);

        MeshCollider collider = terrainObj.GetComponent<MeshCollider>();
        Bounds bound = collider.bounds;

        for (int i = 0; i < spawnNum; i++)
        {
            float randX = Random.Range(-bound.size.x/2, bound.size.x/2);
            float randZ = Random.Range(-bound.size.z/2, bound.size.z/2);

            Vector3 rayOrigin = new Vector3(randX, terrainObj.transform.position.y + 20, randZ);

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 100, LayerMask.GetMask("Environment")))
            {
                SpawnEnvironmentalObject(hit);
            }
        }
    }

    private void SpawnEnvironmentalObject(RaycastHit hit)
    {
        Renderer render = EnvironmentObj.GetComponent<Renderer>();
        Bounds heightBound = render.bounds;
        float envHeight = heightBound.size.y / 2;
        float originDiff = EnvironmentObj.transform.position.y - heightBound.center.y;
        
        Vector3 spawnPos = new Vector3(hit.point.x, hit.point.y + originDiff + envHeight, hit.point.z);
        


        Instantiate(EnvironmentObj, spawnPos, Quaternion.identity);
    }
}
