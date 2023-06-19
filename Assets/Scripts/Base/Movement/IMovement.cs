using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IMovement
{
    public void SetUp(SceneObject obj);

    public void SetCollection(MovementCollection collection);

    public void PerformMovement(Vector2 inputValue);

    public void PerformJump();

}
