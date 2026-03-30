
using System;
using UnityEngine;

public interface IStats
{
    public event Action<AnimationStatData> AnimationStatsChangedEvent;
    public event Action<MovementStatData> MovementStatsChangedEvent;
    public event Action<AttackStatData> AttackStatsChangedEvent;
}

