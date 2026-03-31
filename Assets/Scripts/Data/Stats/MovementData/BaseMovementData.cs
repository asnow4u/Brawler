using System;
using UnityEngine;

public abstract class BaseMovementData : ScriptableObject
{   
    public AnimationClip Animation;

    public abstract bool IsValid();

    internal event Action OnChangedEvent;

#if UNITY_EDITOR
    private void OnValidate()
    {
        OnChangedEvent?.Invoke();
    }
#endif
}