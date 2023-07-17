using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponHandler : IWeapon
{
    private List<Weapon> weapons = new List<Weapon>();
    private List<IAttackPoint> attackPoints = new List<IAttackPoint>();

    public WeaponHandler(Transform transform)
    {         
        foreach (Transform child in transform)
        {
            if (child.TryGetComponent(out Weapon weapon))
            {
                AddWeapon(weapon);
            }
        }        
    }                


    public void AddWeapon(Weapon weapon)
    {
        weapons.Add(weapon);

        attackPoints.AddRange(weapon.AttackPoints);
    }


    public void RemoveWeapon(Weapon weapon)
    {
        if (weapons.Contains(weapon))
        {
            foreach (IAttackPoint attackPoint in weapon.AttackPoints)
            {
                attackPoints.Remove(attackPoint);
            }

            weapons.Remove(weapon);
        }
    }


    public Weapon GetWeaponByIndex(int index)
    {
        if (weapons.Count > index)
        {
            return weapons[index];
        }

        return null;
    }


    private bool FindAttackPointFrom(string tag, out IAttackPoint attackPoint)
    {
        attackPoint = null;

        foreach (IAttackPoint point in attackPoints)
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
}

