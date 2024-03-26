using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class GraphNode
{
    protected Vector3 pos;

    public List<Edge> EdgeList;

    protected TerrainNode terrainNode;

    //Getters
    public Vector3 Pos => terrainNode.Pos;  
    public int ColumnNum => terrainNode.ColumnNum;
    public int RowNum => terrainNode.RowNum;


    public GraphNode(Vector3 pos, TerrainNode terrainNode)
    {
        this.pos = pos;
        this.terrainNode = terrainNode;
        EdgeList = new List<Edge>();
    }


    /// <summary>
    /// Returns a list of edges based on the type provided
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public List<Edge> GetEdgesOfType(EdgeType type) 
    {
        List<Edge> edges = new List<Edge>();
        foreach (Edge edge in EdgeList)
        {
            if (edge.EdgeType == type)
                edges.Add(edge);
        }

        return edges;        
    }    



    /// <summary>
    /// Return if graphNode is connected by an edge
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    public bool IsConnectedByEdge(GraphNode node)
    {
        foreach (Edge edge in EdgeList)
        {
            if (edge.EndNode == node)
                return true;
        }

        return false;
    }
}
