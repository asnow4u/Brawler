using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Move", menuName = "ScriptableObject/Movement/Move")]
public class MoveData : MovementData
{
    public float AccelerationX;
    public float DecelerationX;
    public float AirAccelerationX;
    public float FastFallVelocity;
}
