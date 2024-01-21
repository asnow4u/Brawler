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
    public TerrainNode TerrainNode;

    public List<TraversalPoint> RightTraversalPoints = new List<TraversalPoint>();
    public List<TraversalPoint> LeftTraversalPoints = new List<TraversalPoint>();   

    public Waypoint(Vector3 pos, TerrainNode node)
    {
        this.pos = pos;
        this.TerrainNode = node;
    }


    public bool TryGetTraversalPoint(Waypoint destination, out TraversalPoint traversalPoint)
    { 
        foreach (TraversalPoint point in RightTraversalPoints)
        {
            if (destination.TerrainNode.ColumnNum == point.Destination.TerrainNode.ColumnNum 
                && destination.TerrainNode.RowNum == point.Destination.TerrainNode.RowNum)
            {
                traversalPoint = point;
                return true;
            }
        }

        foreach (TraversalPoint point in LeftTraversalPoints)
        {
            if (destination.TerrainNode.ColumnNum == point.Destination.TerrainNode.ColumnNum
                && destination.TerrainNode.RowNum == point.Destination.TerrainNode.RowNum)
            {
                traversalPoint = point;
                return true;
            }
        }

        traversalPoint = null;
        return false;
    }
}
