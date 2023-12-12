using System;
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

    private List<Waypoint> waypoints = new List<Waypoint>();


    private CapsuleCollider objCollider;
    private float xVelocityLimit;
    private float yVelocityLimit;

    //TEMP
    public Waypoint startWaypoint;
    public Waypoint targetWaypoint;

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

        MapWayPoints(startPos);

        startWaypoint = FindWayPointBy(startPos);
        targetWaypoint = FindWayPointBy(targetPos);

        List<Waypoint> pathPoints = DeterminePath(startWaypoint, targetWaypoint);

        //Debug
        string log = "Found Path:";
        foreach (Waypoint waypoint in pathPoints)
        {
            log += "\n\t" + waypoint.gameObject.name;
        }
        Debug.Log(log);
    }


    private void MapWayPoints(Vector3 startPos)
    {
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
                    Waypoint waypoint = CreateWayPoint(new Vector3(node.DownHit.point.x, node.DownHit.point.y + objCollider.height / 2 + objCollider.radius, node.DownHit.point.z), i, j);

                    EstablishConnectionsTo(waypoint);

                    columnWaypoints.Add(waypoint);
                }
            }

            waypoints.AddRange(columnWaypoints);
        }
    }


    private Waypoint CreateWayPoint(Vector3 pos, int columnNum, int rowNum)
    {
        //Set up waypoint GO
        GameObject waypointGO = new GameObject("WayPoint C" + columnNum + "R" + rowNum);
        waypointGO.transform.position = pos;
        //Adjust sphere collider to bounds width

        //Set Up wayPoint component
        Waypoint waypoint = waypointGO.AddComponent<Waypoint>();
        waypoint.Column = columnNum;
        waypoint.Row = rowNum;

        return waypoint;
    }


    /// <summary>
    /// Determin the closest waypoint given a position
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    private Waypoint FindWayPointBy(Vector3 pos)
    {
        Waypoint closestWaypoint = null;

        foreach (Waypoint waypoint in waypoints)
        {
            if (closestWaypoint == null)
            {
                closestWaypoint = waypoint;
                continue;
            }

            if (Vector3.Distance(pos, closestWaypoint.transform.position) > Vector3.Distance(pos, waypoint.transform.position))
            {
                closestWaypoint = waypoint;
            }
        }

        return closestWaypoint;
    }



    #region WayPoint Connections

    private void EstablishConnectionsTo(Waypoint waypoint)
    {
        //Determine what waypoints can connect to this waypoint
        //Walk
        //Jump

        foreach (Waypoint prevWaypoint in waypoints)
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
                    prevWaypoint.TraversalPoints.Add(new Waypoint.JumpTraversalPoint(waypoint, TraversalType.Jump, jumpVelocity));
                }
            }

            //Downward Jump Connection
            else if (prevWaypoint.Row > waypoint.Row)
            {
                if (DetermineJumpTrajectory(prevWaypoint.transform.position, waypoint.transform.position, xVelocityLimit, yVelocityLimit, out Vector2 jumpVelocity))
                {
                    prevWaypoint.TraversalPoints.Add(new Waypoint.JumpTraversalPoint(waypoint, TraversalType.Jump, jumpVelocity));

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
        float jumpPeak = Mathf.Max(startPos.y + objCollider.bounds.size.y, targetPos.y + objCollider.bounds.size.y);

        //Determine if jump is possible
        if (maxJumpHeight > jumpPeak)
        {
            //Calculate yVelocity to reach jumpPeak
            //V^2 = V0^2 + 2 * G * (Y - Y0) => V = Sqrt(2 * G * (Y - Y0)) 
            float jumpYVelocity = Mathf.Sqrt(Mathf.Abs(2 * Physics.gravity.y * (jumpPeak - startPos.y)));

            //Calculate the time it takes to peak and then land on target
            float jumpTime = CalculateJumpTime(startPos.y, targetPos.y, jumpYVelocity);

            //Calculate how much xVelocity is needed
            //V = D / T
            float jumpXVelocity = (targetPos.x - startPos.x) / jumpTime;

            jumpVelocity = new Vector2(jumpXVelocity, jumpYVelocity);

            //Check for obsticals
            if (CheckJumpTrajectory(startPos, targetPos, jumpVelocity))
            {
                return true;
            }
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
    private bool CheckJumpTrajectory(Vector3 startPos, Vector3 targetPos, Vector2 jumpVelocity)
    {
        float peakJumpTime = CalculateTimeToPeak(jumpVelocity.y);

        //Check jump to peak
        if (CastAlongTrajectory(startPos, jumpVelocity, peakJumpTime, out RaycastHit hit))
        {
            return false;
        }

        //Calculate peak height
        float peakPosY = CalculatePeakHeight(startPos.y, jumpVelocity.y);

        //Calculate amount of time to land and Remove time based on objBounds
        float landTime = CalculateTimeToTargetFromPeak(peakPosY, targetPos.y);

        //Calculate peak X pos
        //V = D/T => D = VT
        float peakPosX = startPos.x + jumpVelocity.x * peakJumpTime;

        //Check jump from peak to landing
        if (CastAlongTrajectory(new Vector3(peakPosX, peakPosY, startPos.z), new Vector2(jumpVelocity.x, 0), landTime, out hit))
        {
            return false;
        }

        return true;
    }


    /// <summary>
    /// Calculate jump time based on upward velocity
    /// </summary>
    /// <param name="yVelocity"></param>
    /// <param name=""></param>
    private float CalculateJumpTime(float startPos, float endPos, float yVelocity)
    {
        float peakTime = CalculateTimeToPeak(yVelocity);
        float peakHeight = CalculatePeakHeight(startPos, yVelocity);
        float landTime = CalculateTimeToTargetFromPeak(peakHeight, endPos);

        return peakTime + landTime;
    }


    private float CalculatePeakHeight(float startPos, float yVelocity)
    {
        float peakTime = CalculateTimeToPeak(yVelocity);

        //Calculate the height of the peak
        //(Y0 - Y) = V0(T) + 1/2 * G * T^2 => Y = V0(T) + 1/2 * G * T^2 + Y0
        return startPos + yVelocity * peakTime + (Physics.gravity.y * peakTime * peakTime) / 2;
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
    /// Return amount of time needed to fall from a start pos and land at end pos
    /// </summary>
    /// <param name="startPos"></param>
    /// <param name="endPos"></param>
    /// <returns></returns>
    private float CalculateTimeToTargetFromPeak(float startPos, float endPos)
    {
        //Calculate the time it takes to land from peak
        //(Y0 - Y) = V0(T) + 1/2 * G * T^2 => T = Sqrt(2(Y0 - Y) / G)
        return Mathf.Sqrt(Mathf.Abs((2 * (endPos - startPos)) / Physics.gravity.y));
    }


    /// <summary>
    /// Use raycasts to determine if jump trajectory is good
    /// NOTE: We dont use a capsule cast due to its inconsistancy of not hitting because its already inside collider at start
    /// </summary>
    /// <param name="startPos"></param>
    /// <param name="trajectoryVelocity"></param>
    /// <param name="trajectoryTime"></param>
    /// <param name="hit"></param>
    /// <returns></returns>
    private bool CastAlongTrajectory(Vector3 startPos, Vector2 trajectoryVelocity, float trajectoryTime, out RaycastHit hit)
    {
        float t = 0;
        Vector3 prevPos = startPos;
        Vector3 nextPos = Vector3.zero;

        hit = new RaycastHit();

        while (t < 1)
        {
            //Increments of 10
            t += 0.1f;

            float time = Mathf.Lerp(0, trajectoryTime, t);

            float nextX = startPos.x + trajectoryVelocity.x * time;
            float nextY = startPos.y + trajectoryVelocity.y * time + (Physics.gravity.y * time * time) / 2;
            nextPos = new Vector3(nextX, nextY, startPos.z);

            Vector3 dir = (nextPos - prevPos).normalized;
            float dist = (nextPos - prevPos).magnitude;

            if (Physics.Raycast(prevPos, dir, out hit, dist, LayerMask.GetMask("Environment")))
            {
                return true;
            }

            //Cast to the left
            if (CastTrajectoryBranch(nextPos, Vector3.left, objCollider.radius, out hit))
            {
                return true;
            }

            //Cast to the right
            if (CastTrajectoryBranch(nextPos, Vector3.right, objCollider.radius, out hit))
            {
                return true;
            }

            prevPos = nextPos;
        }

        return false;
    }


    /// <summary>
    /// Use raycasts to cast out from the main raycast to determin if any objects are within the width of the object
    /// </summary>
    private bool CastTrajectoryBranch(Vector3 startPos, Vector3 dir, float dist, out RaycastHit hit)
    {
        Debug.DrawLine(startPos, startPos + dir * dist, Color.red);
        if (Physics.Raycast(startPos, dir, out hit, dist, LayerMask.GetMask("Environment")))
        {
            return true;
        }

        return false;
    }

    #endregion


    private List<Waypoint> DeterminePath(Waypoint waypoint, Waypoint target, List<Waypoint> curTraversalPath = null)
    {
        //For First time, create list
        if (curTraversalPath == null)
            curTraversalPath = new List<Waypoint>();

        //Check if target has been found
        if (waypoint.Column == target.Column && waypoint.Row == target.Row)
        {
            curTraversalPath.Add(target);                       
            return curTraversalPath; 
        }

        //Move to Traversal points
        if (waypoint.TraversalPoints.Count > 0) 
        {
            curTraversalPath.Add(waypoint);

            //Check if target is one of the traversal points
            foreach (Waypoint.TraversalPoint traversalPoint in waypoint.TraversalPoints)
            {
                if (traversalPoint.Destination == target)
                {
                    return DeterminePath(traversalPoint.Destination, target, curTraversalPath);
                }
            }

            //Randomly choose a traversal path till one succeeded
            List<Waypoint> traversalPath = null;
            List<Waypoint.TraversalPoint> availablePaths = new List<Waypoint.TraversalPoint>(waypoint.TraversalPoints);

            while (traversalPath == null)
            {
                if (availablePaths.Count == 0)
                {
                    curTraversalPath.Remove(waypoint);
                    return null;
                }
                
                int rand = UnityEngine.Random.Range(0, availablePaths.Count);
                traversalPath = DeterminePath(availablePaths[rand].Destination, target, curTraversalPath);

                if (traversalPath != null)
                {
                    return traversalPath;
                }

                else
                    availablePaths.RemoveAt(rand);
            }
        }

        return null;
    }
}
