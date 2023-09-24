using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WeaponData
{
    public AttackData UpTilt;
    public AttackData UpAir;
    public AttackData DownTilt;
    public AttackData DownAir;
    public AttackData ForwardTilt;
    public AttackData ForwardAir;
    public AttackData BackAir;

    public WeaponData(AttackData upTilt,
                      AttackData upAir,
                      AttackData downTilt,
                      AttackData downAir,
                      AttackData forwardTilt,
                      AttackData forwardAir,
                      AttackData backAir)
    {
        this.UpTilt = upTilt;
        this.UpAir = upAir;
        this.DownTilt = downTilt;
        this.DownAir = downAir;
        this.ForwardTilt = forwardTilt;
        this.ForwardAir = forwardAir;
        this.BackAir = backAir;
    }

}
