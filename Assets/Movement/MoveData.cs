using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Move", menuName = "ScriptableObject/Movement/Move")]
public class MoveData : MovementData
{
    [Header("Velocity Limit")]
    public float MaxXVelocity;

    [Header("Acceleration")]
    public float XAcceleration;

    [Header("Decceleration")]
    public float GroundedXDeceleration;   
    public float ArialXDeceleration;

    [Header("Fast Fall")]
    public float FastFallVelocity;
}
