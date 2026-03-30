using System;
using UnityEngine;

public enum SceneObjectType { Player, Enemy, Object }
public enum GroundedState { Airborn, Grounded, Climbing }
public enum ClimbState { Unavailable, Available, Climbing }
public enum Direction { Right, Left, Up, Down }

public interface ISceneObject
{
    //Base states
    public Guid UniqueID { get; }    

    //Direction
    public bool IsFacingRightDirection { get; }
    public void TurnAround();

    //Collision
    public Bounds Bounds { get; }
    public bool TryDetectCollision(Direction direction, float dist, LayerMask mask, out Collider collidingCollider);

    //States
    public GroundedState CurGroundedState { get; }
    public ClimbState CurClimbState { get; }

    public event Action<GroundedState> GroundedStateChangedEvent;
    public event Action<ClimbState, ClimbState> ClimbStateChangedEvent;

    //Debug
    public void Log(string log);
}