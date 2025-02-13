using System;
using UnityEngine;


public class KnockbackCalculator
{
    const float minKnockBackAcceleration = 10f;
    const float exGrowth = 2.8f;

    public Vector3 CalculateKnockbackVelocity(float influence, float totalDamage, float launchAngle, Rigidbody rb)
    {
        float minForce = rb.mass * minKnockBackAcceleration;
        float damageForce = minForce + influence * (Mathf.Pow(totalDamage, exGrowth) / rb.mass);

        float xLaunch = Mathf.Cos(launchAngle * Mathf.Deg2Rad);
        float yLaunch = Mathf.Sin(launchAngle * Mathf.Deg2Rad);
        Vector3 initalLaunchDirection = new Vector2(xLaunch, yLaunch);            

        if (CheckForImmediateBounce(initalLaunchDirection, rb, out Vector3 bouncedDirection))
            initalLaunchDirection = bouncedDirection;

        return initalLaunchDirection * damageForce / rb.mass;
    }

    
    private bool CheckForImmediateBounce(Vector3 initialDirection, Rigidbody rb, out Vector3 bounceDirection)
    {                
        Bounds bounds = rb.GetComponent<Collider>().bounds;
        float max = bounds.max.y;
        float min = bounds.min.y;

        float originX = bounds.center.x;
        float originZ = bounds.center.z;

        Collider[] cols = Physics.OverlapBox(bounds.center, bounds.extents * 2f, Quaternion.identity, LayerMask.GetMask("Environment"));        

        foreach (Collider col in cols)
        {
            for (int i = 0; i < 5; i++)
            {
                Vector3 origin = new Vector3(bounds.center.x, max - (max - min) * i / 5, bounds.center.z);

                if (col.Raycast(new Ray(origin, initialDirection), out RaycastHit hit, 1f))
                {
                    bounceDirection = Vector3.Reflect(initialDirection, hit.normal);
                    return true;
                }
            }
        }

        bounceDirection = Vector3.zero;
        return false;
    }
}
