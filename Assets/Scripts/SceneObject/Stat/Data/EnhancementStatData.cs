using System.Collections.Generic;
using UnityEngine;

public class EnhancementStatData
{
    public class EnhancementStats
    {
        public EnhancementTrigger Trigger;
        public GameObject SpawnPrefab;
        public TrajectoryData Trajectory;
        public bool IsRanged;

        public EnhancementStats(EnhancementData data)
        {
            Trigger = data.Trigger;
            SpawnPrefab = data.SpawnPrefab;
            Trajectory = data.Trajectory;
            IsRanged = data.IsRanged;
        }
    }

    public readonly List<EnhancementStats> Enhancements = new List<EnhancementStats>();
}
