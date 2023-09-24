using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Tilemaps.Tile;

public class WeaponCollection : MonoBehaviour, IWeaponCollection
{
    [SerializeField] private List<Weapon> weapons = new List<Weapon>();
    [SerializeField] private Weapon curWeapon;

    [SerializeField] private Transform weaponHolder;

    private List<IAttackPoint> bodyAttackPoints;

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

    public Weapon GetCurWeapon()
    {
        return curWeapon;
    }


    private Weapon GetWeaponByIndex(int index)
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
                SetWeaponToInventory(weapon);

            SwapWeaponTo(weapons.Count -1);            
        }
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

    #endregion


    #region Attack Points

    private bool TryGetAttackPointFromWeapon(AttackCollider.Type colliderType, out IAttackPoint attackPoint)
    {
        foreach (IAttackPoint point in curWeapon.AttackPoints)
        {
            if (point.GetColliderType() == colliderType)
            {
                attackPoint = point;
                return true;
            }
        }

        attackPoint = null;
        return false;
    }


    private bool TryGetAttackPointFromBody(AttackCollider.Type colliderType, out IAttackPoint attackPoint)
    {
        foreach (IAttackPoint point in bodyAttackPoints)
        {
            if (point.GetColliderType() == colliderType)
            {
                attackPoint = point;
                return true;
            }
        }

        attackPoint = null;
        return false;
    }


    private bool FindAttackPointFrom(AttackCollider.Type colliderType, out IAttackPoint attackPoint)
    {
        if (TryGetAttackPointFromWeapon(colliderType, out attackPoint))
            return true;

        if (TryGetAttackPointFromBody(colliderType, out attackPoint))
            return true;

        return false;
    }


    public void EnableAttackColliders(List<AttackCollider.Type> colliderTypes, Action<IDamage> OnHitEvent)
    {
        foreach(AttackCollider.Type colliderType in colliderTypes)
        {
            if (FindAttackPointFrom(colliderType, out IAttackPoint attackPoint))
            {
                attackPoint.RegisterToHitEvent(OnHitEvent);
                attackPoint.EnableColliders();
            }
        }
    }


    public void DisableAttackColliders(List<AttackCollider.Type> colliderTypes, Action<IDamage> OnHitEvent)
    {
        foreach (AttackCollider.Type colliderType in colliderTypes)
        {
            if (FindAttackPointFrom(colliderType, out IAttackPoint attackPoint))
            {
                attackPoint.UnRegisterToHitEvent(OnHitEvent);
                attackPoint.DisableColliders();
            }
        }
    }


    #endregion
}

