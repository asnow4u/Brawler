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
    [SerializeField] private AttackColliderType attackType;
    private Collider collider;
    private AttackData curAttackData;

    //Getters
    public AttackColliderType ColliderType => attackType;


    public void Start()
    {
        tag = gameObject.tag;
        collider = GetComponent<Collider>();

        Reset();
    }


    public void SetupForAttack(AttackData attackData)
    {
        collider.enabled = true;
        curAttackData = attackData;
    }


    public void Reset()
    {
        collider.enabled = false;
        curAttackData = null;
    }


    //TODO: 
    //Not every sceneobject will have a ragdoll, thus cant only search for ragdoll layer
    //Avoid doing anything other than sending data over to ITakeDamage. (DamageBubble included)

    private void OnTriggerEnter(Collider col)
    {        
        if (col.gameObject.layer == LayerMask.NameToLayer("Ragdoll") &&
            col.gameObject.TryGetComponent(out Rigidbody targetRB))
        {
            ITakeDamage hitTarget = col.GetComponentInParent<ITakeDamage>();

            if (hitTarget != null)
            {
                //Current Attack
                if (curAttackData != null)
                {
                    //Get SceneObject
                    SceneObject sceneObject = GetComponentInParent<SceneObject>();
                    if (sceneObject != null)
                    {
                        int curFrame = sceneObject.AnimationStateHandler.GetCurrentFrameOfCurAnimation();
                        float launchAngle = curAttackData.GetAttackLaunchAngle(curFrame);

                        //Reverse launch angle
                        if (!sceneObject.IsFacingRightDirection())
                            launchAngle = 180 - launchAngle;

                        hitTarget.HitByAttack(attackType, targetRB, curAttackData.GetAttackDamage(curFrame), launchAngle);

                        //Damage bubble                    
                        UIFactory.Instance.SpawnDamageBubble(col.ClosestPoint(transform.position), curAttackData.GetAttackDamage(curFrame));

                        Debug.Log(sceneObject.gameObject.name + " hit " + targetRB.gameObject.name + " with " + attackType);
                    }
                }
            }
        }
    }
}
