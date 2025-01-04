using UnityEngine;
using Game.SceneObjects;

[RequireComponent(typeof(Collider))]
public abstract class Interactable : MonoBehaviour
{
    private void OnTriggerEnter(Collider col)
    {
        if (col.TryGetComponent(out SceneObject sceneObj))
        {
            sceneObj.InteractionHandler.RegisterToInputEvent(InputReceived);
        }
    }

    private void OnTriggerExit(Collider col)
    {
        if (col.TryGetComponent(out SceneObject sceneObj))
        {
            sceneObj.InteractionHandler.UnregisterToInputEvent(InputReceived);                       
        }
    }


    protected abstract void InputReceived(SceneObject sceneObj);    
}
