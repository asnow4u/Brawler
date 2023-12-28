using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Tilemaps.Tile;

public class WeaponCollection : MonoBehaviour, IWeaponCollection
{
    [SerializeField] private List<Weapon> weapons = new List<Weapon>();
    [SerializeField] private Weapon curWeapon = null;

    [SerializeField] private Transform weaponHolder;

    private List<IAttackPoint> bodyAttackPoints;

    public event Action<Weapon> WeaponChangedEvent;

    public void Initialize(SceneObject sceneObj)
    {
        bodyAttackPoints = new List<IAttackPoint>(sceneObj.GetComponentsInChildren<IAttackPoint>());
        
        foreach (Transform child in transform)
        {
            if (child.TryGetComponent(out Weapon weapon))
            {
                AddWeapon(weapon);
            }
        }
    }

    #region Weapon

    private Weapon GetWeaponByIndex(int index)
    {
        if (weapons.Count > index)
        {
            return weapons[index];
        }

        return null;
    }


    private void SetWeaponToInventory(Weapon weapon)
    {
        weapon.transform.SetParent(transform);
        ResetTransform(weapon.transform);
        weapon.gameObject.SetActive(false);
    }   


    private void SetWeaponToHolder(Weapon weapon)
    {        
        weapon.transform.SetParent(weaponHolder);
        ResetTransform(weapon.transform);
        weapon.gameObject.SetActive(true);  
    }


    public Weapon GetCurWeapon()
    {
        return curWeapon;
    }


    public void AddWeapon(Weapon weapon)
    {
        if (!weapons.Contains(weapon))
        {
            weapons.Add(weapon);
            SetWeaponToInventory(weapon);

            SwapWeaponTo(weapons.Count -1);            
        }
    }


    public void RemoveWeapon(Weapon weapon)
    {
        if (weapons.Contains(weapon))
        {
            weapons.Remove(weapon);
        }
    }


    public void SwapWeaponTo(int index)
    {
        Weapon weapon = GetWeaponByIndex(index);

        if (weapon != null)
        {           
            if (curWeapon != null) 
                SetWeaponToInventory(curWeapon);


            SetWeaponToHolder(weapon);            

            curWeapon = weapon;                
        }

        WeaponChangedEvent?.Invoke(weapon);
    }


    private void ResetTransform(Transform trans)
    {
        trans.localPosition = Vector3.zero;
        trans.localRotation = Quaternion.identity;
    }

    #endregion
}

