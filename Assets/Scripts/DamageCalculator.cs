using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageCalculator : MonoBehaviour, ITakeDamage
{
    [SerializeField] private float damageTaken;

    public DamageCalculator()
    {
        damageTaken = 0;
    }

    public void AddPercent(float percent)
    {
        damageTaken += percent;
    }

    public void RemovePercent(float percent)
    {
        damageTaken -= percent;
    }

    public void ResetPercent()
    {
        damageTaken = 0;
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
