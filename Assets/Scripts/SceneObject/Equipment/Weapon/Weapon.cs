using Game.Interactable;
using Game.SceneObjects;
using Game.SceneObjects.Attack;
using Game.SceneObjects.Movement;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : Interactable
{    
    public WeaponType Type;
    public float Mass;
    public MovementCollection MovementCollection;
    public AttackCollection AttackCollection;

    private List<AttackDamageCollider> damageColliders = new List<AttackDamageCollider>();    

    protected override void Awake()
    {
        base.Awake();

        if (MovementCollection == null)
            Debug.LogException(new NullReferenceException("Movement Collection On Weapon " + name + " Is Null!"), this);

        if (AttackCollection == null)
            Debug.LogException(new NullReferenceException("Attack Collection On Weapon " + name + " Is Null!"), this);
        
        foreach (AttackDamageCollider damageCollider in GetComponentsInChildren<AttackDamageCollider>())
            damageColliders.Add(damageCollider);
    }


    /// <summary>
    /// Enable colliders for given <paramref name="attackData"/>
    /// </summary>
    public void EnableCollidersForAttack(AttackData attackData, Action<ITakeDamage, Collider> attackHitCallback)
    {
        foreach (AttackDamageCollider collider in damageColliders)
            collider.Enable(attackData.Animation, attackHitCallback);
    }


    /// <summary>
    /// Disable all colliders within collider collection
    /// </summary>
    public void DisableAllColliders()
    {
        foreach (AttackDamageCollider collider in damageColliders)
            collider.Disable();
    }
}

