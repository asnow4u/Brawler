using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node
{
    public List<Edge> EdgeList;

    public TerrainNode TerrainNode;

    //Getters
    public int ColumnNum => TerrainNode.ColumnNum;
    public int RowNum => TerrainNode.RowNum;
    public Vector3 Pos => TerrainNode.Pos;  

    public Node(TerrainNode terrainNode)
    {
        EdgeList = new List<Edge>();
        TerrainNode = terrainNode;
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
            if (edge.Type == type)
                edges.Add(edge);
        }

        return edges;        
    }    




    public bool IsConnectedNode(Node node)
    {
        foreach (Edge edge in EdgeList)
        {
            if (node == edge.GetConnectingNode(this))
                return true;
        }

        return false;
    }
}
