using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IActionState
{
    public void Setup(SceneObject obj);

    public bool VerifyState(ActionState.State state);

    public bool ChangeState(ActionState.State state);

    public void ResetState();

}
