using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class PlatformGraph : Graph
{
    public PlatformGraph(TerrainNode startNode, TerrainNode endNode, Bounds bounds, MovementCollection collection) : base(startNode, endNode, bounds, collection)
    {
        type = GraphType.Platform;
    }


    /// <summary>
    /// Create nodes inbetween Terrain start and end that work with platform
    /// </summary>
    /// <param name="startNode"></param>
    /// <param name="endNode"></param>
    protected override void CreateNodes(TerrainNode startNode, TerrainNode endNode)
    {
        List<List<TerrainNode>> terrainNodes = TerrainNodeMapper.Instance.GetAllNodesWithin(startNode.ColumnNum,
                                                                                            endNode.ColumnNum,
                                                                                            new TerrainNodeType[] { TerrainNodeType.Surface, TerrainNodeType.SurfaceWall, TerrainNodeType.SurfaceLedge });

        foreach (List<TerrainNode> columnNodeList in terrainNodes)
        {
            foreach (TerrainNode terrainNode in columnNodeList)
            {
                nodeList.Add(new Node(terrainNode));
            }
        }
    }

    protected override Node CalculateNearestGraphNode(TerrainNode terrainNode)
    {
        Node nearestNode = null;

        foreach (Node node in nodeList)
        {
            if (nearestNode == null)
                nearestNode = node;

            else
            {
                if (Vector3.Distance(node.Pos, terrainNode.Pos) < Vector3.Distance(nearestNode.Pos, terrainNode.Pos))
                {
                    nearestNode = node;
                }
            }               
        }
        
        return nearestNode;
    }  


    /// <summary>
    /// Map node based on platforms and other graph nodes
    /// </summary>
    /// <param name="node"></param>
    protected override void MapNodeConnections(Node node)
    {
        foreach (Node connectingNode in nodeList)
        {
            //Cant connect to self
            if (connectingNode != node)
            {
                //Cant connect if already connected
                if (!node.IsConnectedNode(connectingNode))
                {
                    //Movement
                    MapMovementConnections(node, connectingNode);

                    //Jumps
                    MapJumpConnections(node, connectingNode);
                }
            }
        }
    }


    /// <summary>
    /// Map nodes based on movement
    /// </summary>
    /// <param name="node"></param>
    private void MapMovementConnections(Node node, Node connectingNode)
    {  
        if (CheckMovementConnection(node, connectingNode))
        {
            node.EdgeList.Add(new Edge(EdgeType.Ground, node, connectingNode));
            connectingNode.EdgeList.Add(new Edge(EdgeType.Ground, connectingNode, node));
        }        
    }


    private void MapJumpConnections(Node node, Node connectingNode)
    {
        if (CheckJumpConnection(node, connectingNode, out float xVelocity, out float jumpVelocity, out float jumpTime, out float jumpDist))
        {
            node.EdgeList.Add(new JumpEdge(EdgeType.Jump, node, connectingNode, jumpVelocity, xVelocity, jumpTime, jumpDist));
        }
    }


    #region Movement Connection


    /// <summary>
    /// Check if two nodes can be connected by movement
    /// Both nodes have to be next to each other (one column diffrence)
    /// Uses raycasts to check that environment is in between
    /// </summary>
    /// <param name="startNode"></param>
    /// <param name="endNode"></param>
    /// <returns></returns>
    //NOTE: UNSURE WHY BUT REMOVING THE RAYCASTHIT IN THE RAYCAST WILL OCCASIONALLY HAVE WRONG RESULTS 
    private bool CheckMovementConnection(Node startNode, Node endNode)
    {
        //Determine if next to each other
        if (startNode.ColumnNum + 1 == endNode.ColumnNum || startNode.ColumnNum - 1 == endNode.ColumnNum)
        {           
            Vector3 dir = (endNode.TerrainNode.Pos - startNode.TerrainNode.Pos).normalized;
            float dist = (endNode.TerrainNode.Pos - startNode.TerrainNode.Pos).magnitude;

            for (int i = 0; i < 10; i++)
            {
                Vector3 origin = startNode.TerrainNode.Pos + dir * i * (dist / 10);

                if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, TerrainNodeMapper.Instance.ScaleFactor, LayerMask.GetMask("Environment")))
                {
                    return false;
                }
            }

            return true;
        }

        return false;
    }

    #endregion


    #region Jump Connections

    private bool CheckJumpConnection(Node startNode, Node endNode, out float xVelocity, out float jumpYVelocity, out float jumpTime, out float jumpDist)
    {
        xVelocity = 0;
        jumpYVelocity = 0;
        jumpTime = 0;
        jumpDist = 0;

        //Cant jump to same column
        if (startNode.ColumnNum != endNode.ColumnNum)
        {
            //Position on ground with respect to bounds
            Vector3 startPos = startNode.TerrainNode.DownCollision.CollisionPoint + Vector3.up * bounds.extents.y;
            Vector3 endPos = endNode.TerrainNode.DownCollision.CollisionPoint + Vector3.up * bounds.extents.y;

            //Determine if jump height can be reached
            if (IsJumpHeightPossible(startPos, endPos, out jumpYVelocity))
            {
                //Calculate the time it takes to peak and then land
                jumpTime = CalculateJumpTime(startPos.y, endPos.y, jumpYVelocity);

                if (IsJumpDistancePossible(startPos, endPos, jumpTime, out xVelocity, out jumpDist))
                {
                    return true;
                }
            }                        
        }
        
        return false;
    }


    /// <summary>
    /// Calculate both jump peaks from startPos and endPos
    /// Calculate what jump velocity can reach the heightest peak from both points 
    /// Return false if not possible
    /// </summary>
    /// <param name="startPos"></param>
    /// <param name="endPos"></param>
    /// <param name="maxJumpVelocity"></param>
    /// <param name="jumpYVelocity"></param>
    /// <returns></returns>
    private bool IsJumpHeightPossible(Vector3 startPos, Vector3 endPos, out float jumpYVelocity)
    {
        float maxJumpVelocity = moveCollection.GetJumpVelocity();

        //Determine highest peak
        float jumpPeak = Mathf.Max(startPos.y + bounds.extents.y, endPos.y + bounds.extents.y);        

        //Calculate yVelocity to reach jumpPeak
        //V^2 = V0^2 + 2 * G * (Y - Y0) => V0 = Sqrt(-2 * G * (Y - Y0)) 
        jumpYVelocity = Mathf.Sqrt(Mathf.Abs(-2 * Physics.gravity.y * (jumpPeak - startPos.y)));

        //Must be possible
        if (jumpYVelocity > 0f && jumpYVelocity <= maxJumpVelocity)
            return true;

        jumpYVelocity = 0;
        return false;
    }


    /// <summary>
    /// Calculate the range at which the x distance of the jump can be made
    /// If the min amount of velocity is greater than the velocity limit, jump is not possible
    /// </summary>
    /// <param name="startPos"></param>
    /// <param name="endPos"></param>
    /// <param name="jumpTime"></param>
    /// <param name="minXVelocity"></param>
    /// <param name="maxXVelocity"></param>
    /// <returns></returns>
    private bool IsJumpDistancePossible(Vector3 startPos, Vector3 endPos, float jumpTime, out float xVelocity, out float jumpDist)
    {
        float xVelocityLimit = moveCollection.GetMaxXVelocity();
        jumpDist = endPos.x - startPos.x;

        //At what velocity can the jump be made with no acceleration
        xVelocity = jumpDist / jumpTime;

        //Can jump be made
        if (Mathf.Abs(xVelocity) <= xVelocityLimit)
            return true;

        return false;
    }



    #region Jump Time Calculation

    /// <summary>
    /// Calculate jump time based on upward velocity
    /// </summary>
    /// <param name="yVelocity"></param>
    /// <param name=""></param>
    private float CalculateJumpTime(float startYPos, float endYPos, float jumpVelocity)
    {
        float peakTime = CalculateTimeToPeak(jumpVelocity);

        float peakHeight = CalculatePeakHeight(startYPos, jumpVelocity);
        float landTime = CalculateTimeToLandFromPeak(peakHeight, endYPos);

        return peakTime + landTime;
    }


    /// <summary>
    /// Return the amount of time needed to reach the apex of a jump given Y velocity
    /// </summary>
    /// <param name="yVelocity"></param>
    /// <returns></returns>
    private float CalculateTimeToPeak(float yVelocity)
    {
        //Calculate the time it takes to peak
        //V = V0 + G(T) => T = -V/G
        return Mathf.Abs(-yVelocity / Physics.gravity.y);
    }


    /// <summary>
    /// Determine maximum height of jump
    /// </summary>
    /// <param name="startPos"></param>
    /// <param name="yVelocity"></param>
    /// <returns></returns>
    private float CalculatePeakHeight(float startPos, float yVelocity)
    {
        float peakTime = CalculateTimeToPeak(yVelocity);

        //Calculate the height of the peak
        //(Y0 - Y) = V0(T) + 1/2 * G * T^2

        float heightDiff = yVelocity * peakTime + 0.5f * Physics.gravity.y * (peakTime * peakTime);

        return startPos + heightDiff;
    }


    /// <summary>
    /// Return amount of time needed to fall from a start pos and land at end pos
    /// </summary>
    /// <param name="startPos"></param>
    /// <param name="endPos"></param>
    /// <returns></returns>
    private float CalculateTimeToLandFromPeak(float startPos, float endPos)
    {
        //Calculate the time it takes to land from peak
        //(Y0 - Y) = V0(T) + 1/2 * G * T^2 => T = Sqrt(2(Y0 - Y) / G)
        return Mathf.Sqrt(Mathf.Abs((2 * (endPos - startPos)) / Physics.gravity.y));
    }

    #endregion

    #endregion
}
