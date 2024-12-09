using System;
using System.Collections.Generic;
using UnityEngine;

public class WeaponCollection : MonoBehaviour
{
    [SerializeField] private List<Weapon> weapons = new List<Weapon>();

    #region Getter

    /// <summary>
    /// Get <paramref name="weapon"/> from collection by <paramref name="index"/>
    /// </summary>
    public bool TryGetWeaponByIndex(int index, out Weapon weapon)
    {
        weapon = null;

        if (weapons.Count > index)
            weapon = weapons[index];

        return weapon != null;
    }

    #endregion


    /// <summary>
    /// Initialize weapon collection by adding all child weapons
    /// </summary>
    public void Initialize()
    {   
        foreach (Weapon weapon in GetComponentsInChildren<Weapon>())
            AddWeapon(weapon);
    }


    #region Collection

    /// <summary>
    /// Add <paramref name="weapon"/> to collection
    /// </summary>
    public void AddWeapon(Weapon weapon)
    {
        if (!weapons.Contains(weapon))
        {
            weapons.Add(weapon);
            SetWeaponToInventory(weapon);
        }
    }


    /// <summary>
    /// Remove <paramref name="weapon"/> from collection
    /// </summary>
    public void RemoveWeapon(Weapon weapon)
    {
        if (weapons.Contains(weapon))
            weapons.Remove(weapon);
    }

    #endregion


    #region Inventory

    private void ResetTransform(Transform trans)
    {
        trans.localPosition = Vector3.zero;
        trans.localRotation = Quaternion.identity;
    }

    /// <summary>
    /// Set <paramref name="weapon"/> to inventory
    /// </summary>
    private void SetWeaponToInventory(Weapon weapon)
    {
        weapon.transform.SetParent(transform);
        ResetTransform(weapon.transform);
        weapon.gameObject.SetActive(false);
    }


    
    //private void SetWeaponToHolder(Weapon weapon)
    //{        
    //    weapon.transform.SetParent(weaponHolder);
    //    ResetTransform(weapon.transform);
    //    weapon.gameObject.SetActive(true);  
    //} 


    //public void SwapWeaponTo(int index)
    //{
    //    Weapon weapon = GetWeaponByIndex(index);

    //    if (weapon != null)
    //    {           
    //        if (curWeapon != null) 
    //            SetWeaponToInventory(curWeapon);


    //        SetWeaponToHolder(weapon);            

    //        curWeapon = weapon;                
    //    }

    //    WeaponChangedEvent?.Invoke(weapon);
    //}


   

    #endregion
}

