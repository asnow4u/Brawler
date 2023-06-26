using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponCollection
{
    public Weapon curWeapon;
    
    private List<Weapon> weapons = new List<Weapon>();

    public WeaponCollection(Transform equipment)
    {
        if (equipment == null)
        {
            Debug.LogError("Object does not contain Equipment");
        }

        Weapon firstWeapon = equipment.GetComponent<Weapon>();
        weapons.Add(firstWeapon);
        curWeapon = firstWeapon;
    }                


    public Weapon GetWeaponByIndex(int index)
    {
        return weapons[index];
    }    
}

