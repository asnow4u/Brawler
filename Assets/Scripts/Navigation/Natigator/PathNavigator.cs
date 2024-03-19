using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

public abstract class PathNavigator : MonoBehaviour
{
    protected bool isInitialized;
    protected Graph curGraph;    
    protected PathFinder pathFinder;

    protected PathPoint curPathPoint;
    protected PathPoint nextPathPoint;

    protected Action<TraversalType, float> movementCallback;
    
    //Getters
    protected Bounds bounds => GetComponent<CapsuleCollider>().bounds;
    protected MovementInputHandler moveHandler => GetComponent<MovementInputHandler>();
    protected Rigidbody rb => GetComponent<Rigidbody>();

    //Debug
    public bool DisplayGraph = true;
    public bool DisplayPathPoints = true;


    /// <summary>
    /// Initialize
    /// Set up callback for movement
    /// </summary>
    /// <param name="actionCallback"></param>
    public virtual void Initialize(Action<TraversalType, float> actionCallback)
    {
        if (actionCallback == null)
        {
            Debug.LogError("Path Navigator Requires Callback");
            return;
        }     

        movementCallback = actionCallback;
        isInitialized = true;
    }


    /// <summary>
    /// Build graph from current position to destination
    /// Create first pathpoint
    /// Also Create the nextPathPoint
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public async Task SetDestination(Vector3 target)
    {
        if (isInitialized)
        {
            if (TerrainNodeMapper.Instance.TryGetClosetNodeTo(transform.position, out TerrainNode startNode)
                && TerrainNodeMapper.Instance.TryGetClosetNodeTo(target, out TerrainNode endNode))
            {                
                //Graph
                curGraph = CreateGraph(startNode, endNode);

                //Pathfinder
                pathFinder.Setup(curGraph, moveHandler.CurMovementCollection);
                curPathPoint = pathFinder.CreateStartPathPoint(transform.position);

                //Next PathPoint
                SetNextPathPoint();
            }
        }
    }


    /// <summary>
    /// Create Graph to be used for calculating pathPoints
    /// </summary>
    /// <param name="startNode"></param>
    /// <param name="endNode"></param>
    /// <returns></returns>
    protected abstract Graph CreateGraph(TerrainNode startNode, TerrainNode endNode);


    private void FixedUpdate()
    {
        if (curPathPoint != null)
        {
            if (CheckForDestination())
            {
                if (nextPathPoint != null)
                {
                    SetCurPathPoint(nextPathPoint);
                    SetNextPathPoint();
                }

                else
                {
                    //TODO: Target reached
                    curPathPoint = null;
                }
            }


            //Movement
            if (curPathPoint != null)
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
        Bounds bounds = GetComponent<Collider>().bounds;

        if (MathF.Abs(transform.position.x - curPathPoint.Pos.x) < bounds.extents.x / 2)
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
    private void SetCurPathPoint(PathPoint pathPoint)
    {
        if (pathPoint != null)
        {
            curPathPoint = pathPoint;

            //Set velocity for jump
            if (pathPoint.TraversalType == TraversalType.Jump)
            {
                JumpPathPoint jumpPoint = (JumpPathPoint) pathPoint;
                rb.velocity = new Vector3(jumpPoint.InitialVelocity, rb.velocity.y);
                Debug.Log("Jump Inital Velocity Set: " + jumpPoint.InitialVelocity);
            }
        }
    }


    /// <summary>
    /// Determine the next pathpoint
    /// </summary>
    /// <returns></returns>
    private async Task SetNextPathPoint()
    {
        if (curPathPoint != null)
        {            
            nextPathPoint = await pathFinder.GetNextPathPoint(curPathPoint);
        }
    }


    /// <summary>
    /// Move towards the curPathPoint
    /// </summary>
    /// <param name="traversalPoint"></param>
    protected abstract void PerformMovement();




    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            if (DisplayGraph && curGraph != null)
            {
                foreach (Node node in curGraph.NodeList)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawCube(node.TerrainNode.Pos, Vector3.one * 0.2f);

                    foreach (Edge edge in node.EdgeList)
                    {
                        switch (edge.Type)
                        {
                            case EdgeType.Ground:
                                Gizmos.color = Color.blue;
                                Gizmos.DrawLine(edge.StartNode.Pos, edge.EndNode.Pos);
                                break;

                            case EdgeType.Jump:
                                if (edge.StartNode.Pos.y < edge.EndNode.Pos.y)
                                {
                                    Gizmos.color = Color.white;
                                    Gizmos.DrawLine(edge.StartNode.Pos, edge.EndNode.Pos);
                                }

                                break;
                            //        case EdgeType.Fly:
                            //            Gizmos.color = Color.cyan;
                            //            Gizmos.DrawLine(edge.StartNode.Pos, edge.EndNode.Pos);
                            //            break;
                        }
                    }
                }
            }

            if (DisplayPathPoints && curPathPoint != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(curPathPoint.Pos, 0.1f);
                Gizmos.DrawLine(transform.position, curPathPoint.Pos);

                if (nextPathPoint != null)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(nextPathPoint.Pos, 0.1f);
                    Gizmos.DrawLine(curPathPoint.Pos, nextPathPoint.Pos);
                }
            }

            
        }
    }
}

