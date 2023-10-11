using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AirJump", menuName = "ScriptableObject/Movement/AirJump")]
public class AirJumpData : MovementData
{
    public int JumpsAvailable;
    public float AirJumpVelocity;
}
