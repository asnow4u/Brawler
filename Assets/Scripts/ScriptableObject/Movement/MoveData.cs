using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Move", menuName = "ScriptableObject/Movement/Move")]
public class MoveData : MovementData
{
    [Header("Velocity Limit")]
    public float GroundedMaxXVelocity;

    [Header("Grounded Movement")]
    public float GroundedXAcceleration;
    public float GroundedXDeceleration;   
}
