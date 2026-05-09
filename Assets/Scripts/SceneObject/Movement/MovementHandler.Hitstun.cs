using System.Collections.Generic;
using UnityEngine;

internal partial class MovementHandler
{
    //Bounce Properties
    [Header("Bounce")]
    [SerializeField] private float bounceDegrade = 0.9f;
    private const float minGroundBounceVelocity = 20f;


    private void CheckForHitStunBounce()
    {
        Bounds bounds = sceneObject.Bounds;
        Vector3 direction = rb.linearVelocity.normalized;
        float distance = rb.linearVelocity.magnitude * Time.fixedDeltaTime;

        //Get bound points for bounce check
        List<Vector3> boundPoints = new List<Vector3>();
        float centralZ = (bounds.max.z + bounds.min.z) / 2;

        //X Direction
        if (direction.x > 0)
        {
            boundPoints.Add(new Vector3(bounds.max.x, bounds.max.y, centralZ)); // Top-right
            boundPoints.Add(new Vector3(bounds.max.x, bounds.center.y, centralZ));  // Right-center
            boundPoints.Add(new Vector3(bounds.max.x, bounds.min.y, centralZ)); // Bottom-right
        }
        else if (direction.x < 0)
        {
            boundPoints.Add(new Vector3(bounds.min.x, bounds.max.y, centralZ)); // Top-left
            boundPoints.Add(new Vector3(bounds.min.x, bounds.center.y, centralZ));  // Left-center
            boundPoints.Add(new Vector3(bounds.min.x, bounds.min.y, centralZ)); // Bottom-left
        }

        //Y Direction
        if (direction.y > 0)
        {
            boundPoints.Add(new Vector3(bounds.min.x, bounds.max.y, centralZ)); // Top-left
            boundPoints.Add(new Vector3(bounds.center.x, bounds.max.y, centralZ)); // Top-center
            boundPoints.Add(new Vector3(bounds.max.x, bounds.max.y, centralZ)); // Top-right
        }
        else if (direction.y < 0)
        {
            boundPoints.Add(new Vector3(bounds.min.x, bounds.min.y, centralZ)); // Bottom-left
            boundPoints.Add(new Vector3(bounds.center.x, bounds.min.y, centralZ)); // Bottom-center
            boundPoints.Add(new Vector3(bounds.max.x, bounds.min.y, centralZ)); // Bottom-right
        }

        if (boundPoints.Count == 0)
            return;

        //Environment check
        List<Vector3> hitNormals = new List<Vector3>();
        foreach (var point in boundPoints)
        { 
            if (Physics.Raycast(point, direction, out RaycastHit hit, distance, LayerMask.GetMask("Environment")))
                hitNormals.Add(hit.normal);
        }

        if (hitNormals.Count == 0)
            return;

        Vector3 bounceVelocity = CalculateBounceVelocity(hitNormals);
        rb.linearVelocity = bounceVelocity;
    }

    private Vector3 CalculateBounceVelocity(List<Vector3> hitNormals)
    {
        if (hitNormals == null || hitNormals.Count == 0)
            return Vector3.zero;

        //Calculate bounce velocity
        Vector3 averageNormal = Vector3.zero;

        foreach (var normal in hitNormals)
            averageNormal += normal;
        averageNormal /= hitNormals.Count;

        Vector3 bounceVelocity = Vector3.Reflect(rb.linearVelocity, averageNormal) * bounceDegrade;

        //Prevent small bounces on ground
        if (bounceVelocity.magnitude < minGroundBounceVelocity &&
            rb.linearVelocity.y < 0 && bounceVelocity.y > 0)
        {
            return Vector3.zero;
        }

        return bounceVelocity;
    }

}
