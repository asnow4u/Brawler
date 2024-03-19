using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Graph
{
    protected GraphType type;
    protected Node startNode;
    protected Node endNode;
    protected List<Node> nodeList = new List<Node>();

    protected Bounds bounds;
    protected MovementCollection moveCollection;

    //Getters
    public GraphType Type => type;
    public Node StartNode => startNode;
    public Node EndNode => endNode;
    public List<Node> NodeList => nodeList;


    public Graph(TerrainNode startNode, TerrainNode endNode, Bounds bounds, MovementCollection collection)
    {
        this.moveCollection = collection;
        this.bounds = bounds;

        CreateNodes(startNode, endNode);
        
        this.startNode = CalculateNearestGraphNode(startNode);        
        this.endNode = CalculateNearestGraphNode(endNode);

        foreach (Node node in nodeList)
            MapNodeConnections(node);
    }
  

    /// <summary>
    /// Add nodes to the graph
    /// </summary>
    /// <param name="startNode"></param>
    /// <param name="endNode"></param>
    protected abstract void CreateNodes(TerrainNode startNode, TerrainNode endNode);
    protected abstract Node CalculateNearestGraphNode(TerrainNode node);    
    protected abstract void MapNodeConnections(Node node);


    /// <summary>
    /// Determine if the two nodes can be reached by using edges
    /// </summary>
    /// <param name="startNode"></param>
    /// <param name="endNode"></param>
    /// <returns></returns>
    public bool CheckForConnection(Node startNode, Node endNode, List<EdgeType> typeMask = null, List<Node> visitedNodes = null)
    {
        if (visitedNodes == null)
            visitedNodes = new List<Node>();

        if (!visitedNodes.Contains(startNode))
        {
            visitedNodes.Add(startNode);

            foreach (Edge edge in startNode.EdgeList)
            {
                if (typeMask == null || typeMask.Contains(edge.Type))
                {
                    Node connectingNode = edge.GetConnectingNode(startNode);

                    if (connectingNode == endNode)
                        return true;

                    else if (CheckForConnection(connectingNode, endNode, typeMask, visitedNodes))
                        return true;
                }
            }
        }

        return false;
    }
}
