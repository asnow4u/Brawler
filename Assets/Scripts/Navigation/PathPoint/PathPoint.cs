using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TraversalType { Move, Jump, Land, Fly }

public class PathPoint
{
    protected TraversalType type;
    protected Node graphNode;
    protected Vector3 pos;

    //Getters
    public TraversalType TraversalType => type;
    public Node GraphNode => graphNode;
    public Vector3 Pos => pos;


    public PathPoint(Node graphNode, TraversalType type, Vector3 pos)
    {
        this.type = type;
        this.graphNode = graphNode;
        this.pos = pos;
    }
}
