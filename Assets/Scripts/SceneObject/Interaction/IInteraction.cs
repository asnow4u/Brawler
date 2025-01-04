using System;
using Game.SceneObjects;

public interface IInteraction
{
    public void ReceiveInput(SceneObject sceneObj);

    public void RegisterToInputEvent(Action<SceneObject> callback);

    public void UnregisterToInputEvent(Action<SceneObject> callback);
}
