using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public abstract class SceneObject : MonoBehaviour, IDamage
{
    protected float damageTaken;

    [SerializeField]
    protected List<Weapon> currentWeapons;


    public void ResetDamaget()
    {
        damageTaken = 0;
    }

    public void AddDamage(float percent)
    {
        damageTaken += percent;
    }   

    public void RemoveDamage(float percent)
    {
        damageTaken -= percent;
    }   

    public void ApplyForce(float mass, float basePower, Vector3 direction)
    {

        //Grab here in case change to mass
        Rigidbody rb = GetComponent<Rigidbody>();

        float force = mass * (basePower * damageTaken);

        rb.AddForce(force * direction, ForceMode.Impulse);

        //TODO: Set drag amount to 0.1 ish
        //Drag should reset when ground is touched or immobile is finished
    }
}
