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



    private void OnTriggerEnter(Collider col)
    {
        Debug.LogWarning("AttackPoint Collision with " + col.gameObject.name, gameObject);
        if (col.gameObject.layer == LayerMask.NameToLayer("DamageHitBox"))
        {
            ITakeDamage hitTarget = col.GetComponentInParent<ITakeDamage>();
            Debug.LogWarning("ITAKEDAMAGE found");

            if (curAttackData != null)
            {
                SceneObject sceneObject = GetComponentInParent<SceneObject>();
                if (sceneObject != null)
                {
                    int curFrame = sceneObject.AnimationStateHandler.GetCurrentFrameOfCurAnimation();

                    hitTarget.HitByAttack(attackType, curAttackData, curFrame);
                }
            }
        }
    }

}
