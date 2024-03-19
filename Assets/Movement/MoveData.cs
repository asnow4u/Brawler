using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Move", menuName = "ScriptableObject/Movement/Move")]
public class MoveData : MovementData
{
    public float MaxXVelocity;
    public float XAcceleration;
    public float XDeceleration;   
    public float FastFallVelocity;
}
