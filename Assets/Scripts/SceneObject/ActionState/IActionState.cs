using System;
using UnityEngine;

public enum IdleState { Null, GroundIdle, AirIdle, ClimbIdle };

public enum ActionState { Null, Idle, Moving, Attacking, HitStun };

public enum MovementState { Null, Move, AirMove, ClimbMove, WallLean, Vault, Jump, AirJump }

public enum AttackState { Null, UpTilt, DownTilt, ForwardTilt, UpAir, DownAir, ForwardAir };

public enum HitStunState { Null, Freeze, Launch }

public interface IActionState
{
    public ActionState CurActionState { get; }
    public MovementState CurMovementState { get; }
    public AttackState CurAttackState { get; }
    public HitStunState CurHitStunState { get; }

    public void ChangeMovementState(MovementState movementState);
    public void ChangeAttackState(AttackState attackState);
    public void SetHitStun(float hitStunTimer);

    public event Action<ActionState> ActionStateChangedEvent;
    public event Action<IdleState> IdleStateChangedEvent;
    public event Action<MovementState> MovementStateChangedEvent;
    public event Action<AttackState> AttackStateChangedEvent;
    public event Action<HitStunState> HitStunStateChangedEvent;
}
