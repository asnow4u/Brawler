using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using UnityEngine;

[Serializable]
public class ColliderCollection
{
    [SerializeField] private List<AttackCollider> attackColliders = new List<AttackCollider>();

    public List<AttackCollider> AttackColliders => attackColliders;

    
    public ColliderCollection(GameObject weaponGO)
    {
        foreach (AttackCollider attackPoint in weaponGO.GetComponentsInChildren<AttackCollider>())
            attackColliders.Add(attackPoint);
    }    
}
