using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackPoint : MonoBehaviour, IAttackPoint
{
    [SerializeField] private AttackCollider.Type collderType;
    private Collider collider;

    private event Action<IDamage> colliderHitEvent;

    public void Start()
    {
        tag = gameObject.tag;
        collider = GetComponent<Collider>();

        DisableColliders();
    }


    public AttackCollider.Type GetColliderType()
    {
        return collderType;
    }


    public void RegisterToHitEvent(Action<IDamage> callback)
    {
        colliderHitEvent += callback;
    }

    public void UnRegisterToHitEvent(Action<IDamage> callback)
    {
        colliderHitEvent -= callback;
    }


    public void EnableColliders()
    {           
        collider.enabled = true;                   
    }


    public void DisableColliders()
    {
        collider.enabled = false;
    }


    private void OnTriggerEnter(Collider col)
    {        
        if (col.gameObject.layer == LayerMask.NameToLayer("DamageHitBox"))
        {
            IDamage target = col.GetComponentInParent<IDamage>();
            colliderHitEvent?.Invoke(target);
        }
    }

}
