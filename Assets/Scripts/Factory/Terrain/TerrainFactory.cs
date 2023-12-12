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
            float randX = Random.Range(0, bound.size.x);
            float randZ = Random.Range(0, bound.size.z);

            Instantiate(EnvironmentObj, new Vector3(randX, 0, randZ), Quaternion.identity);
        }
    }
}
