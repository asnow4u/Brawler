using UnityEngine;

public abstract class SceneObjectHandler : MonoBehaviour
{
    protected SceneObject sceneObject;

    public virtual void Setup()
    {
        sceneObject = GetComponent<SceneObject>();

        if (sceneObject == null)
            throw new System.NullReferenceException("SceneObject not found on handlers gameobject");
    }

    public abstract void RegisterToEvents();
    public abstract void UnregisterToEvents();
}
