using System;

public enum GroundedState { Grounded, Airborn }
public enum ClimbState { Unavailable, Available, Climbing }
public enum ActionState 
{ 
    Null = -1, 
    Idle = 0, 
    Moving = 1, 
    Attacking = 2, 
    HitStun = 3 
};
public enum IdleState 
{ 
    Null = -1, 
    GroundIdle = 0, 
    AirIdle = 1, 
    ClimbIdle = 2 
};

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

public interface IActionState
{
    public GroundedState CurGroundedState { get; }
    public ClimbState CurClimbState { get; }
    public ActionState CurActionState { get; }    
    public AttackState CurAttackState { get; }

    public void ChangeState(ActionState actionState);
    public bool TryChangeState(ActionState actionState);

    public void ChangeClimbState(ClimbState climbState);    
    public void ChangeAttackState(AttackState attackState);

    public event Action<GroundedState> GroundedStateChangedEvent;
    public event Action<ClimbState> ClimbStateChangedEvent;
    public event Action<ActionState> ActionStateChangedEvent;
    public event Action<IdleState> IdleStateChangedEvent;
    
    public event Action<AttackState> AttackStateChangedEvent;
}
