using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IActionState
{
    public bool ChangeState(int stateIndex);

    public void ResetState();
}
