using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PathFinder : MonoBehaviour
{
    protected Dictionary<(int, int), Waypoint> pathPoints;

    protected Vector3 curTarget;
    protected MovementCollection curMovementCollection;
    protected Bounds bounds => GetComponent<Collider>().bounds;

    public void FindPathTo(Vector3 target, MovementCollection moveCollection)
    {
        pathPoints = new Dictionary<(int, int), Waypoint>();
        curTarget = target;

        UpdateMovementCollection(moveCollection);

        List<Waypoint> newWaypoints = UpdatePathPoints(transform.position, target);

        if (newWaypoints.Count > 0)       
            MapTerrainConnections(newWaypoints);
    }


    /// <summary>
    /// Update the current movement collection
    /// </summary>
    /// <param name="moveCollection"></param>
    protected virtual void UpdateMovementCollection(MovementCollection moveCollection)
    {
        curMovementCollection = moveCollection; 
    }



    protected abstract List<Waypoint> UpdatePathPoints(Vector3 fromPos, Vector3 toPos);


    protected void MapTerrainConnections(List<Waypoint> waypoints)
    {
        foreach (Waypoint waypoint in waypoints)
        {
            //Check for connection with existing pathPoints
            EstablishConnectionsTo(waypoint);
        }
    }


    protected abstract void EstablishConnectionsTo(Waypoint waypoint);
}

