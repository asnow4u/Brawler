using UnityEngine;

/// <summary>Which home an enemy is built with.</summary>
public enum EnemyHomeMode
{
    /// <summary>No home. Radii are measured from the enemy, and it stops where it is when it disengages.</summary>
    None,

    /// <summary>Holds a single spot.</summary>
    Post,

    /// <summary>Walks a route of waypoints.</summary>
    Patrol,
}


/// <summary>
/// Where an enemy belongs. Anchors its disengage radius, tells it where to return to, and drives it
/// while idle. Owned and built by the enemy.
/// </summary>
internal interface IEnemyHome
{
    /// <summary>Records the enemy's starting position.</summary>
    void Initialize(Vector3 spawnPosition);

    /// <summary>Distance from the home region to a world position. Used for the disengage radius.</summary>
    float DistanceFrom(Vector3 worldPosition);

    /// <summary>Where the enemy goes when returning home.</summary>
    Vector3 GetReturnDestination(Vector3 currentPosition);

    /// <summary>
    /// Destination to hold while idle. False to stand still. Arrived and failed report the agent's
    /// state at the previous destination.
    /// </summary>
    bool TryGetIdleDestination(Vector3 currentPosition, bool arrived, bool failed, out Vector3 destination);

    /// <summary>Replaces the home with a fixed point at the given position.</summary>
    void ReHome(Vector3 worldPosition);

    void DrawHomeGizmos(float disengageRadius);
}
