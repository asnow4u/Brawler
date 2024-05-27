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


    public KillZone[] SpawnGhostKillZones(string objID)
    {
        GameObject leftKillZoneObj = Instantiate(killZonePrefab);
        KillZone leftKillZone = leftKillZoneObj.GetComponent<KillZone>();
        leftKillZone.Initialize(KillZoneType.LeftGhost, objID);
                
        GameObject rightKillZoneObj = Instantiate(killZonePrefab);
        KillZone rightKillZone = rightKillZoneObj.GetComponent<KillZone>();
        rightKillZone.Initialize(KillZoneType.RightGhost, objID);

        return new KillZone[] { rightKillZone, leftKillZone};
    }
}
