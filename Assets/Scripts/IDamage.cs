using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public interface IDamage 
{
    public void AddDamage(float percent);

    public void RemoveDamage(float percent);

    public void ResetDamaget();

    public void ApplyForce(float mass, float basePower, Vector3 direction);
}

