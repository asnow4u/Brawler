using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquipmentHandler : IEquipment
{
    private SceneObject sceneObj;

    public IWeaponCollection Weapons { get; private set; }
    //ItemCollection
    //EquipmentCollection


    public EquipmentHandler(SceneObject obj)
    {
        this.sceneObj = obj;

        SetUpWeaponsContainer(obj);
        SetUpItemsContainer(obj); 
        SetUpEquipmentContainer(obj);
    }


    private void SetUpWeaponsContainer(SceneObject obj)
    {
        Weapons = obj.GetComponentInChildren<WeaponCollection>();

        if (Weapons != null)
            Weapons.Initialize(obj);
    }

    private void SetUpItemsContainer(SceneObject obj)
    {
        Transform itemContainer = obj.transform.Find("Items");

        if (itemContainer != null)
        {

        }
    }

    private void SetUpEquipmentContainer(SceneObject obj) 
    {
        Transform equipmentContainer = obj.transform.Find("Equipment");

        if (equipmentContainer != null)
        {
            
        }
    }   
}
