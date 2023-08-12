using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class AttackCollection : MonoBehaviour
{
    public List<Attack> attacks;

    [System.Serializable]
    public class Attack
    {
        public AttackType.Type type;        
        public AnimationClip animationClip; //TODO: animation clips will later be made up of sceneObjectName + weaponName + attackType
        public List<string> attackPointTags;

        [Header("Attack Details")]
        [Range(0f, 20f)] 
        public float damageOutput;
        [Range(200f, 1000f)]
        public float baseKnockback;
        [Range(0f, 1f)]
        public float damageInfluence;

        //NOTE: This should always be faced to the right direction
        public Vector2 directionalForce;
        private bool isFacingRightDir;

        public void SetAttackDirection(bool isRight)
        {
            isFacingRightDir = isRight;
        }


        public void OnHit(IDamage target)
        {
            Debug.Log("Attack: Hit with " + type.ToString());

            target.AddDamage(damageOutput);
            
            target.ApplyForceBasedOnDamage(baseKnockback, damageInfluence, new Vector2(directionalForce.x * (isFacingRightDir ? 1 : -1), directionalForce.y));
        }
    }


    public bool GetAttackByType(AttackType.Type attackType, out Attack requestedAttack)
    {
        requestedAttack = null;

        foreach (Attack attack in attacks)
        {
            if (attack.type == attackType)
            {
                requestedAttack = attack;
                return true;
            }
        }

        return false;
    }

    public bool GetAttackByClip(AnimationClip clip, out Attack requestedAttack)
    {
        requestedAttack = null;

        foreach (Attack attack in attacks)
        {
            if (attack.animationClip.name == clip.name)
            {
                requestedAttack = attack;
                return true;
            }
        }

        return false;
    }
}

