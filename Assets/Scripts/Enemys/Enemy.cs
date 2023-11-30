using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.InputSystem.HID;
using UnityEngine.UIElements;

public abstract class Enemy : SceneObject
{
    protected enum EnemyState { Idle, Alert, Attack }
    protected EnemyState enemyState;

    protected PlatformPathFinder pathFinder;

    [SerializeField] private Transform headTransform;

    //View
    [Header("View")]
    [SerializeField] private float aggroDistance = 5f;
    [SerializeField] private float maxSightDistance = 10f;
    [SerializeField] private float maxAngle = 30f;
    protected SceneObject aggroTarget;

    [SerializeField] private float alertTimerDuration = 1.5f;
    [SerializeField] private float lostTargetTimerDuration = 10f;
    private float alertTimer;
    private float lostTargetTimer;

    //Patrol
    [Header("Idle Patrol")]
    [SerializeField] protected List<Vector2> patrolSpots;
    [SerializeField] protected float patrolWaitDuration;
    protected int patrolIndex;
    protected float patrolTimer;

    [SerializeField] protected Vector2 targetSpot;


    //TEMP
    public Transform pathObject;

    protected override void Initialize()
    {
        base.Initialize();

        //Check distances
        if (aggroDistance > maxSightDistance) { Debug.LogError("Aggro distance cant be higher then sight distance", gameObject); }        

        ObjectType = SceneObjectType.Enemy;

        SetState(EnemyState.Idle);

     
        pathFinder = new PlatformPathFinder(GetComponent<CapsuleCollider>());
        StartCoroutine(FindPath());
       
        

        if (patrolSpots.Count > 0 )
        {
            targetSpot = patrolSpots[0];
        }
    }


    //TEMP
    float moveSpeed = 10;
    float jumpVelocity = 10f;
    public IEnumerator FindPath()
    {          

        //while (true)
        //{
        pathFinder.FindPath(transform.position, pathObject.position, moveSpeed, jumpVelocity);
        yield return new WaitForSeconds(1f);
        //}
    }


    private void SetState(EnemyState state)
    {
        enemyState = state;

        alertTimer = alertTimerDuration;
        lostTargetTimer = lostTargetTimerDuration;

        if (state == EnemyState.Idle)
            aggroTarget = null;
    }


    private void SetAggroTarget(SceneObject sceneObj)
    {
        aggroTarget = sceneObj;
        alertTimer = alertTimerDuration;
        lostTargetTimer = lostTargetTimerDuration;

        //Determine if in aggro range
        if (Vector3.Distance(transform.position, aggroTarget.transform.position) < aggroDistance)
        {
            SetState(EnemyState.Attack);
        }

        else
        {
            SetState(EnemyState.Alert);
        }
    }


    protected void Update()
    {        
        switch (enemyState)
        {
            //case EnemyState.Idle:
            //    Idle();
            //    break;

            //case EnemyState.Alert:
            //    Alert(); 
            //    break;

            //case EnemyState.Attack:
            //    Attack();
            //    break;
        }
    }


    protected abstract void Idle();
    protected abstract void Alert();
    protected abstract void Attack();    


    protected void IncrementPatrolIndex()
    {
        patrolIndex++;

        if (patrolIndex >= patrolSpots.Count)
        {
            patrolIndex = 0;
        }

        targetSpot = patrolSpots[patrolIndex];
    }


    protected bool CheckRangeOfTarget()
    {
        if (targetSpot != null)
        {
            if (Vector3.Distance(transform.position, targetSpot) < 1f)
            {
                return true;
            }
        }

        return false;
    }


    protected void MoveTowardsTargetSpot()
    {
        Vector2 direction = targetSpot - new Vector2(transform.position.x, transform.position.y);
        Debug.Log(direction);
        movementHandler.PerformMovement(direction);
    }


    protected IEnumerator StartPartrolWaitTimer()
    {
        patrolTimer = patrolWaitDuration;
        movementHandler.PerformMovement(Vector2.zero);

        while (patrolTimer > 0)
        {
            patrolTimer -= Time.deltaTime;
            yield return null;
        }

        patrolTimer = 0;
        IncrementPatrolIndex();
    }


    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        List<SceneObject> objectsInView = CheckView();

        //Set target to player or first object seen
        if (aggroTarget == null)
        {
            if (objectsInView.Count > 0)
            {
                SceneObject player = objectsInView.FirstOrDefault(x => x.ObjectType == SceneObjectType.Player);

                if (player != null)
                    SetAggroTarget(player);
                else 
                    SetAggroTarget(objectsInView[0]);
            }
        }

        else
        {
            //Prioritize Player
            if (aggroTarget.ObjectType != SceneObjectType.Player)
            {
                if (objectsInView.Count > 0)
                {
                    SceneObject player = objectsInView.FirstOrDefault(x => x.ObjectType == SceneObjectType.Player);

                    if (player != null)
                    {
                        SetAggroTarget(player);
                    }
                }
            }

            //Check if within aggro distance
            if (enemyState == EnemyState.Alert) 
            {
                if (Vector3.Distance(transform.position, aggroTarget.transform.position) < aggroDistance)
                {
                    SetState(EnemyState.Attack);
                }
            }            

            //Update timers
            UpdateTimers(objectsInView.Contains(aggroTarget));            
        }
    }


    private List<SceneObject> CheckView()
    {        
        List<SceneObject> foundObjs = new List<SceneObject>();
        float rad = 1;

        Vector3 upperPoint = new Vector3(headTransform.position.x, headTransform.position.y + maxSightDistance * Mathf.Tan(Mathf.Deg2Rad * maxAngle) - rad);
        Vector3 lowerPoint = new Vector3(headTransform.position.x, headTransform.position.y - maxSightDistance * Mathf.Tan(Mathf.Deg2Rad * maxAngle) + rad);
        RaycastHit[] hits = Physics.CapsuleCastAll(lowerPoint, upperPoint, rad, headTransform.right, maxSightDistance, LayerMask.GetMask("SceneObject"));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.TryGetComponent(out SceneObject sceneObj)) 
            { 
                //Dont include yourself
                if (sceneObj.UniqueId == UniqueId) { continue; }

                //Check sight angle
                if (Vector3.Angle(headTransform.right, hit.point - headTransform.position) < maxAngle)
                {
                    //Check for Obscured
                    if (Physics.Raycast(headTransform.position, hit.collider.transform.position - headTransform.position, LayerMask.GetMask("SceneObject", "Environment")))
                    {
                        Debug.DrawLine(headTransform.position, hit.point, Color.red);
                        foundObjs.Add(hit.collider.GetComponent<SceneObject>());
                    }                                                
                }
            
            }

        }

        return foundObjs;
    }


    private void UpdateTimers(bool targetInView)
    {
        if (targetInView)
        {
            switch (enemyState)
            {
                //Increase awareness of sceneObject
                case EnemyState.Alert:                    
                    alertTimer -= Time.fixedDeltaTime;

                    if (alertTimer < 0)
                        SetState(EnemyState.Attack);

                    break;

                //Reset Timer for losing target
                case EnemyState.Attack:
                    lostTargetTimer = lostTargetTimerDuration;
                    break;
            }
        }

        else
        {
            switch (enemyState)
            {
                case EnemyState.Alert:
                    alertTimer += Time.fixedDeltaTime;

                    if (alertTimer > alertTimerDuration)                    
                        SetState(EnemyState.Idle);
                    
                    break;

                case EnemyState.Attack:
                    lostTargetTimer -= Time.fixedDeltaTime;
                    
                    if (lostTargetTimer < 0)
                        SetState(EnemyState.Alert);

                    break;
            }
        }
    }


    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(headTransform.position, headTransform.position + new Vector3(maxSightDistance * Mathf.Cos(Mathf.Deg2Rad * maxAngle), maxSightDistance * Mathf.Sin(Mathf.Deg2Rad * maxAngle), headTransform.position.z));

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(headTransform.position, headTransform.position + headTransform.right * maxSightDistance);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(new Vector3(headTransform.position.x, headTransform.position.y + 0.3f, headTransform.position.z), new Vector3(headTransform.position.x, headTransform.position.y + 0.3f, headTransform.position.z) + headTransform.right * aggroDistance);

        Gizmos.color = Color.yellow; // Set the color of the sphere
        Gizmos.DrawWireSphere(new Vector3(headTransform.position.x, headTransform.position.y + maxSightDistance * Mathf.Tan(Mathf.Deg2Rad * maxAngle) - 1, headTransform.position.z), 1);
        Gizmos.DrawWireSphere(new Vector3(headTransform.position.x, headTransform.position.y - maxSightDistance * Mathf.Tan(Mathf.Deg2Rad * maxAngle) + 1, headTransform.position.z), 1);


        if (pathFinder != null)
        {
            Bounds bounds = GetComponent<CapsuleCollider>().bounds;

            //Draw Nodes
            foreach (List<TerrainNodeFinder.Node> rowNodes in pathFinder.terrainFinder.TerrainNodes)
            {
                foreach (TerrainNodeFinder.Node node in rowNodes)
                {
                    if (node.HorizontalInsideTerrainCheck || node.VerticalInsideTerrainCheck)
                        Gizmos.color = Color.black;
                    else
                        Gizmos.color = Color.red;

                    Gizmos.DrawCube(node.Pos, Vector3.one * 0.2f);

                    if (node.UpHit.collider != null)
                    {
                        Gizmos.color = Color.cyan;
                        Gizmos.DrawLine(node.Pos, node.Pos + Vector3.up * bounds.size.y);
                    }

                    if (node.DownHit.collider != null)
                    {
                        Gizmos.color = Color.magenta;
                        Gizmos.DrawLine(node.Pos, node.Pos - Vector3.up * bounds.size.y);
                    }

                    if (node.RightHit.collider != null)
                    {
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawLine(node.Pos, node.Pos + Vector3.right * bounds.size.x);
                    }

                    if (node.LeftHit.collider != null)
                    {
                        Gizmos.color = Color.yellow;
                        Gizmos.DrawLine(node.Pos, node.Pos - Vector3.right * bounds.size.x);
                    }
                }
            }

            Gizmos.color = Color.black;
            //Draw waypoint connections
            foreach (Waypoint.TraversalPoint traversalPoint in pathFinder.root.TraversalPoints)
            {
                DrawWayPointConnections(traversalPoint.Destination);
            }            
        }
    }


    private void DrawWayPointConnections(Waypoint waypoint)
    {        
        foreach (Waypoint.TraversalPoint nextPoint in waypoint.TraversalPoints )
        {
            if (nextPoint.TraversalType == TraversalType.Move)
            {
                DrawMoveTrjectory(waypoint, nextPoint);
            }

            else if (nextPoint.TraversalType == TraversalType.Jump)
            {
                DrawJumpTrjectory(waypoint, (Waypoint.JumpTraversalPoint)nextPoint);
            }
           
            DrawWayPointConnections(nextPoint.Destination);            
        }
    }


    private void DrawMoveTrjectory(Waypoint startPoint, Waypoint.TraversalPoint endPoint)
    {
        Gizmos.DrawLine(startPoint.transform.position, endPoint.Destination.transform.position);
    }


    private void DrawJumpTrjectory(Waypoint startPoint, Waypoint.JumpTraversalPoint jumpPoint)
    {
        float t = 0f;
        float maxTime = (jumpPoint.Destination.transform.position.x - startPoint.transform.position.x) / jumpPoint.JumpVelocity.x;       

        float prevX = startPoint.transform.position.x;
        float prevY = startPoint.transform.position.y;
        float nextX = 0;
        float nextY = 0;    

        while (t < 1) 
        {
            t += 0.1f; //Increments of 10

            float time = Mathf.Lerp(0, maxTime, t);

            nextX = startPoint.transform.position.x + jumpPoint.JumpVelocity.x * time;
            nextY = startPoint.transform.position.y + jumpPoint.JumpVelocity.y * time + (Physics.gravity.y * time * time) / 2;

            Gizmos.DrawLine(new Vector3(prevX, prevY, transform.position.z), new Vector3(nextX, nextY, transform.position.z));            

            prevX = nextX;
            prevY = nextY;
        }
    }



    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("PlayerEvent"))
        {
            if (Camera.main.TryGetComponent(out ICameraTarget cameraTarget))
            {
                cameraTarget.AddTargetFocus(transform);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("PlayerEvent"))
        {
            if (Camera.main.TryGetComponent(out ICameraTarget cameraTarget))
            {                
                cameraTarget.RemoveTargetFocus(transform);
            }
        }
    }
}
