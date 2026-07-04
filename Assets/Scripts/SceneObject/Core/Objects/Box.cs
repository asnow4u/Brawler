using UnityEngine;

public class Box : SceneObject
{
    protected override void Awake()
    {
        base.Awake();

        // NOTE: This is a temporary solution to fix the box physics when stack atop one another.
        // The later goal would be to sleep the rb when the box is stationary atop a physical surface.
        rb.solverIterations = 16;              // position solver — contact stability
        rb.solverVelocityIterations = 4;       // velocity solver — restitution/tangential cleanup
        rb.maxDepenetrationVelocity = 0.5f;      // cap the separation fling
    }
}
