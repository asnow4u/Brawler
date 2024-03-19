using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class FlyingPathFinder : PathFinder
{
    public FlyingPathFinder(GameObject go) : base(go)
    { }

    public override async Task<PathPoint> GetNextPathPoint(PathPoint startPathPoint)
    {
        throw new System.NotImplementedException();
    }


    protected override List<PathPoint> GetAvailablePathPoints(PathPoint startPathPoint)
    {
        throw new System.NotImplementedException();
    }





    protected override List<TraversalNode> BuildPath(PathNode startNode, float curVelocity, PathNode endNode, List<(int, int)> visitedNodes)
    {
        throw new System.NotImplementedException();
    }

 
}
