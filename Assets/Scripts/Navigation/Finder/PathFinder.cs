using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public abstract class PathFinder
{
    protected GameObject go;
    protected List<PathPoint> pathPoints;

    protected Graph curGraph;
    protected Node startNode;
    protected Node endNode;
    protected MovementCollection moveCollection;

    //Getters
    protected Bounds bounds => go.GetComponent<CapsuleCollider>().bounds;
    protected Rigidbody rb => go.GetComponent<Rigidbody>();

    public PathFinder(GameObject go)
    {
        this.go = go;
    }

    /// <summary>
    /// Setup finder
    /// Graph is what will be used to refference connections
    /// MovementCollection will be used to determine if connections are possible
    /// </summary>
    /// <param name="graph"></param>
    /// <param name="collection"></param>
    public void Setup(Graph graph, MovementCollection collection)
    {
        curGraph = graph;
        startNode = graph.StartNode;
        endNode = graph.EndNode;
        moveCollection = collection;
    }


    /// <summary>
    /// Create starting pathpoint
    /// This is used as the starting point for traversing the graph
    /// Should use current pos and cur velocity
    /// </summary>
    /// <param name="startPos"></param>
    /// <param name="startVelocity"></param>
    /// <returns></returns>
    public PathPoint CreateStartPathPoint(Vector3 startPos)
    {
        return new PathPoint(startNode, TraversalType.Move, startPos);
    }


    public abstract Task<PathPoint> GetNextPathPoint(PathPoint pathPoint);    


    protected abstract List<PathPoint> GetAvailablePathPoints(PathPoint pathPoint);











    protected abstract List<TraversalNode> BuildPath(PathNode startNode, float curVelocity, PathNode endNode, List<(int, int)> visitedNodes);

    ///// <summary>
    ///// Recursivly go through all possible waypoints and randomly determine a possible path
    ///// Tracks all previous nodes visited to prevent duplication
    ///// Returns null is target is unreachable
    ///// </summary>
    ///// <param name="node"></param>
    ///// <param name="targetNode"></param>
    ///// <param name="visitedNodes"></param>
    ///// <returns></returns>
    //private List<TraversalNode> BuildPath(PathNode node, PathNode targetNode, List<(int, int)> visitedNodes)
    //{
    //    //Add to visited nodes
    //    visitedNodes.Add((node.ColumnNum, node.RowNum));

    //    //Determine possible Traversal points
    //    List<TraversalNode> possableTraversalPoints = new List<TraversalNode>();

    //    //Left side
    //    foreach (TraversalNode traversalPoint in node.LeftTraversalPoints)
    //    {
    //        if (!visitedNodes.Contains((traversalPoint.Destination.ColumnNum, traversalPoint.Destination.RowNum)))
    //            possableTraversalPoints.Add(traversalPoint);
    //    }

    //    //Right side
    //    foreach (TraversalNode traversalPoint in node.RightTraversalPoints)
    //    {
    //        if (!visitedNodes.Contains((traversalPoint.Destination.ColumnNum, traversalPoint.Destination.RowNum)))
    //            possableTraversalPoints.Add(traversalPoint);
    //    }

    //    if (possableTraversalPoints.Count > 0)
    //    {
    //        //Determine if target is a possible option
    //        foreach (TraversalNode traversalPoint in possableTraversalPoints)
    //        {
    //            if (traversalPoint.Destination.ColumnNum == targetNode.ColumnNum
    //                && traversalPoint.Destination.RowNum == targetNode.RowNum)
    //            {
    //                return new List<TraversalNode> { traversalPoint };
    //            }
    //        }

    //        //Single path       
    //        if (possableTraversalPoints.Count == 1)
    //        {
    //            List<TraversalNode> pathPoints = BuildPath(possableTraversalPoints[0].Destination, targetNode, visitedNodes);

    //            if (pathPoints != null)
    //            {
    //                pathPoints.Insert(0, possableTraversalPoints[0]);
    //                return pathPoints;
    //            }
    //        }

    //        //Multiple paths
    //        else
    //        {
    //            while (possableTraversalPoints.Count > 0)
    //            {
    //                int rand = Random.Range(0, possableTraversalPoints.Count);

    //                List<TraversalNode> pathPoints = BuildPath(possableTraversalPoints[rand].Destination, targetNode, visitedNodes);

    //                if (pathPoints != null)
    //                {
    //                    pathPoints.Insert(0, possableTraversalPoints[rand]);
    //                    return pathPoints;
    //                }

    //                possableTraversalPoints.RemoveAt(rand);
    //            }

    //            return null;
    //        }
    //    }

    //    //Dead End
    //    return null;
    //}

    

}
