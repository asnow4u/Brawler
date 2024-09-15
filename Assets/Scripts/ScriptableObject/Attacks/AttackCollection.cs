using System;
using UnityEngine;


[CreateAssetMenu(fileName = "AttackCollection", menuName = "ScriptableObject/Attack/Collection")]
public class AttackCollection : ScriptableObject
{
    public WeaponData WeaponData;

    public AttackCollection(WeaponData weaponData)
    {
        this.WeaponData = weaponData;
    }


    #region Attack Data

    public bool TryGetAttackByType(AttackType attackType, out AttackData attack)
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

            case AttackType.Dash:
                attack = WeaponData.Dash;
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

        if (WeaponData.Dash != null && WeaponData.Dash.AttackAnimation.name == animationName)
        {
            attack = WeaponData.Dash;
            return true;
        }

        attack = null;
        return false;
    }

    #endregion
}

