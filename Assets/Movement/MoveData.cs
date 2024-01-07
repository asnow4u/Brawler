using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Move", menuName = "ScriptableObject/Movement/Move")]
public class MoveData : MovementData
{
    public float XVelocityLimit;
    public float XVelocityAcceleration;
    public float XVelocityDeceleration;
    public float XAirVelocityAcceleration;
    public float FastFallVelocity;
}
