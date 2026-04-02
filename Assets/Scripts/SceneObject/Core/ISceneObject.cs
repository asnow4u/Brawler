using System;
using UnityEngine;

public enum SceneObjectType { Player, Enemy, Object }
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
    public bool CheckForEnvironmentCollision(Vector3 direction, float dist, out RaycastHit hitInfo);
    public bool ClimbableSurfaceAvailable();

    //Debug
    public void Log(string log);
}