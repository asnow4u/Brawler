using Game.SceneObjects;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class ClimbableEdge : MonoBehaviour
{   
    private BoxCollider collider;
    private bool isRight;
    public bool IsRight => isRight;

    private void Awake()
    {
        collider = GetComponent<BoxCollider>();
        collider.isTrigger = true;

        isRight = transform.parent.transform.position.x < transform.position.x;
    }


    private void OnTriggerStay(Collider other)
    {
        if (other.TryGetComponent(out SceneObject sceneObejct))
        {
            if (sceneObejct.MovementInputHandler != null)
                sceneObejct.MovementInputHandler.UpdateEdgeClimb(this);
        }
    }
}
