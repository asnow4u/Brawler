using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class PathPoint
{
    protected TraversalType type;
    protected GraphNode graphNode;
    protected Vector3 pos;

    //Getters
    public TraversalType TraversalType => type;
    public GraphNode GraphNode => graphNode;
    public Vector3 Pos => pos;


    public PathPoint(GraphNode graphNode, TraversalType type, Vector3 pos)
    {
        this.type = type;
        this.graphNode = graphNode;
        this.pos = pos;
    }
}
