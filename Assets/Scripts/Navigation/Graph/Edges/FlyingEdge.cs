using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingEdge : Edge
{
    public FlyingEdge(GraphNode startNode, GraphNode endNode) : base(startNode, endNode)
    {
        edgeType = EdgeType.Fly;
    }
}
