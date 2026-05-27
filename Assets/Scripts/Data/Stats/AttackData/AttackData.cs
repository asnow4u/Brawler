using System;
using System.Collections.Generic;
using UnityEngine;

/*
* Poke: Fast and extended, Low commitment.
* Swing: Commited arc that hits a point in front of you (Horizontal swing)
* Sweep: Wide arc that covers area (Z axis swing)
* Heavy: Slow and hard hitting, high commitment.
*/
public enum AttackType {Poke, Swing, Sweep, Heavy}

[CreateAssetMenu(fileName = "AttackData", menuName = "ScriptableObjects/SceneObject/Attack/Attack")]
public class AttackData : ScriptableObject
{
    [Header("Type")]
    public AttackType AttackType;

    [Header("Animation")]
    public AnimationData AnimationData;

    [Header("Attack Details")]    
    [Tooltip("Influence defines the amount of knockback velocity that will be applied when the attack makes contact." +
        "\n 0 is no additional knockback," +
        "\n 1 is full knockback")]
    [Range(0, 1)]
    public float Influence;
    
    public float Damage; //TODO: Change to be a animation curve for diversity

    [Tooltip("Launch angle that the target will be launched at when hit." +
        "\n 0 is the forward," +
        "\n 90 is the up," +
        "\n 180 is backwards," +
        "\n 270 is down")]
    public float LaunchAngle;

    [Tooltip("Amount of time(Sec) that attacker and target are stunned when hit." +
        "\nThis helps add enphisis and weight to the attack.")]
    public float HitPauseTime = 0.1f;


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
