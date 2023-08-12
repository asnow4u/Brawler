using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Interactable : MonoBehaviour
{
    private void OnTriggerEnter(Collider col)
    {
        if (col.TryGetComponent(out SceneObject sceneObj))
        {
            sceneObj.interactionHandler.RegisterToInputEvent(InputReceived);
        }
    }

    private void OnTriggerExit(Collider col)
    {
        if (col.TryGetComponent(out SceneObject sceneObj))
        {
            sceneObj.interactionHandler.UnregisterToInputEvent(InputReceived);                       
        }
    }


    protected virtual void InputReceived(SceneObject sceneObj)
    {

    }
}
