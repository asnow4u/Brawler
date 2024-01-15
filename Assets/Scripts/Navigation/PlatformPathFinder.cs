using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;
using UnityEngine.InputSystem.HID;
using static UnityEditor.PlayerSettings;
using static Waypoint;

public class PlatformPathFinder : PathFinder
{
    private CapsuleCollider objCollider;

    //TODO: Save the collection instead
    private float xVelocityLimit;
    private float xAirVelocityLimit;
    private float yVelocityLimit;
    private MoveData moveData;

    //TODO: Make Private
    public TerrainNodeMapper terrainFinder;

    //NOTE: Do we keep? Upon updating waypoints this will not always be correct (Moving platform)
    private List<Waypoint> waypoints;

    //TEMP
    public Waypoint rootWaypoint;
    public Waypoint targetWaypoint;

    public PlatformPathFinder(CapsuleCollider collider)
    {
        this.objCollider = collider;
    }


    public List<Waypoint> FindPath(Vector3 startPos, Vector3 targetPos, MovementCollection curMovementCollection)
    {
        waypoints = new List<Waypoint>();

        if (curMovementCollection.TryGetMovementByType(MovementType.Move, out MovementData moveData) &&
            curMovementCollection.TryGetMovementByType(MovementType.Jump, out MovementData jumpData))
        {
            this.moveData = (MoveData)moveData;
            this.xVelocityLimit = ((MoveData)moveData).XVelocityAcceleration;
            this.xAirVelocityLimit = ((MoveData)moveData).XAirVelocityAcceleration;
            this.yVelocityLimit = ((JumpData)jumpData).JumpVelocity;

            //Determine maxJump height        
            float maxJumpHeight = CalculatePeakHeight(startPos.y, ((JumpData)jumpData).JumpVelocity);

            //Map out the terrain
            //terrainFinder = new TerrainNodeFinder();
            //terrainFinder.MapTerrain(startPos, targetPos, maxJumpHeight, objCollider.bounds);

            //Connect possible waypoints
            rootWaypoint = MapConnections(startPos);

            //Find target waypoint
            targetWaypoint = FindClosestWayPoint(rootWaypoint, targetPos);

            List<Waypoint> pathPoints = DeterminePath(rootWaypoint, targetWaypoint);

            return pathPoints;
        }
        
        return null;
    }


    private Waypoint MapConnections(Vector3 startPos)
    {
        //Set up root wayPoint
        Waypoint rootWaypoint = new Waypoint(startPos, -1, 0);
        waypoints.Add(rootWaypoint);

        //Loop through columns
        //for (int i = 0; i < terrainFinder.TerrainNodes.Count; i++)
        //{
        //    List<Waypoint> columnWaypoints = new List<Waypoint>();

        //    //Loop through rows
        //    for (int j = 0; j < terrainFinder.TerrainNodes[i].Count; j++)
        //    {
        //        TerrainNode node = terrainFinder.TerrainNodes[i][j];

        //        if (node.DownCollision != null)
        //        {
        //            Waypoint waypoint = new Waypoint(new Vector3(node.DownCollision.CollisionPoint.x, node.DownCollision.CollisionPoint.y + objCollider.height / 2 + objCollider.radius), i, j);

        //            EstablishConnectionsTo(waypoint);

        //            columnWaypoints.Add(waypoint);
        //        }
        //    }

        //    waypoints.AddRange(columnWaypoints);
        //}

        return waypoints[0];
    }


    /// <summary>
    /// Determin the closest waypoint given a position 
    /// The waypoint will be determined based on connections
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    private Waypoint FindClosestWayPoint(Waypoint curWaypoint, Vector3 pos, Waypoint closestWaypoint = null)
    {
        if (closestWaypoint == null)
            closestWaypoint = curWaypoint;        
        else
        {
            if (Vector3.Distance(pos, closestWaypoint.pos) > Vector3.Distance(pos, curWaypoint.pos))
            {
                closestWaypoint = curWaypoint;
            }
        }


        foreach (TraversalPoint traversalPoint in curWaypoint.TraversalPoints)
        {
            Waypoint waypoint = FindClosestWayPoint(traversalPoint.Destination, pos, closestWaypoint);

            if (Vector3.Distance(pos, closestWaypoint.pos) > Vector3.Distance(pos, waypoint.pos))
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
        foreach (Waypoint prevWaypoint in waypoints)
        {
            //Move Connection
            if (waypoint.Row == prevWaypoint.Row && waypoint.Column - 1 == prevWaypoint.Column)
            {
                if (CheckMovementPath(prevWaypoint.pos, waypoint.pos))
                {
                    prevWaypoint.TraversalPoints.Add(new TraversalPoint(waypoint, TraversalType.Move));
                }
            }

            //Upward Jump Connection
            else if (prevWaypoint.Row < waypoint.Row)
            {
                if (DetermineJumpTrajectory(prevWaypoint.pos, waypoint.pos, out Vector2 jumpVelocity))
                {
                    prevWaypoint.TraversalPoints.Add(new JumpTraversalPoint(waypoint, TraversalType.Jump, jumpVelocity));
                }
            }

            //Downward Jump Connection
            else if (prevWaypoint.Row > waypoint.Row)
            {
                if (DetermineJumpTrajectory(prevWaypoint.pos, waypoint.pos, out Vector2 jumpVelocity))
                {
                    prevWaypoint.TraversalPoints.Add(new JumpTraversalPoint(waypoint, TraversalType.Jump, jumpVelocity));

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
    private bool DetermineJumpTrajectory(Vector3 startPos, Vector3 targetPos, out Vector2 jumpVelocity)
    {
        //Determine jump height capability and target peak
        float maxJumpHeight = CalculatePeakHeight(startPos.y, yVelocityLimit);
        float jumpPeak = Mathf.Max(startPos.y + objCollider.bounds.size.y, targetPos.y + objCollider.bounds.size.y);

        //Determine if jump height is possible
        if (maxJumpHeight > jumpPeak)
        {
            //Calculate yVelocity to reach jumpPeak
            //V^2 = V0^2 + 2 * G * (Y - Y0) => V = Sqrt(2 * G * (Y - Y0)) 
            float jumpYVelocity = Mathf.Sqrt(Mathf.Abs(2 * Physics.gravity.y * (jumpPeak - startPos.y)));

            //Calculate the time it takes to peak and then land on target
            float jumpTime = CalculateJumpTime(startPos.y, targetPos.y, jumpYVelocity);

            Debug.Log("Start: " + startPos.x + " Target: " + targetPos.x + "\nVelocity: " + jumpYVelocity + "\nTime: " + jumpTime);

            //Determine distance of jump

            //Calculate how long acceleration will apply till cap is reached
            //V = V0 + A * T => T = (V - V0) / A | Note: V0 will start at 0
            //float accelerationTime = moveData.XVelocityLimit / moveData.XAirVelocityAcceleration;

            //Calculate distance traveled while accelerating
            //V^2 = V0^2 + 2 * A * D => (V^2 - V0^20) / (2 * A) | Note: V0 will start at 0
            //float accelerationDist = (moveData.XVelocityLimit * moveData.XVelocityLimit) / (2 * moveData.XAirVelocityAcceleration);

            //float dist = accelerationDist + (moveData.XVelocityLimit * (jumpTime - accelerationTime));
            float dist = moveData.XVelocityLimit * jumpTime;
                                             
            
            //Determin if jump distance is possible
            if (dist > Mathf.Abs(targetPos.x - startPos.x)) 
            {
                
                //TODO: Calculate JumpxVelocity for jump
                //NOTE: Currently the enemy is traveling at their max limit.
                //What we set for the x jumpVelocity is sending the acceleration that should be applied. 
                //What needs to happen is we need to determine what deceleration we need to apply 
                //Need to implement drag based on rb
                //This might also need to be calculated just before the jump to have all the accurate data (moveSpeed and rb)
                //Probably need to increase threshold of CheckJumpTrajectory

                float jumpXVelocity = (targetPos.x - startPos.x) / jumpTime;

                //Create jump Velocity
                jumpVelocity = new Vector2(jumpXVelocity, jumpYVelocity);

                //Check for obsticals
                if (CheckJumpTrajectory(startPos, targetPos, jumpVelocity))
                {
                    return true;
                }
            
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
        float landTime = CalculateTimeToLandFromPeak(peakPosY, targetPos.y);

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
        float landTime = CalculateTimeToLandFromPeak(peakHeight, endPos);

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
    private float CalculateTimeToLandFromPeak(float startPos, float endPos)
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
            foreach (TraversalPoint traversalPoint in waypoint.TraversalPoints)
            {
                if (traversalPoint.Destination == target)
                {
                    return DeterminePath(traversalPoint.Destination, target, curTraversalPath);
                }
            }

            //Randomly choose a traversal path till one succeeded
            List<Waypoint> traversalPath = null;
            List<TraversalPoint> availablePaths = new List<TraversalPoint>(waypoint.TraversalPoints);

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
