using Game.SceneObjects;
using UnityEngine;


public interface ITakeDamage 
{
    public SceneObject SceneObject { get; }
    public bool CheckForImmunity(SceneObject attacker);
    public void AddDamage(float percent);
    public void RemoveDamage(float percent);
    public void ResetDamage();
    public void HitByAttack(Vector3 contactPoint, float influence, float attackDamage, float launchAngle);    
}

