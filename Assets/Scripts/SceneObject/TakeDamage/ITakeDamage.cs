using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public interface ITakeDamage 
{
    public void AddDamage(float percent);
    public void RemoveDamage(float percent);
    public void ResetDamage();
    public void HitByAttack(AttackColliderType attackType, Rigidbody rb, float attackDamage, float launchAngle);    
}

