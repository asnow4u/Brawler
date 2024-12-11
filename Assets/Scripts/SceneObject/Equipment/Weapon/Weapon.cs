using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public WeaponType Type;

    public MovementCollection MovementCollection;
    public AttackCollection AttackCollection;
    public ColliderCollection ColliderCollection;

    private AttackData curAttack;


    private void Start()
    {
        if (MovementCollection == null)
            throw new System.NullReferenceException("Movement Collection On Weapon " + name + " Is Null!");

        if (AttackCollection == null)
            throw new System.NullReferenceException("Attack Collection On Weapon " + name + " Is Null!");

        ColliderCollection = new ColliderCollection(gameObject);
    }


    /// <summary>
    /// Enable colliders for given <paramref name="attackData"/>
    /// </summary>
    public void EnableCollidersForAttack(AttackData attackData, Action<ITakeDamage, Collider> attackHitCallback)
    {
        foreach (AttackCollider collider in ColliderCollection.AttackColliders)
            collider.Enable(attackData.AttackAnimation, attackHitCallback);
    }


    /// <summary>
    /// Disable all colliders within collider collection
    /// </summary>
    public void DisableAllColliders()
    {
        foreach (AttackCollider collider in ColliderCollection.AttackColliders)
            collider.Disable();
    }   
}

