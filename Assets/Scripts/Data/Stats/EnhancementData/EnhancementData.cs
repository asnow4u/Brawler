using System;
using UnityEngine;

/// <summary>What causes an enhancement to activate.</summary>
public enum EnhancementTrigger
{
    Attack,
    AttackHit,
}

[CreateAssetMenu(fileName = "EnhancementData", menuName = "ScriptableObjects/SceneObject/Enhancement/Enhancement")]
public class EnhancementData : ScriptableObject
{
    [Header("Trigger")]
    [Tooltip("What causes the enhancement to activate.")]
    public EnhancementTrigger Trigger;

    [Header("Effect")]
    [Tooltip("Prefab spawned when the enhancement activates. The prefab holds its own stats.")]
    public GameObject SpawnPrefab;

    [Tooltip("How the spawned prefab flies.")]
    public TrajectoryData Trajectory;

    [Tooltip("Spawns something that travels to distant targets.")]
    public bool IsRanged;

    public bool IsValid()
    {
        return SpawnPrefab != null && Trajectory != null;
    }


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
