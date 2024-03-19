using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public interface IDamage 
{
    public void AddDamage(float percent);

    public void RemoveDamage(float percent);

    public void ResetDamage();

    public void ApplyForceBasedOnDamage(float baseKnockBack, float damageInfluence, Vector2 direction);

    public IEnumerator ApplyHitStun(float totalForce);
}

