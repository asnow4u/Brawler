using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TraversalType { Move, Jump, Land }
[Serializable]
public class TraversalPoint
{
    public Waypoint Destination;
    public TraversalType TraversalType;

    public TraversalPoint(Waypoint destination, TraversalType type)
    {
        this.Destination = destination;
        this.TraversalType = type;
    }
}

[Serializable]
public class JumpTraversalPoint : TraversalPoint
{
    public Vector2 JumpVelocity;

    public JumpTraversalPoint(Waypoint destination, TraversalType type, Vector2 jumpVelocity) : base(destination, type)
    {
        this.JumpVelocity = jumpVelocity;
    }
}


[Serializable]
public class Waypoint
{
    public Vector2 pos;
    public int Column;
    public int Row;

    public List<TraversalPoint> TraversalPoints = new List<TraversalPoint>();   

    public Waypoint(Vector3 pos, int column, int row)
    {
        this.pos = pos;
        this.Column = column;
        this.Row = row;
    }


    public bool TryGetTraversalPoint(Waypoint destination, out TraversalPoint traversalPoint)
    {
        foreach (TraversalPoint point in TraversalPoints)
        {
            if (point.Destination.Column == destination.Column && point.Destination.Row == destination.Row)
            {
                traversalPoint = point;
                return true;
            }
        }

        traversalPoint = null;
        return false;
    }
}
