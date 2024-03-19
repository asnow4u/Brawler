using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.XR;

public class PlatformPathFinder : PathFinder
{
    private PlatformGraph platformGraph => (PlatformGraph)curGraph;

    public PlatformPathFinder(GameObject go) : base(go)
    { }   


    private Vector3 CalculatePathPointPos(Node node)
    {
        Vector3 surface = node.TerrainNode.DownCollision.CollisionPoint;
        return surface + Vector3.up * bounds.extents.y;
    }


    /// <summary>
    /// From a given node calculate all available options through the graph
    /// Determine if option can reach targetNode
    /// </summary>
    /// <param name="graph"></param>
    /// <param name="startNode"></param>
    /// <param name="targetNode"></param>
    /// <param name="collection"></param>
    /// <returns></returns>
    public override async Task<PathPoint> GetNextPathPoint(PathPoint pathPoint)
    {        
        List<PathPoint> availablePathPoints = GetAvailablePathPoints(pathPoint);

        //No path points found
        if (availablePathPoints.Count == 0)
            return null;

        //One path point found
        else if (availablePathPoints.Count == 1)
            return availablePathPoints[0];

        //Multiple path points found
        else
        {
            int rand = UnityEngine.Random.Range(0, availablePathPoints.Count);

            //TODO: PLAN:
            //Determine if its possible to make it to the target using recursion
            //If possible return
            //If not, discard option and randomly pick again
            //If no options are valid, choose closest option

            return availablePathPoints[rand];
        }
    }


    /// <summary>
    /// Return a list of all possible movement and jump pathpoints
    /// </summary>
    /// <returns></returns>
    protected override List<PathPoint> GetAvailablePathPoints(PathPoint pathPoint)
    {               
        List<PathPoint> pathPoints = new List<PathPoint>();

        //Get possible move points
        pathPoints.AddRange(GetAvailableMovePathPoints(pathPoint));

        //Prevent jumping twice in a row
        if (pathPoint.TraversalType != TraversalType.Jump)
        {
            //Get possible jump points
            pathPoints.AddRange(GetAvailableJumpPathPoints(pathPoint));
        }

        return pathPoints;
    }


    #region Movement Path Points

    /// <summary>
    /// Return a list of possible movePathpoints that are connected to the startNode
    /// </summary>
    /// <param name="pathPoint"></param>
    /// <returns></returns>
    private List<PathPoint> GetAvailableMovePathPoints(PathPoint pathPoint)
    {
        List<PathPoint> movePathPoints = new List<PathPoint>();

        foreach (Edge edge in pathPoint.GraphNode.GetEdgesOfType(EdgeType.Ground))
        {            
            Node connectingNode = edge.GetConnectingNode(pathPoint.GraphNode);
            
            //Determine if movement is possible
            if (CheckMovementConnection(pathPoint, connectingNode, out PathPoint connectingPoint))
            {
                movePathPoints.Add(connectingPoint);
            }      
        }

        return movePathPoints;
    }


    /// <summary>
    /// Determine if movement between startPoint and node is good
    /// Uses raycasts to check for environment
    /// Calculates the expected velocity at the endPoint
    /// Creates endPoint
    /// </summary>
    /// <param name="startPoint"></param>
    /// <param name="node"></param>
    /// <param name="endPoint"></param>
    /// <returns></returns>
    private bool CheckMovementConnection(PathPoint startPoint, Node node, out PathPoint endPoint)
    {
        //TODO: Determine if slope is to much
        //TODO: Need to determine step height (Whats the lowest the raycast should check for environmental objects in the way)        

        //Position
        Vector3 endPointPos = CalculatePathPointPos(node);

        //Raycasts
        Vector3 dir = (endPointPos - startPoint.Pos).normalized;
        float dist = (endPointPos - startPoint.Pos).magnitude;

        //Top Raycast
        Vector3 origin = startPoint.Pos + Vector3.up * bounds.extents.y;

        if (Physics.Raycast(origin, dir, dist, LayerMask.GetMask("Environment")))
        {
            endPoint = null;
            return false;
        }

        //Center Raycast
        if (Physics.Raycast(startPoint.Pos, dir, dist, LayerMask.GetMask("Environment")))
        {
            endPoint = null;
            return false;
        }

        //EndPoint
        endPoint = new PathPoint(node, TraversalType.Move, endPointPos);
        return true;
    }

    #endregion


    #region Jump Path Points

    /// <summary>
    /// Return a list of possible jumpPathPoints
    /// If startNode is an edge node, then determine if any other node can be jumped to
    /// If startNode is not an edge node, then determine if any edge nodes can be jumped to
    /// Cant jump to a node that is connected (A movement path can connect the two nodes)
    /// Calculate if jump is possible and determine if any environment is in the way
    /// </summary>
    /// <param name="pathPoint"></param>
    /// <returns></returns>
    private List<JumpPathPoint> GetAvailableJumpPathPoints(PathPoint pathPoint)
    {
        List<JumpPathPoint> jumpPathPoints = new List<JumpPathPoint>();

        //Edge node (node with single ground connecing edge)
        if (pathPoint.GraphNode.GetEdgesOfType(EdgeType.Ground).Count == 1)
        {
            foreach (JumpEdge edge in pathPoint.GraphNode.GetEdgesOfType(EdgeType.Jump))
            {
                Node connectingNode = edge.GetConnectingNode(pathPoint.GraphNode);

                //Determine if node can be reached by ground connections
                if (!curGraph.CheckForConnection(pathPoint.GraphNode, connectingNode, new List<EdgeType>() { EdgeType.Ground }))
                {
                    if (CheckJumpConnection(pathPoint, edge, out float initVelocity))
                    {
                        PathPoint endPoint = new PathPoint(connectingNode, TraversalType.Jump, CalculatePathPointPos(connectingNode));

                        //Calculate jump trajectory(Determine if jump is possible and get x / y influence values
                        if (DetermineJumpTrajectory(initVelocity, edge, out float jumpXInfluence, out float jumpYInfluence))
                        {
                            //Create jumpPathPoint
                            jumpPathPoints.Add(new JumpPathPoint(endPoint, initVelocity, jumpXInfluence, jumpYInfluence));
                        }
                    }
                }
            }
        }

        //Non Edge Node
        else
        {
            foreach (JumpEdge edge in pathPoint.GraphNode.GetEdgesOfType(EdgeType.Jump))
            {
                Node connectingNode = edge.GetConnectingNode(pathPoint.GraphNode);                

                //Determine if connecting node is an edge node
                if (connectingNode.GetEdgesOfType(EdgeType.Ground).Count == 1)
                {                    
                    //Determine if node can be reached by ground connections
                    if (!curGraph.CheckForConnection(pathPoint.GraphNode, connectingNode, new List<EdgeType>() { EdgeType.Ground }))
                    {
                        if (CheckJumpConnection(pathPoint, edge, out float initVelocity))
                        {
                            PathPoint endPoint = new PathPoint(connectingNode, TraversalType.Jump, CalculatePathPointPos(connectingNode));

                            //Calculate jump trajectory (Determine if jump is possible and get x/y values
                            if (DetermineJumpTrajectory(initVelocity, edge, out float jumpXInfluence, out float jumpYInfluence))
                            {
                                //Create jumpPathPoint
                                jumpPathPoints.Add(new JumpPathPoint(endPoint, initVelocity, jumpXInfluence, jumpYInfluence));
                            }
                        }
                    }
                }
            }
        }

        return jumpPathPoints;
    }


    /// <summary>
    /// Determine if cur velocity is within jump threshold
    /// Determine that a connecting path exists to the endNode
    /// </summary>
    /// <param name="startPoint"></param>
    /// <param name="edge"></param>
    /// <returns></returns>
    private bool CheckJumpConnection(PathPoint startPoint, JumpEdge edge, out float initVelocity)
    {
        //Determine if a path exists to get to the target
        if (platformGraph.CheckForConnection(edge.GetConnectingNode(startPoint.GraphNode), endNode))
        {
            initVelocity = edge.JumpXVelocity;
            return true;
        }

        initVelocity = 0f;
        return false;
    }


    #endregion


    #region Jump Calculations

    /// <summary>    
    /// Determines the jump velocity needed using the following equations
    /// X = X0 + Vx(T)
    /// Y = Y0 + Vy(T) - (g(T)^2) / 2
    /// 
    /// Kinematic Equations
    /// V^2 = V0^2 + 2 * G * (Y - Y0)  
    /// V = V0 + G(T)
    /// (Y0 - Y) = V0(T) + 1/2 * G * T^2
    /// 
    /// </summary>
    /// <returns></returns>
    private bool DetermineJumpTrajectory(float initVelocity, JumpEdge jumpEdge, out float jumpXInfulence, out float jumpYInfluence)
    {
        jumpXInfulence = 0f;
        jumpYInfluence = 0f;

        float maxJumpVelocity = moveCollection.GetJumpVelocity();
        float maxXAcceleration = moveCollection.GetXAcceleration();

        //Calculate XAcceleration
        //X = V0 * T + 1/2 * A * T^2 => A = (2 ( X - V0 * T)) / T^2
        float jumpXAcceleration = (2 * (jumpEdge.JumpDistance - (initVelocity * jumpEdge.JumpTime))) / (jumpEdge.JumpTime * jumpEdge.JumpTime);

        if (Mathf.Abs(jumpXAcceleration) <= maxXAcceleration)
        {
                //Determine Infulence
                jumpYInfluence = jumpEdge.JumpYVelocity / maxJumpVelocity;
                jumpXInfulence = jumpXAcceleration / maxXAcceleration;

                return true;
        }

        return false;
    }

    #endregion



























    #region Jump Connection


    /// <summary>
    /// Determine the acceleration required to make a jump from startpos to endpos in jumpTime
    /// Determine if acceleration is possible
    /// Check that velocity dosent change signs (pos => neg / neg => pos)
    /// </summary>
    /// <param name="startPos"></param>
    /// <param name="endPos"></param>
    /// <param name="xVelocity"></param>
    /// <param name="jumpTime"></param>
    /// <param name="maxXAcceleration"></param>
    /// <param name="jumpXAcceleration"></param>
    /// <returns></returns>
    private bool IsJumpDistancePossible(Vector3 startPos, Vector3 endPos, float jumpTime, out float jumpXAcceleration)
    {
        //float maxXAcceleration = moveCollection.GetXAcceleration();
        //float maxXVelocity = moveCollection.GetMaxXVelocity();

        //float xVelocity = rb.velocity.x;

        //string log = "Jump Calculations";
        //log += "\n PosDiff: " + (endPos.x - startPos.x);
        //log += "\n Time: " + jumpTime;
        //log += "\n Initial Velocity " + rb.velocity.x;


        ////NOTE: Bounds.extents.x is to compensate for PathNavigator.CheckForDestination();
        //float diff = (startPos.x - rb.transform.position.x) - bounds.extents.x;
        //log += "\n Velocity PosDiff: " + diff;

        ////V^2 = V0^2 + 2 * a * X
        //xVelocity = Mathf.Clamp( Mathf.Sqrt( Mathf.Abs( (rb.velocity.x * rb.velocity.x) + (2 * moveCollection.GetXAcceleration() * diff))), -moveCollection.GetMaxXVelocity(), moveCollection.GetMaxXVelocity());
        //log += "\n Start Velocity: " + xVelocity;

        ////Reverse if headed to the left
        //if (startPos.x > endPos.x)
        //    xVelocity = -xVelocity;




        ////Calculate XAcceleration
        ////X = V0 * T + 1/2 * A * T^2 => A = (2 ( X - V0 * T)) / T^2
        //jumpXAcceleration = (2 * ((endPos.x - startPos.x) - (xVelocity * jumpTime))) / (jumpTime * jumpTime);
        //log += "\n Acceleration: " + jumpXAcceleration;


        //if (Mathf.Abs(jumpXAcceleration) <= maxXAcceleration)
        //{
        //    //Determine if needed
        //    //Check for velocity sign changes
        //    float jumpXEndVelocity = xVelocity + jumpXAcceleration * jumpTime;
        //    log += "\n EndVelocity: " + jumpXEndVelocity;

        //    if (Mathf.Abs(jumpXEndVelocity) <= maxXVelocity)
        //    {
        //        Debug.Log(log);
        //        return true;
        //    }
        //}

        jumpXAcceleration = 0;
        return false;
    }



    /// <summary>
    /// Check for existing connection
    /// Determine if jump is possible in both ways
    /// </summary>
    /// <param name="startNode"></param>
    /// <param name="jumpTargets"></param>
    private void EstablishPossibleJumpConnections(PathNode startNode, List<PathNode> jumpTargets)
    {
        foreach (PathNode jumpNode in jumpTargets)
        {
            if (!startNode.TryGetTraversalPoint(jumpNode, out TraversalNode node))
            {
                //Check if within jump range
                if (CheckJumpConnection(startNode.Pos, jumpNode.Pos))
                {
                    //Debug.Log("Add Jump Connection (" + startNode.ColumnNum + ", " + startNode.RowNum + ") => (" + jumpNode.ColumnNum + ", " + jumpNode.RowNum + ")");

                    if (startNode.ColumnNum < jumpNode.ColumnNum)
                    {
                        startNode.RightTraversalPoints.Add(new TraversalNode(jumpNode, TraversalType.Jump));
                        jumpNode.LeftTraversalPoints.Add(new TraversalNode(startNode, TraversalType.Jump));
                    }

                    else
                    {
                        startNode.LeftTraversalPoints.Add(new TraversalNode(jumpNode, TraversalType.Jump));
                        jumpNode.RightTraversalPoints.Add(new TraversalNode(startNode, TraversalType.Jump));
                    }                                                       
                }
            }
        }
    }


    /// <summary>
    /// Determine if maxVelocity can reach both the X and Y componenets of the jump
    /// Checks both start => end and end => start jumps
    /// </summary>
    /// <param name="startPos"></param>
    /// <param name="endPos"></param>
    /// <returns></returns>
    private bool CheckJumpConnection(Vector3 startPos, Vector3 endPos)
    {        
        //Determine if both jump heights are possible at max velocity
        //if (IsJumpHeightPossible(startPos, endPos))
        //{
            //Calculate the time it takes to peak and then land
            //float startJumpTime = CalculateJumpTime(startPos.y, endPos.y, maxYVelocity);            
            //float endJumpTime = CalculateJumpTime(endPos.y, startPos.y, maxYVelocity);               

            //if (IsJumpDistancePossible(startPos, endPos, maxXVelocity, startJumpTime) 
            //    && IsJumpDistancePossible(endPos, startPos, maxXVelocity, endJumpTime))
            {
                return true;
            }
        //}
        
        return false;
    }

    #endregion


    #region Build Path

    /// <summary>
    /// Recursivly go through all possible waypoints and randomly determine a possible path
    /// Tracks all previous nodes visited to prevent duplication
    /// Returns null is target is unreachable
    /// </summary>
    /// <param name="node"></param>
    /// <param name="destinationNode"></param>
    /// <param name="visitedNodes"></param>
    /// <returns></returns>
    protected override List<TraversalNode> BuildPath(PathNode node, float curXVelocity, PathNode destinationNode, List<(int, int)> visitedNodes)
    {
        //Add to visited nodes
        visitedNodes.Add((node.ColumnNum, node.RowNum));

        //Determine possible Traversal points
        List<TraversalNode> possibleTraversalNodes = CollectPossibleRoutes(node, visitedNodes);

        if (possibleTraversalNodes.Count > 0)
        {
            //Determine if destination is reachable
            foreach (TraversalNode traversalNode in possibleTraversalNodes)
            {
                if (traversalNode.Destination.ColumnNum == destinationNode.ColumnNum
                    && traversalNode.Destination.RowNum == destinationNode.RowNum)
                {
                    if (CalculateNodeTraversal(node, traversalNode, curXVelocity, out float targetXVelocity))
                    {
                        return new List<TraversalNode>() { traversalNode };
                    }
                }
            }


            //Single path       
            if (possibleTraversalNodes.Count == 1)
            {
                if (CalculateNodeTraversal(node, possibleTraversalNodes[0], curXVelocity, out float targetXVelocity))
                {                    
                    List<TraversalNode> pathPoints = BuildPath(possibleTraversalNodes[0].Destination, targetXVelocity, destinationNode, visitedNodes);

                    if (pathPoints != null)
                    {
                        pathPoints.Insert(0, possibleTraversalNodes[0]);
                        return pathPoints;
                    }
                }
            }

            //Multiple paths
            else
            {
                while (possibleTraversalNodes.Count > 0)
                {
                    //int rand = Random.Range(0, possibleTraversalNodes.Count);

                    //if (CalculateNodeTraversal(node, possibleTraversalNodes[rand], curXVelocity, out float targetXVelocity))
                    //{
                    //    List<TraversalNode> pathPoints = BuildPath(possibleTraversalNodes[rand].Destination, targetXVelocity, destinationNode, visitedNodes);

                    //    if (pathPoints != null)
                    //    {
                    //        pathPoints.Insert(0, possibleTraversalNodes[rand]);
                    //        return pathPoints;
                    //    }
                    //}

                    //possibleTraversalNodes.RemoveAt(rand);
                }

                return null;
            }
        }

        //Dead End
        return null;
    }


    /// <summary>
    /// Collect the possible traversalNodes in both right and left directions that havent been visited
    /// </summary>
    /// <param name="node"></param>
    /// <param name="visitedNodes"></param>
    /// <returns></returns>
    private List<TraversalNode> CollectPossibleRoutes(PathNode node, List<(int, int)> visitedNodes)
    {
        //Determine possible Traversal points
        List<TraversalNode> possableRoutes = new List<TraversalNode>();
        //Left side
        foreach (TraversalNode traversalPoint in node.LeftTraversalPoints)
        {
            if (!visitedNodes.Contains((traversalPoint.Destination.ColumnNum, traversalPoint.Destination.RowNum)))
                possableRoutes.Add(traversalPoint);
        }

        //Right side
        foreach (TraversalNode traversalPoint in node.RightTraversalPoints)
        {
            if (!visitedNodes.Contains((traversalPoint.Destination.ColumnNum, traversalPoint.Destination.RowNum)))
                possableRoutes.Add(traversalPoint);
        }

        return possableRoutes;
    }



    private bool CalculateNodeTraversal(PathNode curNode, TraversalNode traversalNode, float startXVelocity, out float endXVelocity)
    {

        switch (traversalNode.TraversalType)
        {
            case TraversalType.Jump:

                Debug.Log("Add Jump Connection " + startXVelocity + " (" + curNode.ColumnNum + ", " + curNode.RowNum + ") => (" + traversalNode.Destination.ColumnNum + ", " + traversalNode.Destination.RowNum + ")");
                //if (CalculateJumpTrajectory(curNode.Pos, traversalNode.Destination.Pos, startXVelocity, out float jumpYVelocity, out float jumpXAcceleration))
                //{
                //    traversalNode.XAccelerationRate = jumpXAcceleration;
                //    traversalNode.YVelocity = jumpYVelocity;
                //    Debug.Log("Good");
                //  //  return true;
                //}
                Debug.Log("Bad");
                break;

            case TraversalType.Move:

                //traversalNode.XAccelerationRate = xAcceleration;
                //return true;    
                break;
        }

        endXVelocity = 0;
        return false;
    }

    #endregion










    


 

    


   



 


    #region TODO: Check Jump Trajectory

    /// <summary>
    /// Check if jump trajectory hits any other environment objects
    /// </summary>
    /// <param name="startPos"></param>
    /// <param name="jumpTime"></param>
    /// <param name="jumpXVelocity"></param>
    /// <param name="jumpYVelocity"></param>
    /// <returns></returns>
    //private bool CheckJumpTrajectory(Vector3 startPos, Vector3 targetPos, Vector2 jumpVelocity)
    //{
    //    float peakJumpTime = CalculateTimeToPeak(jumpVelocity.y);

    //    //Check jump to peak
    //    if (CastAlongTrajectory(startPos, jumpVelocity, peakJumpTime, out RaycastHit hit))
    //    {
    //        return false;
    //    }

    //    //Calculate peak height
    //    float peakPosY = CalculatePeakHeight(startPos.y, jumpVelocity.y);

    //    //Calculate amount of time to land and Remove time based on objBounds
    //    float landTime = CalculateTimeToLandFromPeak(peakPosY, targetPos.y);

    //    //Calculate peak X pos
    //    //V = D/T => D = VT
    //    float peakPosX = startPos.x + jumpVelocity.x * peakJumpTime;

    //    //Check jump from peak to landing
    //    if (CastAlongTrajectory(new Vector3(peakPosX, peakPosY, startPos.z), new Vector2(jumpVelocity.x, 0), landTime, out hit))
    //    {
    //        return false;
    //    }

    //    return true;
    //}



    /// <summary>
    /// Use raycasts to determine if jump trajectory is good
    /// NOTE: We dont use a capsule cast due to its inconsistancy of not hitting because its already inside collider at start
    /// </summary>
    /// <param name="startPos"></param>
    /// <param name="trajectoryVelocity"></param>
    /// <param name="trajectoryTime"></param>
    /// <param name="hit"></param>
    /// <returns></returns>
    //private bool CastAlongTrajectory(Vector3 startPos, Vector2 trajectoryVelocity, float trajectoryTime, out RaycastHit hit)
    //{
    //    float t = 0;
    //    Vector3 prevPos = startPos;
    //    Vector3 nextPos = Vector3.zero;

    //    hit = new RaycastHit();

    //    while (t < 1)
    //    {
    //        //Increments of 10
    //        t += 0.1f;

    //        float time = Mathf.Lerp(0, trajectoryTime, t);

    //        float nextX = startPos.x + trajectoryVelocity.x * time;
    //        float nextY = startPos.y + trajectoryVelocity.y * time + (Physics.gravity.y * time * time) / 2;
    //        nextPos = new Vector3(nextX, nextY, startPos.z);

    //        Vector3 dir = (nextPos - prevPos).normalized;
    //        float dist = (nextPos - prevPos).magnitude;

    //        if (Physics.Raycast(prevPos, dir, out hit, dist, LayerMask.GetMask("Environment")))
    //        {
    //            return true;
    //        }

    //        //Cast to the left
    //        if (CastTrajectoryBranch(nextPos, Vector3.left, objCollider.radius, out hit))
    //        {
    //            return true;
    //        }

    //        //Cast to the right
    //        if (CastTrajectoryBranch(nextPos, Vector3.right, objCollider.radius, out hit))
    //        {
    //            return true;
    //        }

    //        prevPos = nextPos;
    //    }

    //    return false;
    //}


    /// <summary>
    /// Use raycasts to cast out from the main raycast to determin if any objects are within the width of the object
    /// </summary>
    //private bool CastTrajectoryBranch(Vector3 startPos, Vector3 dir, float dist, out RaycastHit hit)
    //{
    //    if (Physics.Raycast(startPos, dir, out hit, dist, LayerMask.GetMask("Environment")))
    //    {
    //        return true;
    //    }

    //    return false;
    //}

    #endregion







}
