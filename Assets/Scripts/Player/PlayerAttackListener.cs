using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttackListener : MonoBehaviour
{
    public Fist rightFist;
    public Fist leftFist;

    //NOTE: Called From Animation Event (Change in Animation tab)
    public void UpTilt()
    {
        rightFist.EnableCollider();
        rightFist.SetCurAttack("UpTilt");
    }

    public void ForwardTilt()
    {
        rightFist.EnableCollider();
        rightFist.SetCurAttack("ForwardTilt");
    }

    public void DownTilt()
    {
        rightFist.EnableCollider();
        rightFist.SetCurAttack("DownTilt");
    }

    public void UpAir()
    {
        leftFist.EnableCollider();
        leftFist.SetCurAttack("UpAir");
    }

    public void ForwardAir()
    {
        rightFist.EnableCollider();
        rightFist.SetCurAttack("ForwardAir");
    }

    public void DownAir()
    {
        rightFist.EnableCollider();
        rightFist.SetCurAttack("DownAir");
    }

    public void Finish()
    {
        rightFist.DisableCollider();
        leftFist.DisableCollider();
    }
}
