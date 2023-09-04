using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour, ICameraTarget
{
    public enum CameraState { Follow, Fixed }    

    [SerializeField] private Transform player;
    [SerializeField] private CameraTarget cameraTarget;

    private CameraState cameraState;

    private void Start()
    {
        if (cameraTarget == null)
            Debug.LogError("Camera Target must be set");

        // Initialize the camera states
        ChangeCameraState(CameraState.Follow, new Transform[] { player });
    }


    public void ChangeCameraState(CameraState state, Transform[] targets = null)
    {
        switch (state)
        {
            case CameraState.Follow:
                cameraState = CameraState.Follow;                
                break;

            case CameraState.Fixed:
                cameraState = CameraState.Fixed;                
                break;
        }

        if (targets != null && targets.Length > 0)
        {
            cameraTarget.Targets.Clear();
            cameraTarget.Targets.AddRange(targets);
        }
            
    }


    public void ResetTargetFocusToPlayer()
    {
        if (cameraState == CameraState.Follow)
        {
            cameraTarget.Targets.Clear();
            cameraTarget.Targets.AddRange(new Transform[] { player });
        }
    }


    public void AddTargetFocus(Transform target)
    {
        if (cameraState == CameraState.Follow)
            cameraTarget.Targets.Add(target);
    }


    public void RemoveTargetFocus(Transform target)
    {
        if (cameraState == CameraState.Follow && cameraTarget.Targets.Contains(target))
            cameraTarget.Targets.Remove(target);
    }
}
