using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Patrol settings configured on the enemy.</summary>
[Serializable]
internal class PatrolSettings
{
    [Tooltip("Object whose children are the waypoints. Found by the name PatrolPoints when empty.")]
    public Transform PatrolPoints;
    [Tooltip("How long the enemy waits at each waypoint, in seconds.")]
    public float DwellDuration = 1.5f;
    [Tooltip("Distance from a waypoint that counts as reaching it, in world units.")]
    public float WaypointTolerance = 0.75f;
}


/// <summary>
/// Walks a route of waypoints back and forth, dwelling at each one. The disengage radius is measured
/// from the nearest waypoint. With no usable waypoints, or after ReHome, it holds a single spot.
/// </summary>
internal class EnemyPatrol : IEnemyHome
{
    private const string PatrolPointsName = "PatrolPoints";

    private readonly Transform owner;
    private readonly PatrolSettings settings;
    private readonly List<Vector3> route = new List<Vector3>();

    private int index;
    private int direction = 1;

    private bool dwelling;
    private float dwellStartTime;

    private bool degraded;
    private Vector3 fallbackPost;

    private bool HasRoute => !degraded && route.Count > 0;


    public EnemyPatrol(Transform owner, PatrolSettings settings)
    {
        this.owner = owner;
        this.settings = settings;
        fallbackPost = owner.position;
    }


    #region Home

    public void Initialize(Vector3 spawnPosition)
    {
        fallbackPost = spawnPosition;

        BuildRoute();

        if (route.Count == 0)
        {
            degraded = true;
            return;
        }

        index = NearestWaypointIndex(spawnPosition);
    }

    public float DistanceFrom(Vector3 worldPosition)
    {
        if (!HasRoute)
            return Vector3.Distance(fallbackPost, worldPosition);

        float nearest = float.PositiveInfinity;

        for (int i = 0; i < route.Count; i++)
            nearest = Mathf.Min(nearest, Vector3.Distance(route[i], worldPosition));

        return nearest;
    }

    public Vector3 GetReturnDestination(Vector3 currentPosition)
    {
        if (!HasRoute)
            return fallbackPost;

        index = NearestWaypointIndex(currentPosition);
        dwelling = false;

        return route[index];
    }

    public bool TryGetIdleDestination(Vector3 currentPosition, bool arrived, bool failed, out Vector3 destination)
    {
        destination = fallbackPost;

        if (!HasRoute)
            return false;

        // An unreachable waypoint is skipped rather than retried.
        if (failed)
        {
            Advance();
            destination = route[index];
            return true;
        }

        if (dwelling)
        {
            if (Time.time - dwellStartTime < settings.DwellDuration)
                return false;

            Advance();
            destination = route[index];
            return true;
        }

        if (arrived || Vector3.Distance(currentPosition, route[index]) <= settings.WaypointTolerance)
        {
            dwelling = true;
            dwellStartTime = Time.time;
            return false;
        }

        destination = route[index];
        return true;
    }

    public void ReHome(Vector3 worldPosition)
    {
        degraded = true;
        fallbackPost = worldPosition;
    }

    #endregion


    #region Route

    private void BuildRoute()
    {
        route.Clear();

        Transform points = settings.PatrolPoints != null ? settings.PatrolPoints : owner.Find(PatrolPointsName);

        if (points == null)
            return;

        for (int i = 0; i < points.childCount; i++)
            route.Add(points.GetChild(i).position);
    }

    /// <summary>Steps to the next waypoint, reversing at either end of the route.</summary>
    private void Advance()
    {
        dwelling = false;

        if (route.Count <= 1)
            return;

        if (index + direction >= route.Count || index + direction < 0)
            direction = -direction;

        index += direction;
    }

    private int NearestWaypointIndex(Vector3 worldPosition)
    {
        int nearest = 0;
        float bestSqrDistance = float.PositiveInfinity;

        for (int i = 0; i < route.Count; i++)
        {
            float sqrDistance = (route[i] - worldPosition).sqrMagnitude;

            if (sqrDistance >= bestSqrDistance)
                continue;

            bestSqrDistance = sqrDistance;
            nearest = i;
        }

        return nearest;
    }

    #endregion


    #region Gizmos

    public void DrawHomeGizmos(float disengageRadius)
    {
        if (!Application.isPlaying)
            BuildRoute();

        if (!HasRoute)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f);
            Gizmos.DrawWireSphere(fallbackPost, disengageRadius);
            return;
        }

        for (int i = 0; i < route.Count; i++)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(route[i], Vector3.one * 0.3f);

            Gizmos.color = new Color(1f, 0.5f, 0f);
            Gizmos.DrawWireSphere(route[i], disengageRadius);

            if (i > 0)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(route[i - 1], route[i]);
            }
        }

        if (!Application.isPlaying)
            return;

        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(route[index], 0.45f);
    }

    #endregion
}
