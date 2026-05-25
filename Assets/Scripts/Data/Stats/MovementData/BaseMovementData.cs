using System;
using UnityEngine;

public abstract class BaseMovementData : ScriptableObject
{   
    public AnimationData AnimationData;    

    public abstract bool IsValid();


    #region Editor Updating

    public event Action OnChangedEvent;

    #if UNITY_EDITOR

        private void OnValidate()
        {
            if (Application.isPlaying)
                OnChangedEvent?.Invoke();
        }

    #endif

    #endregion
}