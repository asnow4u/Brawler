using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackType
{
    public enum Type { UpTilt, DownTilt, ForwardTilt, UpAir, DownAir, ForwardAir, BackAir};
}

public class MovementType
{
    public enum Type { Move, Jump, AirJump, Fall, Land, Roll }
}