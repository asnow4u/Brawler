using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquipmentHandler : IEquipment
{
    private SceneObject sceneObj;
    private WeaponCollection weaponCollection;

    public event Action<Weapon> OnWeaponChange;

    public EquipmentHandler(SceneObject obj)
    {
        this.sceneObj = obj;
        weaponCollection = new WeaponCollection(obj.transform.Find("Equipment"));
    }


    public void RegisterToWeaponChange(Action<Weapon> weaponChangeListener)
    {
        OnWeaponChange += weaponChangeListener;
    }


    public void AddWeapon(Weapon weapon)
    {
        throw new System.NotImplementedException();
    }

    public void RemoveWeapon(Weapon weapon)
    {
        throw new System.NotImplementedException();
    }


    public void SwapWeapon(int index)
    {
        Weapon weapon = weaponCollection.GetWeaponByIndex(index);

        if (weapon != null) 
        {
            weaponCollection.curWeapon = weapon;
            OnWeaponChange?.Invoke(weapon);
        }
    }
}
