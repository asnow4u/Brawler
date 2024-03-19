using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingGraph : Graph
{
    public FlyingGraph(TerrainNode startNode, TerrainNode endNode, Bounds bounds, MovementCollection collection) : base(startNode, endNode, bounds, collection)
    {
        type = GraphType.Flying;
    }


    /// <summary>
    /// Get all nodes inbetween terrain start and end that work with flying
    /// </summary>
    /// <param name="startNode"></param>
    /// <param name="endNode"></param>
    protected override void CreateNodes(TerrainNode startNode, TerrainNode endNode)
    {
        List<List<TerrainNode>> terrainNodes = TerrainNodeMapper.Instance.GetAllNodesWithin(startNode.ColumnNum,
                                                                                           endNode.ColumnNum,
                                                                                           new TerrainNodeType[] { TerrainNodeType.Air });

        foreach (List<TerrainNode> columnNodeList in terrainNodes)
        {
            foreach (TerrainNode terrainNode in columnNodeList)
            {
                nodeList.Add(new Node(terrainNode));
            }
        }
    }


    //TODO:
    protected override Node CalculateNearestGraphNode(TerrainNode node)
    {
        return null;
    }




    /// <summary>
    /// Map all possible nodes of graph to node
    /// The connecting node has to be 1 column and/or 1 row away
    /// </summary>
    /// <param name="node"></param>
    protected override void MapNodeConnections(Node node)
    {
        foreach (Node connectingNode in nodeList)
        {
            //Cant connect to self
            if (connectingNode != node)
            {
                if (!node.IsConnectedNode(connectingNode))
                {
                    //Check no more then one column away
                    if (Mathf.Abs(node.ColumnNum - connectingNode.ColumnNum) <= 1)
                    {
                        //Check no more then one row away
                        if (Mathf.Abs(node.RowNum - connectingNode.RowNum) <= 1)
                        {
                            node.EdgeList.Add(new Edge(EdgeType.Fly, node, connectingNode));
                            connectingNode.EdgeList.Add(new Edge(EdgeType.Fly, connectingNode, node));
                        }
                    }
                }
            }
        }
    }
}
