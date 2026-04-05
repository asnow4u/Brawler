using System;
using UnityEngine;

[RequireComponent(typeof(SceneObject))]
[RequireComponent(typeof(Collider))]
internal class HurtBox : MonoBehaviour, IHurtBox
{
    private ISceneObject sceneObject;

    public Guid SceneObjectID => sceneObject.UniqueID;
    public event Action<HitData> OnHitEvent;

    private void Awake()
    {
        sceneObject = GetComponentInParent<ISceneObject>();
        if (sceneObject == null)
            Debug.LogError("SceneObject not found as a parent to " + gameObject.name, gameObject);
    }

    public void Hit(HitData hitData)
    {
        OnHitEvent?.Invoke(hitData);
    }
}
