using Game.SceneObjects;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class ClimbableEdge : MonoBehaviour
{   
    private BoxCollider collider;
    private const float requiredPercentageAboveEdge = 0.5f;

    private void Awake()
    {
        collider = GetComponent<BoxCollider>();
        collider.isTrigger = true;
    }


    private void OnTriggerStay(Collider other)
    {
        if (other.TryGetComponent(out SceneObject sceneObejct))
        {
            if (sceneObejct.MovementInputHandler == null) return;
            if (other.bounds.max.y <= collider.bounds.max.y) return;

            //Calculate how much of the other collider is above this edge
            float edgeHeightDifference = other.bounds.max.y - collider.bounds.max.y;
            float percentageAboveEdge = edgeHeightDifference / other.bounds.size.y;

            //Determine if scene object is heading towards the edge
            bool edgeOnRightSide = transform.position.x > sceneObejct.transform.position.x;
            bool correctInfluence = edgeOnRightSide && sceneObejct.MovementInputHandler.HorizontalInfluence > 0 ||
                                            !edgeOnRightSide && sceneObejct.MovementInputHandler.HorizontalInfluence < 0;

            if (percentageAboveEdge >= requiredPercentageAboveEdge && correctInfluence)
                sceneObejct.MovementInputHandler.UpdateEdgeClimb();
        }
    }
}
