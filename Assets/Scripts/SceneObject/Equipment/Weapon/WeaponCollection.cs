using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponCollection : MonoBehaviour, IWeaponCollection
{
    [SerializeField] private List<Weapon> weapons = new List<Weapon>();
    [SerializeField] private Weapon curWeapon;

    [SerializeField] private Transform weaponHolder;

    private void Start()
    {         
        foreach (Transform child in transform)
        {
            if (child.TryGetComponent(out Weapon weapon))
            {
                AddWeapon(weapon);
            }
        }
    }

    #region Weapon

    public Weapon GetWeaponByIndex(int index)
    {
        if (weapons.Count > index)
        {
            return weapons[index];
        }

        return null;
    }


    public void AddWeapon(Weapon weapon)
    {
        if (!weapons.Contains(weapon))
        {
            weapons.Add(weapon);

            if (weapon.weaponType != Weapon.Type.Base)
                SetWeaponToEquipment(weapon);

            SwapWeaponTo(weapons.Count -1);            
        }
    }


    private void SetWeaponToEquipment(Weapon weapon)
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
                SetWeaponToEquipment(curWeapon);

            if (weapon.weaponType != Weapon.Type.Base)
            {
                SetWeaponToHolder(weapon);
            }

            curWeapon = weapon;                
        }      
    }


    private void ResetTransform(Transform trans)
    {
        trans.localPosition = Vector3.zero;
        trans.localRotation = Quaternion.identity;
    }


    public MovementCollection GetMovementCollection()
    {
        return curWeapon.MovementCollection;
    }


    public AttackCollection GetAttackCollection()
    {
        return curWeapon.AttackCollection;
    }

    #endregion


    #region Attack Points

    private bool FindAttackPointFrom(string tag, out IAttackPoint attackPoint)
    {
        attackPoint = null;

        foreach (IAttackPoint point in curWeapon.AttackPoints)
        {
            if (point.GetTag()  == tag)
            {
                attackPoint = point;
                return true;
            }
        }

        return false;
    }


    public void EnableAttackColliders(List<string> colliderTags, Action<IDamage> OnHitEvent)
    {
        foreach(string tag in colliderTags)
        {
            if (FindAttackPointFrom(tag, out IAttackPoint attackPoint))
            {
                attackPoint.RegisterToHitEvent(OnHitEvent);
                attackPoint.EnableColliders();
            }
        }
    }


    public void DisableAttackColliders(List<string> colliderTags, Action<IDamage> OnHitEvent)
    {
        foreach (string tag in colliderTags)
        {
            if (FindAttackPointFrom(tag, out IAttackPoint attackPoint))
            {
                attackPoint.UnRegisterToHitEvent(OnHitEvent);
                attackPoint.DisableColliders();
            }
        }
    }

    #endregion
}

