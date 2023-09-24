using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class AttackCollection : MonoBehaviour
{
    public WeaponData WeaponData;

    public bool GetAttackByType(AttackType.Type attackType, out AttackData attack)
    {
        switch (attackType)
        {
            case AttackType.Type.UpTilt:
                attack = WeaponData.UpTilt;
                return true;

            case AttackType.Type.ForwardTilt:
                attack = WeaponData.ForwardTilt;
                return true;

            case AttackType.Type.DownTilt:
                attack = WeaponData.DownTilt;
                return true;

            case AttackType.Type.UpAir:
                attack = WeaponData.UpAir;
                return true;

            case AttackType.Type.ForwardAir:
                attack = WeaponData.ForwardAir;
                return true;

            case AttackType.Type.DownAir:
                attack = WeaponData.DownAir;
                return true;

            case AttackType.Type.BackAir:
                attack = WeaponData.BackAir;
                return true;
        }

        attack = null;
        return false;
    }   
    

    public bool GetAttackByAnimationClipName(string clipName, out AttackData attack)
    {
        if (WeaponData.ForwardTilt.AttackAnimation.name == clipName)
        {
            attack = WeaponData.ForwardTilt;
            return true;
        }

        if (WeaponData.UpTilt.AttackAnimation.name == clipName)
        {
            attack = WeaponData.UpTilt;
            return true;
        }

        if (WeaponData.DownTilt.AttackAnimation.name == clipName)
        {
            attack = WeaponData.DownTilt;
            return true;
        }

        if (WeaponData.ForwardAir.AttackAnimation.name == clipName)
        {
            attack = WeaponData.ForwardAir;
            return true;
        }

        if (WeaponData.UpAir.AttackAnimation.name == clipName)
        {
            attack = WeaponData.UpAir;
            return true;
        }

        if (WeaponData.DownAir.AttackAnimation.name == clipName)
        {
            attack = WeaponData.DownAir;
            return true;
        }

        if (WeaponData.BackAir.AttackAnimation.name == clipName)
        {
            attack = WeaponData.BackAir;
            return true;
        }

        attack = null;
        return false;
    }
}

