using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainFactory : MonoBehaviour
{
    public static TerrainFactory Instance;

    public GameObject EnvironmentObj;


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

    

    public void LoadEnvironment(GameObject terrainObj)
    {
        //TODO: Load environmentObjects randomly around terrainObj
    }
}
