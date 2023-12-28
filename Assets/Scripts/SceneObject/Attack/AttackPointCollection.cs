using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using UnityEngine;

[Serializable]
public class AttackPointCollection
{
    //TODO: Need a way to get the sceneobjs attack points when weapon is picked up

    public List<GameObject> AttackPoints;

    public AttackPointCollection(List<GameObject> attackPoints)
    {
        this.AttackPoints = attackPoints;
    }

    private bool TryGetAttackPointFromType(AttackColliderType colliderType, out IAttackPoint attackPoint)
    {
        foreach (GameObject point in AttackPoints)
        {
            if (point.TryGetComponent(out attackPoint))
            {
                if (attackPoint.GetColliderType() == colliderType)
                {
                    return true;
                }
            }
        }

        attackPoint = null;
        return false;
    }


    public void EnableAttackColliders(List<AttackColliderType> colliderTypes, Action<IDamage> OnHitEvent)
    {
        foreach (AttackColliderType colliderType in colliderTypes)
        {
            if (TryGetAttackPointFromType(colliderType, out IAttackPoint attackPoint))
            {
                attackPoint.RegisterToHitEvent(OnHitEvent);
                attackPoint.EnableColliders();
            }
        }
    }


    public void DisableAttackColliders(List<AttackColliderType> colliderTypes, Action<IDamage> OnHitEvent)
    {
        foreach (AttackColliderType colliderType in colliderTypes)
        {
            if (TryGetAttackPointFromType(colliderType, out IAttackPoint attackPoint))
            {
                attackPoint.UnRegisterToHitEvent(OnHitEvent);
                attackPoint.DisableColliders();
            }
        }
    }


  


}
