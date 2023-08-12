using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IMovement
{
    public void Setup(SceneObject obj);

    public void PerformMovement(Vector2 inputValue);

    public void PerformJump();

}
