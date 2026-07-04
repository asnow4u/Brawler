using System;
using UnityEngine;

public enum SceneObjectType { Player, Enemy, Projectile, Object }
public enum Direction { Right, Left, Up, Down }

public interface ISceneObject
{
    //Base states
    public Guid UniqueID { get; }    
    public SceneObjectType ObjectType { get; }

    //Direction
    public bool IsFacingRightDirection { get; }
    public void TurnAround();

    //Collision
    public Bounds Bounds { get; }
    public bool TryDetectCollision(Direction direction, float dist, LayerMask mask, out Collider collidingCollider);
    public bool ClimbableSurfaceAvailable();

    //Debug
    public void Log(string log);
}