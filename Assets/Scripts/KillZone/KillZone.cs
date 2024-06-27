using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//NOTE: Solid is not yet implemented. Would want to make a seperate class for it having killzone be an abstract class
public enum KillZoneType { LeftGhost, RightGhost, LeftSolid, RightSolid }


[RequireComponent(typeof(Collider))]
public class KillZone : MonoBehaviour
{
    [SerializeField] private KillZoneType type;
    [SerializeField] private string killID;

    private const float OFFSCREENDISTANCE = 1.5f;
    private const float MINVELOCITY = 20f;


    #region Initialize

    public void Initialize(KillZoneType type, string uniqueID) 
    {
        this.type = type;
        this.killID = uniqueID;

        SetUpCollider();
    }


    private void SetUpCollider()
    {
        switch (type)
        {
            case KillZoneType.LeftGhost:
            case KillZoneType.RightGhost:
                GetComponent<Collider>().isTrigger = true;
                break;
        }
    }

    #endregion

    private void Update()
    {
        (Vector3 leftView, Vector3 rightView) cameraView = GetCameraViewport();

        if (type == KillZoneType.LeftGhost &&
            transform.position.x > cameraView.leftView.x)
        {
            transform.position = cameraView.leftView;
        }

        else if (type == KillZoneType.RightGhost &&
                 transform.position.x < cameraView.rightView.x)
        {
            transform.position = cameraView.rightView;            
        }                 
    }


    //NOTE: This should probably be moved to a diffent script dealing with the cameras
    private (Vector3, Vector3) GetCameraViewport()
    {
        Camera cam = Camera.main;
        float depth = Mathf.Abs(cam.transform.position.z);
        float cameraWidth = depth * Mathf.Tan((Camera.main.fieldOfView / 2) * Mathf.Deg2Rad) * Camera.main.aspect;

        Vector3 leftSide = new Vector3(cam.transform.position.x - cameraWidth, cam.transform.position.y, 0) - Vector3.one * OFFSCREENDISTANCE;
        Vector3 rightSide = new Vector3(cam.transform.position.x + cameraWidth, cam.transform.position.y, 0) + Vector3.one * OFFSCREENDISTANCE;

        return (leftSide, rightSide);
    }


    #region Collision

    private void OnTriggerEnter(Collider col)
    {
        if (col.gameObject.layer == LayerMask.NameToLayer("Ragdoll"))
        {            
            SceneObject hitSceneObject = col.gameObject.GetComponentInParent<SceneObject>();
            if (hitSceneObject != null)
                CollisionWithSceneObject(hitSceneObject);
        }
    }


    private void CollisionWithSceneObject(SceneObject sceneObject)
    {
        if (sceneObject.UniqueId == killID)
        {                        
            if (sceneObject.AnimationStateHandler.CurActionState == ActionState.HitStun)
            {                                
                //TODO: Spawn partical effect
                //TODO: Determine if player
                Destroy(sceneObject.gameObject);
            }
        }
    }

    #endregion
}
