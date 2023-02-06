using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fist : AttackCalculator
{
    public bool isRight;
    private Collider collider;

    // Start is called before the first frame update
    void Start()
    {
        collider = GetComponent<Collider>();
    }


    public void EnableCollider()
    {
        collider.enabled = true;
    }


    public void DisableCollider()
    {
        collider.enabled = false;
    }


    private void OnTriggerEnter(Collider col)
    {
        DamageCalculator target = col.GetComponent<DamageCalculator>();

        if (target != null)
        {
            HitTarget(target);
        }
    }
}
