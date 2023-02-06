using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackCalculator : MonoBehaviour, IDealDamage
{
    [System.Serializable]
    public class Attack
    {
        public string name;
        public AnimationClip animationClip;
        public float damageOutput;
        public float basePower;
        public Vector3 forceDirection;
    }

    public List<Attack> attackList;
    
    private Attack curAttack;

    public void SetCurAttack(string attackName)
    {        
        foreach (Attack attack in attackList)
        {
            if (attack.name == attackName)
            {
                curAttack = attack;
            }
        }        
    }


    public void HitTarget(DamageCalculator target)
    {
        target.AddPercent(curAttack.damageOutput);
        target.ApplyForce(GetComponent<Rigidbody>().mass, curAttack.basePower, curAttack.forceDirection);
    }
}
