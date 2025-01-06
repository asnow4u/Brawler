using Game.SceneObjects;
using System;

namespace Game.Interactable
{
    public class InteractionHandler
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
}
