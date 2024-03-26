using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.InputSystem.HID;
using static UnityEngine.UI.Image;

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
                Vector3 collisionSurface = terrainNode.DownCollision.CollisionPoint;
                Vector3 pos = collisionSurface + Vector3.up * bounds.extents.y;
                nodeList.Add(new GraphNode(pos, terrainNode));
            }
        }
    }

    /// <summary>
    /// Determine which graphNode is the closest to the terrainNode
    /// </summary>
    /// <param name="terrainNode"></param>
    /// <returns></returns>
    protected override GraphNode CalculateNearestGraphNode(TerrainNode terrainNode)
    {
        GraphNode nearestNode = null;

        foreach (GraphNode node in nodeList)
        {
            if (nearestNode == null)
                nearestNode = node;

            else
            {
                if (Vector3.Distance(node.Pos, terrainNode.Pos) < Vector3.Distance(nearestNode.Pos, terrainNode.Pos))
                    nearestNode = node;
            }               
        }
        
        return nearestNode;
    }  


    /// <summary>
    /// Map node based on platforms and other graph nodes
    /// </summary>
    /// <param name="node"></param>
    protected override void MapNodeConnections(GraphNode node)
    {
        foreach (GraphNode connectingNode in nodeList)
        {
            //Cant connect to self
            if (connectingNode != node)
            {
                //Cant connect if already connected
                if (!node.IsConnectedByEdge(connectingNode))
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
    private void MapMovementConnections(GraphNode node, GraphNode connectingNode)
    {  
        if (CheckMovementConnection(node, connectingNode))
        {
            node.EdgeList.Add(new GroundEdge(node, connectingNode));
        }        
    }


    private void MapJumpConnections(GraphNode node, GraphNode connectingNode)
    {
        if (CheckJumpConnection(node, connectingNode, out float initialXVelocity, out float jumpXInfluence, out float jumpYInfluence))
        {
            node.EdgeList.Add(new JumpEdge(node, connectingNode, initialXVelocity, jumpXInfluence, jumpYInfluence));
        }
    }


    #region Movement Connection


    /// <summary>
    /// Check if two nodes can be connected by movement
    /// Both nodes have to be next to each other (one column diffrence)
    /// Check between nodes that no gaps exist
    /// Determine that no environment is in between
    /// </summary>
    /// <param name="node"></param>
    /// <param name="connectingNode"></param>
    /// <returns></returns>
    //NOTE: UNSURE WHY BUT REMOVING THE RAYCASTHIT IN THE RAYCAST WILL OCCASIONALLY HAVE WRONG RESULTS 
    private bool CheckMovementConnection(GraphNode node, GraphNode connectingNode)
    {
        //Determine if next to each other
        if (node.ColumnNum + 1 == connectingNode.ColumnNum || node.ColumnNum - 1 == connectingNode.ColumnNum)
        {           
            Vector3 dir = (connectingNode.Pos - node.Pos).normalized;
            float dist = (connectingNode.Pos - node.Pos).magnitude;

            if (CheckBetweenForGaps(node.Pos, dir, dist) &&
                CheckForBlockingEnvironment(node.Pos, dir, dist) &&
                CheckForClimbableSlope())
            {
                return true;                
            }
        }

        return false;
    }


    /// <summary>
    /// Use downward raycasts to determine that walkable environment exists
    /// Checks from a starting pos to some end pos that is a dist in a dir
    /// </summary>
    /// <param name="startPos"></param>
    /// <param name="dir"></param>
    /// <param name="dist"></param>
    /// <returns></returns>
    private bool CheckBetweenForGaps(Vector3 startPos, Vector3 dir, float dist)
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 origin = startPos + dir * i * (dist / 10);

            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, TerrainNodeMapper.Instance.ScaleFactor, LayerMask.GetMask("Environment")))
            {
                return false;
            }
        }

        return true;
    }


    /// <summary>
    /// Use raycasts to determine from some start pos, if environment exists
    /// </summary>
    /// <param name="startPos"></param>
    /// <param name="dir"></param>
    /// <param name="dist"></param>
    /// <returns></returns>
    private bool CheckForBlockingEnvironment(Vector3 startPos, Vector3 dir, float dist)
    {
        //Center Raycast
        if (Physics.Raycast(startPos, dir, dist, LayerMask.GetMask("Environment")))
        {
            return false;
        }

        //Top Raycast
        if (Physics.Raycast(startPos + Vector3.up * bounds.extents.y, dir, dist, LayerMask.GetMask("Environment")))
        {
            return false;
        }

        return true;
    }


    //TODO: Determine if slope is to much
    //TODO: Need to determine step height (Whats the lowest the raycast should check for environmental objects in the way)  
    private bool CheckForClimbableSlope()
    {
        return true;
    }

    #endregion


    #region Jump Connections

    private bool CheckJumpConnection(GraphNode node, GraphNode connectingNode, out float jumpXVelocity, out float jumpXInfluence, out float jumpYInfluence)
    {
        //Cant jump to same column
        if (node.ColumnNum != connectingNode.ColumnNum)
        {
            //Determine if jump height can be reached
            if (IsJumpHeightPossible(node.Pos, connectingNode.Pos, out float jumpYVelocity))
            {
                //Calculate the time it takes to peak and then land
                float jumpTime = CalculateJumpTime(node.Pos.y, connectingNode.Pos.y, jumpYVelocity);

                if (IsJumpDistancePossible(node.Pos, connectingNode.Pos, jumpTime, out jumpXVelocity))
                {
                    if (CheckJumpTrajectory(node.Pos, jumpXVelocity, jumpYVelocity, jumpTime))
                    {
                        //NOTE: No acceleration used at this time
                        jumpXInfluence = 0f;
                        jumpYInfluence = jumpYVelocity / moveCollection.GetJumpVelocity();

                        return true;
                    }
                }
            }                        
        }
        
        jumpXVelocity = 0f;
        jumpXInfluence = 0f;
        jumpYInfluence = 0f;

        return false;
    }


    /// <summary>
    /// Calculate both jump peaks from startPos and endPos
    /// Calculate what jump velocity can reach the heightest peak from starting pos 
    /// Return false if not possible    
    /// </summary>
    /// <param name="startPos"></param>
    /// <param name="endPos"></param>
    /// <param name="maxJumpVelocity"></param>
    /// <param name="jumpYVelocity"></param>
    /// <returns></returns>
    //TODO: Determine a better peak measurement (In this case a big enemy would have to jump really high for a jump to be allowed)
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
    private bool IsJumpDistancePossible(Vector3 startPos, Vector3 endPos, float jumpTime, out float xVelocity)
    {
        float xVelocityLimit = moveCollection.GetMaxXVelocity();
        float jumpDist = endPos.x - startPos.x;

        //At what velocity can the jump be made with no acceleration
        xVelocity = jumpDist / jumpTime;

        //Can jump be made
        if (Mathf.Abs(xVelocity) <= xVelocityLimit)
            return true;

        return false;
    }


    /// <summary>
    /// Use raycasts to determine if trajectory arc will collide with any environment
    /// </summary>
    /// <param name="startPos"></param>
    /// <param name="jumpXVelocity"></param>
    /// <param name="jumpYVelocity"></param>
    /// <param name="jumpTime"></param>
    /// <returns></returns>
    private bool CheckJumpTrajectory(Vector3 startPos, float jumpXVelocity, float jumpYVelocity, float jumpTime)
    {
        float t = 0f;        

        Vector3 prevPos = startPos;
        Vector3 nextPos = Vector3.zero;

        while (t < 1)
        {
            //Increment of 10
            t += 0.1f;

            float time = Mathf.Lerp(0, jumpTime, t);

            //Calculate next point
            float nextX = startPos.x + jumpXVelocity * time;
            float nextY = startPos.y + jumpYVelocity * time + (Physics.gravity.y * time * time) / 2;
            nextPos = new Vector3(nextX, nextY, startPos.z);

            Vector3 dir = (nextPos - prevPos).normalized;
            float dist = (nextPos - prevPos).magnitude;

            //Check between
            if (Physics.Raycast(prevPos, dir, dist, LayerMask.GetMask("Environment")))
                return false;

            //Check left buffer
            if (Physics.Raycast(nextPos, Vector3.left, bounds.extents.x, LayerMask.GetMask("Environment")))
                return false;

            //Check right buffer
            if (Physics.Raycast(nextPos, Vector3.right, bounds.extents.x, LayerMask.GetMask("Environment")))
                return false;

            prevPos = nextPos;
        }

        return true;
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
