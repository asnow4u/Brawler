using System;
using System.Collections.Generic;
using UnityEngine;

internal class Weapon : Item, IWeapon
{    
    [SerializeField] private WeaponData weaponData;
    public WeaponData WeaponData => weaponData;

    protected override void Awake()
    {
        if (weaponData == null)
            Debug.LogError("Weapon Data not set for " + name, this);        
    }
}

