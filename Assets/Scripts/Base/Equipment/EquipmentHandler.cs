using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquipmentHandler : IEquipment
{
    private SceneObject sceneObj;

    private Weapon curWeapon;
    public IWeapon WeaponCollection { get; private set; }

    public EquipmentHandler(SceneObject obj)
    {
        this.sceneObj = obj;

        Transform equipment = obj.transform.Find("Equipment");

        if (equipment != null)
        {
            WeaponCollection = new WeaponHandler(equipment);

            SwapWeapon(0);
        }
    }


    public void SwapWeapon(int index)
    {
        if (WeaponCollection != null)
        {
            Weapon weapon = WeaponCollection.GetWeaponByIndex(index);

            if (weapon != null) 
            {
                curWeapon = weapon;
            }        
        }
    }


    public MovementCollection GetCurrentWeaponMovementCollection()
    {
        return curWeapon.MovementCollection;
    }


    public AttackCollection GetCurrentWeaponAttackCollection()
    {
        return curWeapon.AttackCollection;
    }

}
