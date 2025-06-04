using Game.SceneObjects;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KillZoneFactory : MonoBehaviour
{   
    public static KillZoneFactory instance;

    [SerializeField] private GameObject deathEffectPrefab;

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


    public KillZone[] SpawnKillZones(Transform sceneObjectTransform)
    {
        GameObject leftKillZoneObj = new GameObject("LeftKillZone");
        HorizontalKillZone leftKillZone = leftKillZoneObj.AddComponent<HorizontalKillZone>();
        leftKillZone.Initialize(KillZoneType.Left, sceneObjectTransform, deathEffectPrefab);

        GameObject rightKillZoneObj = new GameObject("RightKillZone");
        HorizontalKillZone rightKillZone = rightKillZoneObj.AddComponent<HorizontalKillZone>();
        rightKillZone.Initialize(KillZoneType.Right, sceneObjectTransform, deathEffectPrefab);

        GameObject topKillZoneObj = new GameObject("TopKillZone");
        KillZone topKillZone = topKillZoneObj.AddComponent<KillZone>();
        topKillZone.transform.position = new Vector3(0, 30f, 0); //TEMP: Change this to be based off of some value
        topKillZone.Initialize(KillZoneType.Top, sceneObjectTransform);

        return new KillZone[] { rightKillZone, leftKillZone, topKillZone};
    }
}
