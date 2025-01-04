using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Move", menuName = "ScriptableObjects/Movement/AirMove")]
public class AirMoveData : MovementData
{
    [Header("Velocity Limit")]
    public float AerialMaxXVelocity;

    [Header("Arial Movement")]
    public float AerialXAcceleration;
    public float AerialXDeceleration;
}
