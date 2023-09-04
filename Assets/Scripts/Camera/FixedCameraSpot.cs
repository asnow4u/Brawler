using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class FixedCameraSpot : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("PlayerEvent"))
        {
            if (Camera.main.TryGetComponent(out ICameraTarget cameraTarget))
            {
                cameraTarget.ChangeCameraState(CameraController.CameraState.Fixed, new Transform[] { transform });
        
                StartCoroutine(Delay());
            }
        }
    }

    private IEnumerator Delay()
    {
        yield return new WaitForSeconds(10f);


        //After awhile set reset camera back to player
        if (Camera.main.TryGetComponent(out ICameraTarget cameraTarget))
        {
            cameraTarget.ChangeCameraState(CameraController.CameraState.Follow);
            cameraTarget.ResetTargetFocusToPlayer();
        }


    }
}
