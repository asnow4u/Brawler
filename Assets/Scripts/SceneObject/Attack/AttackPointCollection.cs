using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using UnityEngine;

[Serializable]
public class AttackPointCollection
{
    [SerializeField] private List<AttackPoint> curAttackPoints;
    [SerializeField] private List<AttackPoint> baseAttackPoints;

    public List<AttackPoint> CurAttackPoints => curAttackPoints;


    #region Initialize

    public AttackPointCollection(GameObject root)
    {
        baseAttackPoints = new List<AttackPoint>(); 
        curAttackPoints = new List<AttackPoint>();
        foreach (AttackPoint attackPoint in root.GetComponentsInChildren<AttackPoint>())
        {
            baseAttackPoints.Add(attackPoint);
            curAttackPoints.Add(attackPoint);
        }
    }

    #endregion


    #region Getters

    private bool TryGetAttackPointFromType(AttackColliderType colliderType, out AttackPoint foundAttackPoint)
    {
        foreach (AttackPoint attackPoint in curAttackPoints)
        {
            if (attackPoint.ColliderType == colliderType)
            {
                foundAttackPoint = attackPoint;
                return true;
            }            
        }

        foundAttackPoint = null;
        return false;
    }

    #endregion


    #region Update AttackPointCollection

    /// <summary>
    /// Add additional attackpoints from another collection to this collection
    /// </summary>
    /// <param name="attackPointCollection"></param>
    public void AddAttackPointsFrom(AttackPointCollection attackPointCollection)
    {
        curAttackPoints.AddRange(attackPointCollection.CurAttackPoints);
    }


    /// <summary>
    /// Return the curAttackPoint list back to what it originally was
    /// </summary>
    public void ResetAttackPointCollection()
    {
        curAttackPoints = baseAttackPoints;
    }

    #endregion


    #region Colliders

    public void EnableCollidersForAttack(AttackData attackData)
    {
        foreach (AttackColliderType colliderType in attackData.ColliderType)
        {
            if (TryGetAttackPointFromType(colliderType, out AttackPoint attackPoint))
            {                
                attackPoint.PrepForAttack(attackData);
            }
        }
    }


    public void DisableCollidersForAttack(AttackData attackData)
    {
        foreach (AttackColliderType colliderType in attackData.ColliderType)
        {
            if (TryGetAttackPointFromType(colliderType, out AttackPoint attackPoint))
            {
                attackPoint.Reset();
            }
        }
    }

    #endregion
}
