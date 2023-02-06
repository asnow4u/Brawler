using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDealDamage
{
    public void HitTarget(DamageCalculator target);
}


public interface ITakeDamage 
{
    public void AddPercent(float percent);

    public void RemovePercent(float percent);

    public void ResetPercent();

    public void ApplyForce(float mass, float basePower, Vector3 direction);
}

