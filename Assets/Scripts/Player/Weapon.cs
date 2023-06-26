using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MovementCollection), typeof(AttackCollection))]
public class Weapon : MonoBehaviour
{
    public MovementCollection movementCollection;
    public AttackCollection attackCollection;

}

