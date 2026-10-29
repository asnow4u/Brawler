using System;
using UnityEngine;

[Serializable]
public class WeaponData
{
    //Attack Data
    public AttackDataCollection PlayerAttackCollection;
    public AttackDataCollection EnemyAttackCollection;


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
