using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingPathNavigator : PathNavigator
{
    protected override void SetupPathFinder()
    {
        pathFinder = new FlyingPathFinder(gameObject);
    }


    protected override Graph CreateGraph(TerrainNode startNode, TerrainNode endNode)
    {
        GraphFactory graphFactory = new GraphFactory();
        return graphFactory.CreateGraph(GraphType.Flying, startNode, endNode, bounds, moveHandler.CurMovementCollection);
    }

    protected override void PerformMovement()
    {
        
    }
}
