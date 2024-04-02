using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Graph
{
    protected GraphType type;
    protected GraphNode startNode;
    protected GraphNode endNode;
    protected List<GraphNode> nodeList = new List<GraphNode>();

    protected Bounds bounds;
    protected MovementCollection moveCollection;

    //Getters
    public GraphType Type => type;
    public GraphNode StartNode => startNode;
    public GraphNode EndNode => endNode;
    public List<GraphNode> NodeList => nodeList;


    public Graph(TerrainNode startNode, TerrainNode endNode, Bounds bounds, MovementCollection collection)
    {
        this.moveCollection = collection;
        this.bounds = bounds;

        CreateNodes(startNode, endNode);
        
        this.startNode = CalculateNearestGraphNode(startNode);        
        this.endNode = CalculateNearestGraphNode(endNode);
        
        MapNodeConnections(NodeList);
    }
  

    /// <summary>
    /// Add nodes to the graph
    /// </summary>
    /// <param name="startNode"></param>
    /// <param name="endNode"></param>
    protected abstract void CreateNodes(TerrainNode startNode, TerrainNode endNode);
    protected abstract GraphNode CalculateNearestGraphNode(TerrainNode node);    
    protected abstract void MapNodeConnections(List<GraphNode> nodeList);


    /// <summary>
    /// Determine if the two nodes can be reached by using edges
    /// </summary>
    /// <param name="startNode"></param>
    /// <param name="endNode"></param>
    /// <returns></returns>
    public bool CheckForConnection(GraphNode startNode, GraphNode endNode, List<EdgeType> typeMask = null, List<GraphNode> visitedNodes = null)
    {
        if (visitedNodes == null)
            visitedNodes = new List<GraphNode>();      

        if (!visitedNodes.Contains(startNode))
        {
            visitedNodes.Add(startNode);

            foreach (Edge edge in startNode.EdgeList)
            {
                if (typeMask == null || typeMask.Contains(edge.Type))
                {
                    if (edge.EndNode == endNode)
                        return true;                    

                    else if (CheckForConnection(edge.EndNode, endNode, typeMask, visitedNodes))
                        return true;
                }
            }
        }

        return false;
    }


    /// <summary>
    /// Recursivly grab all connecting nodes based on the edge typeMask provided
    /// </summary>
    /// <param name="startNode"></param>
    /// <param name="typeMask"></param>
    /// <param name="connectingNodes"></param>
    /// <returns></returns>
    public List<GraphNode> GetAllConnectingNodes(GraphNode startNode, List<EdgeType> typeMask = null, List<GraphNode> connectingNodes = null)
    {
        if (connectingNodes == null)
            connectingNodes = new List<GraphNode>();

        if (!connectingNodes.Contains(startNode))
        {
            connectingNodes.Add(startNode);

            foreach (Edge edge in startNode.EdgeList)
            {
                if (typeMask == null || typeMask.Contains(edge.Type))
                {
                    if (!connectingNodes.Contains(edge.EndNode))
                        connectingNodes = GetAllConnectingNodes(edge.EndNode, typeMask, connectingNodes);
                }
            }
        }

        return connectingNodes;
    }
}
