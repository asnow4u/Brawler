using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(DamageCalculator))]
public class Enemy : MonoBehaviour
{
    private DamageCalculator damage;

    private void Start()
    {
        damage = GetComponent<DamageCalculator>();
    }
}
