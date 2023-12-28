using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WeaponType { Base, Sword, Spear, GreatSword, Hammer };

public class Weapon : MonoBehaviour
{
    public WeaponType Type;

    public MovementCollection MovementCollection;
    public AttackCollection AttackCollection;
}

