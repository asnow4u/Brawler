using System;
using System.Collections.Generic;
using UnityEngine;

internal class Weapon : Item, IWeapon
{    
    [SerializeField] private WeaponData weaponData;
    public WeaponData WeaponData => weaponData;

    [SerializeField] private Transform gripPoint;
    public Transform GripPoint => gripPoint;

    protected override void Awake()
    {
        if (weaponData == null)
            Debug.LogError("Weapon Data not set for " + name, this);

        if (gripPoint == null)
            Debug.LogError("Weapon Grip Point not set for " + name, this);
    }
}

