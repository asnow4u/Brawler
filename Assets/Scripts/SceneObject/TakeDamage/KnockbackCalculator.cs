using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class KnockbackCalculator
{
    const float minKnockBackForce = 700f;

    public Vector3 CalculateForceKnockBack(float influence, float totalDamage, float mass, float launchAngle)
    {        
        float damageForce = minKnockBackForce + influence * (Mathf.Pow(totalDamage, 2.75f) / mass);

        float xLaunch = Mathf.Cos(launchAngle * Mathf.Deg2Rad);
        float yLaunch = Mathf.Sin(launchAngle * Mathf.Deg2Rad);
        Vector3 launchDirection = new Vector2(xLaunch, yLaunch);            

        return launchDirection * damageForce;
    }
}
