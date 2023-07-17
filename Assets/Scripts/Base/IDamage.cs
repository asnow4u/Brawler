using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public interface IDamage 
{
    public void AddDamage(float percent);

    public void RemoveDamage(float percent);

    public void ResetDamaget();

    public void ApplyForceBasedOnDamage(float baseKnockBack, float damageInfluence, Vector2 direction);
}

