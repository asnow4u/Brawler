using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class FixedFightSpot : MonoBehaviour
{
    //TODO: Create the nessisary spawners for the enemies needed
    //TODO: Track enemies left

    List<KillZone> killZones = new List<KillZone>();

    private bool isRunning = false;
    private bool isFinished = false;

  


    private void SpawnKillZones()
    {       
        killZones.Add(KillZoneFactory.instance.Spawn(true, true, null, OnEnemyDestroyed));
        killZones.Add(KillZoneFactory.instance.Spawn(false, true, null, OnEnemyDestroyed));
    }


    private void OnEnemyDestroyed(SceneObject sceneObj)
    {
        isFinished = true;
    }


    private void OnTriggerEnter(Collider other)
    {
        if (!isRunning && other.gameObject.layer == LayerMask.NameToLayer("PlayerEvent"))
        {
            if (Camera.main.TryGetComponent(out ICameraTarget cameraTarget))
            {
                isRunning = true;
                cameraTarget.ChangeCameraState(CameraController.CameraState.Fixed, new Transform[] { transform });
        
                SpawnKillZones();

                //Temp
                StartCoroutine(Delay());
            }
        }
    }


    private IEnumerator Delay()
    {
        while (!isFinished)
        {
            yield return null;
        }        

        //After awhile set reset camera back to player
        if (Camera.main.TryGetComponent(out ICameraTarget cameraTarget))
        {
            cameraTarget.ChangeCameraState(CameraController.CameraState.Follow);
            cameraTarget.ResetTargetFocusToPlayer();
        }

        foreach (KillZone zone in new List<KillZone>(killZones))
        {
            Destroy(zone.gameObject);
        }
        killZones.Clear();

        isRunning = false;
        isFinished = false;
    }
}
