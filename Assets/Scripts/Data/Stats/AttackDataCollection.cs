using System;
using UnityEngine;

[CreateAssetMenu(fileName = "AttackCollection", menuName = "ScriptableObjects/SceneObject/Attack/Collection")]
public class AttackDataCollection : ScriptableObject
{
    public AttackData UpTiltData;
    public AttackData UpAirData;
    public AttackData DownTiltData;
    public AttackData DownAirData;
    public AttackData ForwardTiltData;
    public AttackData ForwardAirData;


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
