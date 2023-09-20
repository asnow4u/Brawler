using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class AttackCollection : MonoBehaviour
{
    [Header("Ground Attacks")]
    public AttackData ForwardTilt;
    public AttackData UpTilt;
    public AttackData DownTilt;

    [Header("Air Attacks")]
    public AttackData ForwardAir;
    public AttackData BackAir;
    public AttackData UpAir;
    public AttackData DownAir;


    public bool GetAttackByType(AttackType.Type attackType, out AttackData attack)
    {
        switch (attackType)
        {
            case AttackType.Type.UpTilt:
                attack = UpTilt;
                return true;

            case AttackType.Type.ForwardTilt:
                attack = ForwardTilt;
                return true;

            case AttackType.Type.DownTilt:
                attack = DownTilt;
                return true;

            case AttackType.Type.UpAir:
                attack = UpAir;
                return true;

            case AttackType.Type.ForwardAir:
                attack = ForwardAir;
                return true;

            case AttackType.Type.DownAir:
                attack = DownAir;
                return true;

            case AttackType.Type.BackAir:
                attack = BackAir;
                return true;
        }

        attack = null;
        return false;
    }   
    

    public bool GetAttackByAnimationClipName(string clipName, out AttackData attack)
    {
        if (ForwardTilt.AttackAnimation.name == clipName)
        {
            attack = ForwardTilt;
            return true;
        }

        if (UpTilt.AttackAnimation.name == clipName)
        {
            attack = UpTilt;
            return true;
        }

        if (DownTilt.AttackAnimation.name == clipName)
        {
            attack = DownTilt;
            return true;
        }

        if (ForwardAir.AttackAnimation.name == clipName)
        {
            attack = ForwardAir;
            return true;
        }

        if (UpAir.AttackAnimation.name == clipName)
        {
            attack = UpAir;
            return true;
        }

        if (DownAir.AttackAnimation.name == clipName)
        {
            attack = DownAir;
            return true;
        }

        if (BackAir.AttackAnimation.name == clipName)
        {
            attack = BackAir;
            return true;
        }

        attack = null;
        return false;
    }
}

