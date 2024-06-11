using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class KnockbackCalculator
{
    const float minKnockBackForce = 700f;

    public Vector3 CalculateForceKnockBack(AttackColliderType attackType, float totalDamage, float mass, float launchAngle)
    {        
        float damageForce = minKnockBackForce + (GetForceInfluence(attackType) * (Mathf.Pow(totalDamage, 2.75f) / mass));

        float xLaunch = Mathf.Cos(launchAngle * Mathf.Deg2Rad);
        float yLaunch = Mathf.Sin(launchAngle * Mathf.Deg2Rad);
        Vector3 launchDirection = new Vector2(xLaunch, yLaunch);            

        return launchDirection * damageForce;
    }


    private float GetForceInfluence(AttackColliderType attackType)
    {
        switch (attackType)
        {
            case AttackColliderType.PlayerRightFist:
            case AttackColliderType.PlayerLeftFist:
            case AttackColliderType.PlayerRightFoot:
            case AttackColliderType.PlayerLeftFoot:

                return 1f;

            case AttackColliderType.Sword:

                return 0.4f;

            default:
                return 1;
        }
    }
}
