
using System;

public enum AttackState 
{ 
    Null = -1, 
    UpTilt = 0, 
    DownTilt = 1, 
    ForwardTilt = 2, 
    UpAir = 3, 
    DownAir = 4, 
    ForwardAir = 5 
};

public interface IAttack
{
    public AttackState CurAttackState { get; }

    public event Action<AttackState> AttackStateChangedEvent;

}
