using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Weapon : MonoBehaviour
{
    [System.Serializable]
    public class Attack
    {
        public string name;
        public float damageOutput;
        public float basePower;
        public Vector3 forceDirection;
    }

    public List<Attack> attackList;
    protected Attack curAttack;

    protected Collider[] colliders;

    private void Start()
    {
        colliders = GetComponentsInChildren<Collider>();
    }

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


    public void ResetCurAttack()
    {
        curAttack = null;
    }


    public void EnableColliders()
    {
        if (curAttack != null)
        {
            foreach (Collider col in colliders)
            {
                col.enabled = true;
            }
        }
    }
    

    public void DisableColliders()
    {
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }
    }

    private void HitTarget(SceneObject target)
    {
        target.AddDamage(curAttack.damageOutput);
        target.ApplyForce(GetComponent<Rigidbody>().mass, curAttack.basePower, curAttack.forceDirection);
    }


    private void OnTriggerEnter(Collider col)
    {
        SceneObject target = col.GetComponent<SceneObject>();

        if (target != null)
        {
            HitTarget(target);
        }
    }


}
