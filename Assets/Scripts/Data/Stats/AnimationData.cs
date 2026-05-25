using System;
using UnityEngine;

[Serializable]
public class AnimationData
{
    public AnimationClip Animation;
    public float AnimationSpeed = 1f;


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
