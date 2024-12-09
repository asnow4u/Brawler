using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingPathNavigator : PathNavigator
{
    protected override void CreatePathFinder()
    {
        pathFinder = new FlyingPathFinder(gameObject);
    }


    protected override Graph CreateGraph(TerrainNode startNode, TerrainNode endNode)
    {
        if (moveHandler.TryGetCurrentMovementCollection(out MovementCollection curMovementCollection))
        {
            GraphFactory graphFactory = new GraphFactory();
            return graphFactory.CreateGraph(GraphType.Flying, startNode, endNode, bounds, curMovementCollection);
        }

        return null;
    }

    protected override void PerformMovement()
    {
        
    }
}
