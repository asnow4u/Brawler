using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//NOTE: TraversalType.Move only accounts for horizontal movement and not vertical movement


[Serializable]
public class TraversalNode
{
    public PathNode Destination;
    public TraversalType TraversalType;
    
    public float YVelocity;
    public float XAccelerationRate;

    public TraversalNode(PathNode destination, TraversalType type)
    {
        this.Destination = destination;
        this.TraversalType = type;
    }
}


[Serializable]
public class PathNode
{
    public Vector2 Pos;
    public float TargetXVelocity;

    public List<TraversalNode> RightTraversalPoints = new List<TraversalNode>();
    public List<TraversalNode> LeftTraversalPoints = new List<TraversalNode>();   
    
    public TerrainNode TerrainNode;
    public int ColumnNum => TerrainNode.ColumnNum;
    public int RowNum => TerrainNode.RowNum;

    public PathNode(Vector3 pos, TerrainNode node)
    {
        this.Pos = pos;
        this.TerrainNode = node;
    }


    public bool TryGetTraversalPoint(PathNode destination, out TraversalNode traversalPoint)
    { 
        foreach (TraversalNode point in RightTraversalPoints)
        {
            if (destination.ColumnNum == point.Destination.ColumnNum 
                && destination.RowNum == point.Destination.RowNum)
            {
                traversalPoint = point;
                return true;
            }
        }

        foreach (TraversalNode point in LeftTraversalPoints)
        {
            if (destination.ColumnNum == point.Destination.ColumnNum
                && destination.RowNum == point.Destination.RowNum)
            {
                traversalPoint = point;
                return true;
            }
        }

        traversalPoint = null;
        return false;
    }
}
