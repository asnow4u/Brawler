using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class KnockbackCalculator
{
    const float minKnockBackAcceleration = 4f;

    public Vector3 CalculateForceKnockBack(float influence, float totalDamage, float mass, float launchAngle)
    {        
        float minForce = mass * minKnockBackAcceleration;
        float damageForce = minForce + influence * (Mathf.Pow(totalDamage, 2.75f) / mass);

        float xLaunch = Mathf.Cos(launchAngle * Mathf.Deg2Rad);
        float yLaunch = Mathf.Sin(launchAngle * Mathf.Deg2Rad);
        Vector3 launchDirection = new Vector2(xLaunch, yLaunch);            

        return launchDirection * damageForce;
    }
}
