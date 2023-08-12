using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractionHandler : IInteraction
{
    private Action<SceneObject> inputAction;

    public void ReceiveInput(SceneObject sceneObj)
    {
        inputAction?.Invoke(sceneObj);
    }

    public void RegisterToInputEvent(Action<SceneObject> callback)
    {        
        inputAction += callback;
    }

    public void UnregisterToInputEvent(Action<SceneObject> callback)
    {
        inputAction -= callback;
    }
}
