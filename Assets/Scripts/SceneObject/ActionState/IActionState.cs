using System;

public enum GroundedState { Grounded, Airborn }
public enum ClimbState { Unavailable, Available, Climbing }
public enum ActionState { Null, Idle, Moving, Attacking, HitStun };
public enum IdleState { Null, GroundIdle, AirIdle, ClimbIdle };
public enum MovementState { Null, Move, AirMove, ClimbMove, WallLean, Vault, Jump, AirJump }
public enum AttackState { Null, UpTilt, DownTilt, ForwardTilt, UpAir, DownAir, ForwardAir };
public enum HitStunState { Null, Stop, Launch }

public interface IActionState
{
    public GroundedState CurGroundedState { get; }
    public ClimbState CurClimbState { get; }
    public ActionState CurActionState { get; }
    public MovementState CurMovementState { get; }
    public AttackState CurAttackState { get; }
    public HitStunState CurHitStunState { get; }

    public void ChangeClimbState(ClimbState climbState);
    public void ChangeMovementState(MovementState movementState);
    public void ChangeAttackState(AttackState attackState);
    public void ChangeHitStunState(HitStunState hitStunState);

    public event Action<GroundedState> GroundedStateChangedEvent;
    public event Action<ClimbState> ClimbStateChangedEvent;

    public event Action<ActionState> ActionStateChangedEvent;
    public event Action<IdleState> IdleStateChangedEvent;
    public event Action<MovementState> MovementStateChangedEvent;
    public event Action<AttackState> AttackStateChangedEvent;
    public event Action<HitStunState> HitStunStateChangedEvent;
}
