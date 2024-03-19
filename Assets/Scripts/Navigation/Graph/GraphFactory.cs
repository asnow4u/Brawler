using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GraphType { Platform, Flying }

public class GraphFactory
{
    /// <summary>
    /// Create a graph based on the type
    /// The graph will use nodes between startNode and endNode
    /// </summary>
    /// <param name="type"></param>
    /// <param name="startNode"></param>
    /// <param name="endNode"></param>
    /// <param name="collection"></param>
    /// <returns></returns>
    public Graph CreateGraph(GraphType type, TerrainNode startNode, TerrainNode endNode, Bounds bounds, MovementCollection collection)
    {
        switch (type)
        {
            case GraphType.Platform:
                return new PlatformGraph(startNode, endNode, bounds, collection);                

            case GraphType.Flying:
                return new FlyingGraph(startNode, endNode, bounds, collection);                
        }

        return null;
    }
}
