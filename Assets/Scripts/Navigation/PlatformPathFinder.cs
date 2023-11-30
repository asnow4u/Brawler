using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;
using UnityEngine.InputSystem.HID;

public class PlatformPathFinder
{
    //TODO: Make Private
    public TerrainNodeFinder terrainFinder;
    
    private CapsuleCollider objCollider;
    private float xVelocityLimit;
    private float yVelocityLimit;

    //TEMP
    public Waypoint root;

    public PlatformPathFinder(CapsuleCollider collider)
    {
        this.objCollider = collider; 
    }


    public void FindPath(Vector3 startPos, Vector3 targetPos, float xVelocity, float jumpVelocity)
    {
        this.xVelocityLimit = xVelocity;
        this.yVelocityLimit = jumpVelocity;

        terrainFinder = new TerrainNodeFinder();
        terrainFinder.MapTerrain(startPos, targetPos, objCollider.bounds);

        root = MapWayPoints(startPos);

    }


    private Waypoint MapWayPoints(Vector3 startPos)
    {        
        List<Waypoint> waypoints = new List<Waypoint>();

        //Set up root wayPoint
        Waypoint rootWaypoint = CreateWayPoint(startPos, -1, 0);
        waypoints.Add(rootWaypoint);        

        //Loop through columns
        for (int i = 0; i < terrainFinder.TerrainNodes.Count; i++)
        {
            List<Waypoint> columnWaypoints = new List<Waypoint>();

            //Loop through rows
            for (int j = 0; j < terrainFinder.TerrainNodes[i].Count; j++)
            {
                TerrainNodeFinder.Node node = terrainFinder.TerrainNodes[i][j];

                if (node.DownHit.collider != null)
                {
                    Waypoint waypoint = CreateWayPoint(node.Pos, i, j);

                    EstablishConnections(waypoint, waypoints);                    

                    columnWaypoints.Add(waypoint);
                }
            }

            waypoints.AddRange(columnWaypoints);
        }

        return waypoints[1];
    }


    private Waypoint CreateWayPoint(Vector3 pos, int columnNum, int rowNum)
    {
        //Set up waypoint GO
        GameObject waypointGO = new GameObject("WayPoint");
        waypointGO.transform.position = pos;
        //Adjust sphere collider to bounds width

        //Set Up wayPoint component
        Waypoint waypoint = waypointGO.AddComponent<Waypoint>();
        waypoint.Column = columnNum;    
        waypoint.Row = rowNum;

        return waypoint;
    }


    private void EstablishConnections(Waypoint waypoint, List<Waypoint> prevWaypoints)
    {
        //Determine what waypoints can connect to this waypoint
        //Walk
        //Jump

        foreach (Waypoint prevWaypoint in prevWaypoints)
        {
            //TODO: Check for start

            //Move Connection
            if (waypoint.Row == prevWaypoint.Row && waypoint.Column - 1 == prevWaypoint.Column)
            {         
                if (CheckMovementPath(prevWaypoint.transform.position, waypoint.transform.position))
                {
                    prevWaypoint.TraversalPoints.Add(new Waypoint.TraversalPoint(waypoint, TraversalType.Move));
                }
            }

            //Upward Jump Connection
            else if (prevWaypoint.Row < waypoint.Row)
            {
                if (DetermineJumpTrajectory(prevWaypoint.transform.position, waypoint.transform.position, xVelocityLimit, yVelocityLimit, out Vector2 jumpVelocity))
                {
                    if (CheckRisingJumpTrajectory(prevWaypoint.transform.position, waypoint.transform.position, jumpVelocity))
                    {
                        prevWaypoint.TraversalPoints.Add(new Waypoint.JumpTraversalPoint(waypoint, TraversalType.Jump, jumpVelocity));
                    }
                }
            }

            //Downward Jump Connection
            else if (prevWaypoint.Row > waypoint.Row)
            {
                if (DetermineJumpTrajectory(prevWaypoint.transform.position, waypoint.transform.position, xVelocityLimit, yVelocityLimit, out Vector2 jumpVelocity))
                {
                    //TODO: Check jump trajectory

                    //prevWaypoint.TraversalPoints.Add(new Waypoint.JumpTraversalPoint(waypoint, TraversalType.Jump, jumpVelocity));
                }
            }

            //Gap Jump
        }
    }


    /// <summary>
    /// Check if any enviroment object are in the way
    /// </summary>
    /// <param name="startPos"></param>
    /// <param name="endPos"></param>
    /// <returns></returns>
    private bool CheckMovementPath(Vector3 startPos, Vector3 endPos)
    {
        Vector3 dir = (endPos - startPos).normalized;
        float dist = (endPos - startPos).magnitude;

        Vector3 CapsulePoint1 = new Vector3(startPos.x, startPos.y + objCollider.bounds.size.y / 2, startPos.z);
        Vector3 CapsulePoint2 = new Vector3(startPos.x, startPos.y - objCollider.bounds.size.y / 2, startPos.z);

        if (Physics.CapsuleCast(CapsulePoint1, CapsulePoint2, objCollider.radius, dir, out RaycastHit hit, dist, LayerMask.GetMask("Environment")))
        {
            return false;
        }

        return true;
    }



    /// <summary>
    /// Determines the jump velocity needed using the following equations
    /// X = X0 + Vx(T)
    /// Y = Y0 + Vy(T) - (g(T)^2) / 2
    /// 
    /// Kinematic Equations
    /// V^2 = V0^2 + 2 * G * (Y - Y0)  
    /// V = V0 + G(T)
    /// (Y0 - Y) = V0(T) + 1/2 * G * T^2
    /// </summary>
    private bool DetermineJumpTrajectory(Vector3 startPos, Vector3 targetPos, float maxXVelocity, float maxYVelocity, out Vector2 jumpVelocity)
    {
        //Determine jump height capability and target peak
        float maxJumpTime = Mathf.Abs(maxYVelocity / Physics.gravity.y); //a = v/t => t = v/a
        float maxJumpHeight = startPos.y + maxYVelocity * maxJumpTime + (Physics.gravity.y * maxJumpTime * maxJumpTime) / 2;
        float jumpPeak = Mathf.Max( startPos.y + objCollider.bounds.size.y, targetPos.y + objCollider.bounds.size.y); 

        //Determine if jump is possible
        if (maxJumpHeight > jumpPeak)
        {
            //Calculate yVelocity to reach jumpPeak
            //V^2 = V0^2 + 2 * G * (Y - Y0) => V = Sqrt(2 * G * (Y - Y0)) 
            float jumpYVelocity = Mathf.Sqrt( Mathf.Abs(2 * Physics.gravity.y * (jumpPeak - startPos.y)));

            //Calculate the time it takes to peak and then land on target
            //V = V0 + G(T) => T = V/G
            //(Y0 - Y) = V0(T) + 1/2 * G * T^2 => T = Sqrt(2(Y0 - Y) / G)
            float jumpTime = -jumpYVelocity / Physics.gravity.y + Mathf.Sqrt( Mathf.Abs(2 * (jumpPeak - targetPos.y) / Physics.gravity.y));

            //Calculate how much xVelocity is needed
            //V = D / T
            float jumpXVelocity = (targetPos.x - startPos.x) / jumpTime;

                           
            jumpVelocity = new Vector2(jumpXVelocity, jumpYVelocity);
            return true;
        }

        jumpVelocity = Vector2.zero;
        return false;
    }


    /// <summary>
    /// Check if jump trajectory hits any other environment objects
    /// </summary>
    /// <param name="startPos"></param>
    /// <param name="jumpTime"></param>
    /// <param name="jumpXVelocity"></param>
    /// <param name="jumpYVelocity"></param>
    /// <returns></returns>
    private bool CheckRisingJumpTrajectory(Vector3 startPos, Vector3 targetPos, Vector2 jumpVelocity)
    {
        float t = 0f;

        Vector3 prevPos = startPos;
        Vector3 nextPos = Vector3.zero;

        //Find jump time (V = D / T => T = D / V)         
        float jumpTime = (targetPos.y - startPos.y) / jumpVelocity.y;

        //Remove time based on objBounds 
        jumpTime -= (objCollider.bounds.size.y + 1) / jumpVelocity.y;

        while (t < 1)
        {
            //Increments of 10
            t += 0.1f; 

            float time = Mathf.Lerp(0, jumpTime, t);

            float nextX = startPos.x + jumpVelocity.x * time;
            float nextY = startPos.y + jumpVelocity.y * time + (Physics.gravity.y * time * time) / 2;
            nextPos = new Vector3(nextX, nextY, startPos.z);

            Vector3 dir = (nextPos - prevPos).normalized;
            float dist = (nextPos - prevPos).magnitude;

            Vector3 CapsulePoint1 = new Vector3(prevPos.x, prevPos.y + objCollider.bounds.size.y / 2, prevPos.z);
            Vector3 CapsulePoint2 = new Vector3(prevPos.x, prevPos.y - objCollider.bounds.size.y / 2, prevPos.z);

            if (Physics.CapsuleCast(CapsulePoint1, CapsulePoint2, objCollider.radius, dir, out RaycastHit hit, dist, LayerMask.GetMask("Environment")))
            {
                return false;
            }

            prevPos = nextPos;
        }

        return true;
    }
}
