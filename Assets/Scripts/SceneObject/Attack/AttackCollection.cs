using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum AttackType { UpTilt, DownTilt, ForwardTilt, UpAir, DownAir, ForwardAir, BackAir };

[Serializable]
public class AttackCollection
{
    public WeaponData WeaponData;
    public AttackPointCollection AttackPointCollection;

    public AttackCollection(WeaponData weaponData, List<GameObject> attackPoints)
    {
        this.WeaponData = weaponData;
        AttackPointCollection = new AttackPointCollection(attackPoints);
    }


    #region Attack Data

    public bool GetAttackByType(AttackType attackType, out AttackData attack)
    {
        switch (attackType)
        {
            case AttackType.UpTilt:
                attack = WeaponData.UpTilt;
                return true;

            case AttackType.ForwardTilt:
                attack = WeaponData.ForwardTilt;
                return true;

            case AttackType.DownTilt:
                attack = WeaponData.DownTilt;
                return true;

            case AttackType.UpAir:
                attack = WeaponData.UpAir;
                return true;

            case AttackType.ForwardAir:
                attack = WeaponData.ForwardAir;
                return true;

            case AttackType.DownAir:
                attack = WeaponData.DownAir;
                return true;

            case AttackType.BackAir:
                attack = WeaponData.BackAir;
                return true;
        }

        attack = null;
        return false;
    }   
    

    public bool TryGetAttackByAnimation(string animationName, out AttackData attack)
    {
        if (WeaponData.ForwardTilt != null && WeaponData.ForwardTilt.AttackAnimation.name == animationName)
        {
            attack = WeaponData.ForwardTilt;
            return true;
        }

        if (WeaponData.UpTilt != null && WeaponData.UpTilt.AttackAnimation.name == animationName)
        {
            attack = WeaponData.UpTilt;
            return true;
        }

        if (WeaponData.DownTilt != null && WeaponData.DownTilt.AttackAnimation.name == animationName)
        {
            attack = WeaponData.DownTilt;
            return true;
        }

        if (WeaponData.ForwardAir != null && WeaponData.ForwardAir.AttackAnimation.name == animationName)
        {
            attack = WeaponData.ForwardAir;
            return true;
        }

        if (WeaponData.UpAir != null && WeaponData.UpAir.AttackAnimation.name == animationName)
        {
            attack = WeaponData.UpAir;
            return true;
        }

        if (WeaponData.DownAir != null && WeaponData.DownAir.AttackAnimation.name == animationName)
        {
            attack = WeaponData.DownAir;
            return true;
        }

        if (WeaponData.BackAir != null && WeaponData.BackAir.AttackAnimation.name == animationName)
        {
            attack = WeaponData.BackAir;
            return true;
        }

        attack = null;
        return false;
    }

    #endregion


    #region Attack Points

    public void EnableAttackColliders(List<AttackColliderType> colliderTypes, Action<IDamage> OnHitEvent)
    {
        AttackPointCollection.EnableAttackColliders(colliderTypes, OnHitEvent);
    }


    public void DisableAttackColliders(List<AttackColliderType> colliderTypes, Action<IDamage> OnHitEvent)
    {
        AttackPointCollection.DisableAttackColliders(colliderTypes, OnHitEvent);
    }

    #endregion
}

