using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "Jump", menuName = "ScriptableObjects/Movement/Jump")]
public class JumpData : MovementData
{
    public float JumpVelocity;
    public float GravityScaler;
}
