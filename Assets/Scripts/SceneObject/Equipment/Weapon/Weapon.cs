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

    public MovementCollection MovementCollection;
    public AttackCollection AttackCollection;

    private List<DamageCollider> damageColliders = new List<DamageCollider>();

    private AttackData curAttack;


    private void Start()
    {               
        if (MovementCollection == null)
            Debug.LogException(new NullReferenceException("Movement Collection On Weapon " + name + " Is Null!"), this);

        if (AttackCollection == null)
            Debug.LogException(new NullReferenceException("Attack Collection On Weapon " + name + " Is Null!"), this);
        
        foreach (DamageCollider damageCollider in GetComponentsInChildren<DamageCollider>())
            damageColliders.Add(damageCollider);
    }


    /// <summary>
    /// Enable colliders for given <paramref name="attackData"/>
    /// </summary>
    public void EnableCollidersForAttack(AttackData attackData, Action<ITakeDamage, Collider> attackHitCallback)
    {
        foreach (DamageCollider collider in damageColliders)
            collider.Enable(attackData.AttackAnimation, attackHitCallback);
    }


    /// <summary>
    /// Disable all colliders within collider collection
    /// </summary>
    public void DisableAllColliders()
    {
        foreach (DamageCollider collider in damageColliders)
            collider.Disable();
    }


    protected override void InputReceived(SceneObject sceneObj)
    {
        if (transform.GetChild(0).TryGetComponent(out Weapon weapon))
        {
            //Setup add sceneObject attackpoints to weapon
            if (sceneObj.AttackInputHandler != null)
            {
                //foreach (GameObject attackPointObj in sceneObj.AttackInputHandler.BaseAttackCollection.AttackPointCollection.AttackPoints)
                //{
                //    weapon.AttackCollection.AttackPointCollection.AttackPoints.Add(attackPointObj);
                //}
            }

            //sceneObj.EquipmentHandler.Weapons.AddWeapon(weapon);
        }

        sceneObj.InteractionHandler.UnregisterToInputEvent(InputReceived);
    }
}

