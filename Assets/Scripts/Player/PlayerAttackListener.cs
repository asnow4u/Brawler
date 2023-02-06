using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttackListener : MonoBehaviour
{
    public Fist rightFist;
    public Fist leftFist;

    //Called From Animation Event (Change in Animation tab)
    public void UpTilt()
    {
        rightFist.EnableCollider();
        rightFist.SetCurAttack("UpTilt");
    }

    public void Finish()
    {
        rightFist.DisableCollider();
        leftFist.DisableCollider();
    }
}
