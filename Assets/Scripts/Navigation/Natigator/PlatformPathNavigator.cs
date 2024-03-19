using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class PlatformPathNavigator : PathNavigator
{
    private PlatformPathFinder platformPathFinder => (PlatformPathFinder)pathFinder;

    public override void Initialize(Action<TraversalType, float> actionCallback)
    {
        base.Initialize(actionCallback);

        pathFinder = new PlatformPathFinder(gameObject);
    }


    protected override Graph CreateGraph(TerrainNode startNode, TerrainNode endNode)
    {
        GraphFactory graphFactory = new GraphFactory();
        return graphFactory.CreateGraph(GraphType.Platform, startNode, endNode, bounds, moveHandler.CurMovementCollection);
    }


    /// <summary>
    /// Perform movement towards the curPathPoint
    /// Movement will either be moving on the ground or jumping
    /// </summary>
    protected override void PerformMovement()
    {
        //Movement
        if (curPathPoint.TraversalType == TraversalType.Move)
        {
            //Determine direction

            //To The Right
            if (transform.position.x < curPathPoint.Pos.x)
            {
                movementCallback(TraversalType.Move, 1f);
            }

            //To The Left
            else
            {
                movementCallback(TraversalType.Move, -1f);
            }
        }

        //Jump
        else if (curPathPoint.TraversalType == TraversalType.Jump)
        {
            JumpPathPoint jumpPoint = (JumpPathPoint)curPathPoint;

            Debug.Log("Jump Influence:" +
            "\nMovement: " + jumpPoint.JumpXInfluence +
            "\nJump: " + jumpPoint.JumpYInfluence +
            "\nInitialVelocity: " + jumpPoint.InitialVelocity);

            //Move
            movementCallback(TraversalType.Move, 0);
            
            //Must be moving towards the point before jumpoint
            if ((rb.velocity.x > 0 && (jumpPoint.Pos.x - transform.position.x) > 0) ||
                rb.velocity.x < 0 && (jumpPoint.Pos.x - transform.position.x) < 0)
            {
                movementCallback(TraversalType.Jump, jumpPoint.JumpYInfluence);
            }
        }
    }







    //protected override void CreatePathFinder(MovementCollection collection)
    //{
    //    pathFinder = new PlatformPathFinder(collection, GetComponent<Collider>().bounds);
    //}


    //protected override List<TraversalNode> CreatePath(Vector3 start, Vector3 end)
    //{
    //    return platformPathFinder.CreatePath(start, end);
    //}






    //NOTE: Later:
    //Be able to take in a list of targets
    //Be able to Update the path based on a new target




    //private void OnDrawGizmos()
    //{
    //    if (Application.isPlaying
    //        && path != null)
    //    {            
    //        for (int i=0; i < path.Count; i++)
    //        {
    //            Gizmos.color = Color.blue;
    //            Gizmos.DrawSphere(path[i].Destination.Pos, 0.1f);

    //            if (i > 0)
    //            {
    //                //Connections
    //                Gizmos.color = Color.black;
    //                switch (path[i].TraversalType)
    //                {
    //                    case TraversalType.Move:
    //                        Gizmos.DrawLine(path[i-1].Destination.Pos, path[i].Destination.Pos);
    //                        break;

    //                    case TraversalType.Jump:
    //                        //DrawJumpTrjectory(path[i-1].Destination.Pos, (JumpTraversalNode)path[i]);
    //                        break;
    //                }
    //            }
    //        }
    //    }
    //}


    //private void DrawJumpTrjectory(Vector3 startPoint, JumpTraversalNode jumpPoint)
    //{
    //    Gizmos.color = Color.white;

    //    float t = 0f;

    //    float jumpTime = Mathf.Abs(jumpPoint.JumpVelocity.y / Physics.gravity.y);
    //    float jumpPeak = startPoint.y + ((jumpPoint.JumpVelocity.y * jumpTime) + (0.5f * Physics.gravity.y * jumpTime * jumpTime));
    //    float landTime = Mathf.Sqrt(Mathf.Abs((jumpPeak - jumpPoint.Destination.Pos.y) / (0.5f * Physics.gravity.y)));

    //    float maxTime = jumpTime + landTime;

    //    float prevX = startPoint.x;
    //    float prevY = startPoint.y;
    //    float nextX = 0;
    //    float nextY = 0;

    //    while (t < 1)
    //    {
    //        t += Time.fixedDeltaTime;

    //        float time = Mathf.Lerp(0, maxTime, t);

    //        nextX = startPoint.x + jumpPoint.JumpVelocity.x * time;
    //        nextY = startPoint.y + (jumpPoint.JumpVelocity.y * time + (0.5f * Physics.gravity.y * time * time));

    //        Gizmos.DrawLine(new Vector3(prevX, prevY, transform.position.z), new Vector3(nextX, nextY, transform.position.z));

    //        prevX = nextX;
    //        prevY = nextY;
    //    }
    //}
}

