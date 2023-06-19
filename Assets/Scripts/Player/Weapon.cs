using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Weapon : MonoBehaviour
{
    private AttackHandler handler;

    private void Start()
    {
        handler = GetComponent<AttackHandler>();
    }
}

