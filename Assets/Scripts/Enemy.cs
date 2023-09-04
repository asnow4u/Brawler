using SceneObj;
using UnityEngine;


public class Enemy : SceneObject
{
    protected override void Initialize()
    {
        base.Initialize();
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("PlayerEvent"))
        {
            if (Camera.main.TryGetComponent(out ICameraTarget cameraTarget))
            {
                cameraTarget.AddTargetFocus(transform);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("PlayerEvent"))
        {
            if (Camera.main.TryGetComponent(out ICameraTarget cameraTarget))
            {                
                cameraTarget.RemoveTargetFocus(transform);
            }
        }
    }
}
