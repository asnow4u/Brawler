using System;
using UnityEngine;

/// <summary>Base for the effect entries held by an EffectsDataCollection.</summary>
public abstract class EffectData : ScriptableObject
{
    #region Editor Updating

    public event Action OnChangedEvent;

    #if UNITY_EDITOR

        protected virtual void OnValidate()
        {
            if (Application.isPlaying)
                OnChangedEvent?.Invoke();
        }

    #endif

    #endregion
}
