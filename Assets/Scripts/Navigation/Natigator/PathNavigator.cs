using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public abstract class PathNavigator : MonoBehaviour
{
    protected Graph curGraph;    
    protected PathFinder pathFinder;

    //PathPoints
    [SerializeField] protected Edge curRoute;
    [SerializeField] protected Edge nextRoute;

    //Getters
    protected Bounds bounds => GetComponent<CapsuleCollider>().bounds;
    protected MovementInputHandler moveHandler => GetComponent<MovementInputHandler>();
    protected Rigidbody rb => GetComponent<Rigidbody>();

    //Events
    public event Action<EdgeType, float> PathMovementEvent;
    public event Action DestinationReachedEvent;

    //Debug
    public bool DisplayGraph = false;
    public bool DisplayRoutes = false;   
    public List<GraphNode> routes = new List<GraphNode>();

    /// <summary>
    /// Build graph from current position to destination
    /// Create first pathpoint
    /// Also Create the nextPathPoint
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public async Task SetDestination(Vector3 target)
    {
        CreatePathFinder();

        if (TerrainNodeMapper.Instance.TryGetClosetNodeTo(transform.position, out TerrainNode startNode)
            && TerrainNodeMapper.Instance.TryGetClosetNodeTo(target, out TerrainNode endNode))
        {
            //Graph
            curGraph = CreateGraph(startNode, endNode);

            //Pathfinder
            //pathFinder.Setup(curGraph, moveHandler.CurMovementCollection);            

            //Routes
            SetCurRoute(await pathFinder.GetNextRoute(curGraph.StartNode));
            SetNextRoute();
        }
    }


    protected abstract void CreatePathFinder();


    /// <summary>
    /// Create Graph to be used for calculating pathPoints
    /// </summary>
    /// <param name="startNode"></param>
    /// <param name="endNode"></param>
    /// <returns></returns>
    protected abstract Graph CreateGraph(TerrainNode startNode, TerrainNode endNode);


    private void FixedUpdate()
    {
        if (curRoute != null)
        {
            if (CheckForDestination())
            {
                if (nextRoute != null)
                {
                    SetCurRoute(nextRoute);
                    SetNextRoute();
                }

                else
                {                    
                    curRoute = null;                    
                    DestinationReachedEvent?.Invoke();
                }
            }


            //Movement
            if (curRoute != null)
                PerformMovement();
        }
    }


    /// <summary>
    /// Determine distance to curPathPoint
    /// return true if close enough
    /// </summary>
    /// <returns></returns>
    private bool CheckForDestination()
    {
        if (Vector3.Distance(GetComponent<Collider>().bounds.center, curRoute.EndNode.Pos) < 0.2f)
        {
            return true;
        }

        return false;
    }


    /// <summary>
    /// Set the current path point to go towards
    /// Based on the traversal type, provide additionals
    /// </summary>
    /// <param name="pathPoint"></param>
    private void SetCurRoute(Edge route)
    {
        if (route != null)
        {
            curRoute = route;

            //Set velocity for jump
            if (route.Type == EdgeType.Jump)
            {
                JumpEdge jumpRoute = (JumpEdge)route;
                rb.linearVelocity = new Vector3(jumpRoute.InitialVelocity, rb.linearVelocity.y);
            }
        }
    }


    /// <summary>
    /// Determine the next pathpoint
    /// </summary>
    /// <returns></returns>
    private async Task SetNextRoute()
    {
        if (curRoute != null)
        {
            routes.Add(curRoute.EndNode);
            nextRoute = await pathFinder.GetNextRoute(curRoute.EndNode);
        }
    }


    /// <summary>
    /// Move towards the curPathPoint
    /// </summary>
    /// <param name="traversalPoint"></param>
    protected abstract void PerformMovement();


    /// <summary>
    /// Used to trigger the PathMovementEvent
    /// </summary>
    /// <param name="traversalType"></param>
    /// <param name="influence"></param>
    protected void TriggerMovementEvent(EdgeType edgeType, float influence)
    {
        PathMovementEvent?.Invoke(edgeType, influence);
    }


    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            if (DisplayGraph && curGraph != null)
            {
                foreach (GraphNode node in curGraph.NodeList)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawCube(node.Pos, Vector3.one * 0.2f);

                    foreach (Edge edge in node.EdgeList)
                    {
                        switch (edge.Type)
                        {
                            case EdgeType.Ground:
                                Gizmos.color = Color.blue;
                                Gizmos.DrawLine(edge.StartNode.Pos, edge.EndNode.Pos);
                                break;

                            case EdgeType.Jump:
                                Gizmos.color = Color.yellow;
                                Gizmos.DrawLine(edge.StartNode.Pos, edge.EndNode.Pos);                                

                                break;
                            //        case EdgeType.Fly:
                            //            Gizmos.color = Color.cyan;
                            //            Gizmos.DrawLine(edge.StartNode.Pos, edge.EndNode.Pos);
                            //            break;
                        }
                    }
                }
            }

            if (DisplayRoutes && curRoute != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(curRoute.EndNode.Pos, 0.1f);
                Gizmos.DrawLine(transform.position, curRoute.EndNode.Pos);

                if (nextRoute != null)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(nextRoute.EndNode.Pos, 0.1f);
                    Gizmos.DrawLine(curRoute.EndNode.Pos, nextRoute.EndNode.Pos);
                }
            }
        }
    }
}

