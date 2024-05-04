using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Move", menuName = "ScriptableObject/Movement/Move")]
public class MoveData : MovementData
{
    [Header("Velocity Limit")]
    public float MaxXVelocity;

    [Header("Grounded Movement")]
    public float GroundedXAcceleration;
    public float GroundedXDeceleration;   

    [Header("Arial Movement")]
    public float ArialXAcceleration;
    public float ArialXDeceleration;
}
