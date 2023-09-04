using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICameraTarget
{
    public void ChangeCameraState(CameraController.CameraState state, Transform[] targets = null);
    public void ResetTargetFocusToPlayer();
    public void AddTargetFocus(Transform target);
    public void RemoveTargetFocus(Transform target);
}
