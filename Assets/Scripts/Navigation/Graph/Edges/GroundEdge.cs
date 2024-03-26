using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundEdge : Edge
{
    public GroundEdge(GraphNode startNode, GraphNode endNode) : base(startNode, endNode)
    {
        edgeType = EdgeType.Ground;
    }
}
