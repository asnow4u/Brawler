using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInteraction
{
    public void ReceiveInput(SceneObject sceneObj);

    public void RegisterToInputEvent(Action<SceneObject> callback);

    public void UnregisterToInputEvent(Action<SceneObject> callback);
}
