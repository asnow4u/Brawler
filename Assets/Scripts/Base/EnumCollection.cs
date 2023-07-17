using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackType
{
    public enum Type { upTilt, downTilt, forwardTilt, upAir, downAir, forwardAir, backAir};
}

public class MovementType
{
    public enum Type { move, jump, airJump, land}
}
