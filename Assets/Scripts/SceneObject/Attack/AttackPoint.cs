using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AttackColliderType
{
    //Player
    PlayerRightFist, PlayerLeftFist, PlayerRightFoot, PlayerLeftFoot,

    //Enemy

    //Weapons
    Sword
}

[Serializable]
public class AttackPoint : MonoBehaviour
{
    [SerializeField] private AttackColliderType collderType;
    private Collider collider;
    private AttackData curAttackData;

    //Getters
    public AttackColliderType ColliderType => collderType;


    public void Start()
    {
        tag = gameObject.tag;
        collider = GetComponent<Collider>();

        Reset();
    }


    public void PrepForAttack(AttackData attackData)
    {
        collider.enabled = true;
        curAttackData = attackData;
    }


    public void Reset()
    {
        collider.enabled = false;
        curAttackData = null;
    }



    private void OnTriggerEnter(Collider col)
    {        
        if (col.gameObject.layer == LayerMask.NameToLayer("DamageHitBox"))
        {
            ITakeDamage target = col.GetComponentInParent<ITakeDamage>();

            if (curAttackData != null)
            {
                //TODO: use attackData for attack

            }            
        }
    }

}
