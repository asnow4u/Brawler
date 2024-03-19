using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EdgeType { Ground, Jump, Fly }

public class Edge
{
    public EdgeType Type;
    public Node StartNode;
    public Node EndNode;


    public Edge(EdgeType type, Node startNode, Node endNode)
    {
        this.Type = type;
        this.StartNode = startNode;
        this.EndNode = endNode;
    }


    public Node GetConnectingNode(Node node)
    {
        if (node == StartNode)
            return EndNode;

        if (node == EndNode) 
            return StartNode;

        return null;
    }
}
