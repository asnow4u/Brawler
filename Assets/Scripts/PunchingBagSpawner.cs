using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PunchingBagSpawner : MonoBehaviour
{
    public GameObject bagPrefab;

    public bool SpawnPunchingBag;

    private void Update()
    {
        if (SpawnPunchingBag)
        {
            SpawnPunchingBag = false;
            Spawn();
        }
    }



    public void Spawn()
    {
        Instantiate(bagPrefab, transform);
    }

}
