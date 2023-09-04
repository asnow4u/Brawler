using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public enum CameraState
    {
        Follow,
        Fixed,
        Group
    }
    
    public CameraTarget target;

    private CameraState cameraState;

    private void Start()
    {
        if (target == null)
            Debug.LogError("Camera Target must be set");

        // Initialize the camera states
        cameraState = CameraState.Follow;
    }



    [ContextMenu("SetFollow")]
    public void SetFollow()
    {
        cameraState = CameraState.Follow;
    }

    [ContextMenu("SetFixed")]
    public void SetFixed()
    {
        cameraState = CameraState.Fixed;
    }

    [ContextMenu("SetGroup")]
    public void SetGroup()
    {
        cameraState = CameraState.Group;
    }
}
