using Game.SceneObjects;
using Game.SceneObjects.ActionStates;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//NOTE: Solid is not yet implemented. Would want to make a seperate class for it having killzone be an abstract class
public enum KillZoneType { Left, Right, LeftSolid, RightSolid }


public class KillZone : MonoBehaviour
{
    [SerializeField] private KillZoneType type;
    [SerializeField] private Transform objTransform;

    private const float OFFSCREENDISTANCE = 1.5f;


    #region Initialize

    public void Initialize(KillZoneType type, Transform objTransform) 
    {
        this.type = type;
        this.objTransform = objTransform;

        UpdatePosition();
    }

    #endregion

    private void Update()
    {
        UpdatePosition();
        CheckForKill();
    }


    /// <summary>
    /// Update the position of the killzone based on the camera view
    /// </summary>
    private void UpdatePosition()
    {
        (Vector3 leftView, Vector3 rightView) cameraView = GetCameraViewport();

        if (type == KillZoneType.Left && transform.position.x > cameraView.leftView.x)
            transform.position = cameraView.leftView;

        else if (type == KillZoneType.Right && transform.position.x < cameraView.rightView.x)
            transform.position = cameraView.rightView;
    }


    /// <summary>
    /// Get the left and right side of the camera view
    /// </summary>
    private (Vector3, Vector3) GetCameraViewport()
    {
        Camera cam = Camera.main;
        float depth = Mathf.Abs(cam.transform.position.z);
        float cameraWidth = depth * Mathf.Tan((Camera.main.fieldOfView / 2) * Mathf.Deg2Rad) * Camera.main.aspect;

        Vector3 leftSide = new Vector3(cam.transform.position.x - cameraWidth, cam.transform.position.y, 0) - Vector3.one * OFFSCREENDISTANCE;
        Vector3 rightSide = new Vector3(cam.transform.position.x + cameraWidth, cam.transform.position.y, 0) + Vector3.one * OFFSCREENDISTANCE;

        return (leftSide, rightSide);
    }


    /// <summary>
    /// Check if the objects transform has passed the killzone and should be destroyed
    /// </summary>
    private void CheckForKill()
    {
        if (objTransform != null)
        {
            if (type == KillZoneType.Left && objTransform.position.x < transform.position.x)
                KillObject();

            else if (type == KillZoneType.Right && objTransform.position.x > transform.position.x)
                KillObject();
        }
    }


    /// <summary>
    /// Kill the object that has passed the killzone
    /// </summary>
    private void KillObject()
    {
        if (objTransform.TryGetComponent(out SceneObject sceneObject))
        {
            if (sceneObject is Player player)
            {
                //TODO: Handle player death
            }
            else
                Destroy(sceneObject.gameObject);
        }
    }
}
