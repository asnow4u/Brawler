using UnityEngine;

//NOTE: Line the gameobject's position with the corner of the edge

[RequireComponent(typeof(BoxCollider))]
public class ClimbableEdge : MonoBehaviour
{   
    private bool isRight;
    public bool IsRight => isRight;

    private void Awake()
    {
        Collider collider = GetComponent<BoxCollider>();
        collider.isTrigger = true;

        isRight = transform.parent.transform.position.x < transform.position.x;
    }
}
