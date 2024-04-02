using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EdgeType { Ground, Jump, Fly }

[Serializable]
public abstract class Edge
{
    protected EdgeType edgeType;
    protected GraphNode startNode;
    protected GraphNode endNode;

    //Getters
    public EdgeType Type => edgeType;
    public GraphNode StartNode => startNode;
    public GraphNode EndNode => endNode;


    public Edge(GraphNode startNode, GraphNode endNode)
    {        
        this.startNode = startNode;
        this.endNode = endNode;
    }
}
