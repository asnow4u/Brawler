using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KillZoneFactory : MonoBehaviour
{   
    public static KillZoneFactory instance;

    [SerializeField] private GameObject killZonePrefab;

    private void Start()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this);
        }
    }


    public KillZone Spawn(bool isRightDependent, bool isSolid = false, string objID = null, Action<SceneObject> listener = null)
    {
        GameObject obj = Instantiate(killZonePrefab);

        KillZone killZone = obj.GetComponent<KillZone>();
        killZone.SetUp(isRightDependent, isSolid, objID, listener);

        return killZone;
    }


}
