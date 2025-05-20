using Game.SceneObjects;
using System.Collections;
using UnityEditor.SceneManagement;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class JumpThroughPlatform : MonoBehaviour
{
    private BoxCollider platformCollider;

    //NOTE: will create a trigger box collider based on the original box collider
    private void Awake()
    {
        platformCollider = GetComponent<BoxCollider>();

        BoxCollider detectionCollider = gameObject.AddComponent<BoxCollider>();
        detectionCollider.isTrigger = true;
        detectionCollider.center = platformCollider.center;
        detectionCollider.size = platformCollider.size + platformCollider.size * 0.5f;
    }


    private void OnTriggerStay(Collider other)
    {        
        if (other.TryGetComponent(out SceneObject sceneObject))
        {
            if (sceneObject.Collider.bounds.min.y > transform.position.y && sceneObject.MovementInputHandler.VerticalInfluence >= 0)
                Physics.IgnoreCollision(platformCollider, other, false);
            else
                Physics.IgnoreCollision(platformCollider, other, true);
        }
    }
}
