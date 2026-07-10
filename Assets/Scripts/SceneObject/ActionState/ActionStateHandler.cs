using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(ISceneObject))]
public class ActionStateHandler : MonoBehaviour, IActionState
{
    private ISceneObject sceneObject;

    [SerializeField] private GroundedState curGroundedState;
    public GroundedState CurGroundedState => curGroundedState;

    [SerializeField] private ActionState curActionState;
    public ActionState CurActionState => curActionState;

    [SerializeField] private IdleState curIdleState;
    public IdleState CurIdleState => curIdleState;

    public event Action<GroundedState> GroundedStateChangedEvent;

    public event Action<ActionState> ActionStateChangedEvent;
    public event Action<IdleState> IdleStateChangedEvent;
    

    #region Initialize

    private void Awake()
    {
        sceneObject = GetComponent<ISceneObject>();
        if (sceneObject == null)
            Debug.LogError("ActionStateHandler requires a component that implements ISceneObject.");
    }

    private void Start()
    {
        ChangeState(ActionState.Idle);
    }

    private void FixedUpdate()
    {
        UpdateGroundedState();
    }

    private void UpdateGroundedState()
    {
        GroundedState groundedState = curGroundedState;
        
        bool surfaceCollision = sceneObject.TryDetectCollision(Direction.Down, 0.01f, LayerMask.GetMask("Environment"), out _);
        
        // Check standing on an object sceneObject
        if (!surfaceCollision && sceneObject.TryDetectCollision(Direction.Down, 0.01f, LayerMask.GetMask("SceneObject"), out Collider hitCollider))
        {
            if (hitCollider.TryGetComponent(out ISceneObject hitSceneObject))
            {
                if (hitSceneObject.ObjectType == SceneObjectType.Object)
                    surfaceCollision = true;
            }
        }
        groundedState = surfaceCollision ? GroundedState.Grounded : GroundedState.Airborn;

        if (groundedState != curGroundedState)
        {
            curGroundedState = groundedState;
            sceneObject.Log("Grounded State: " + curGroundedState);
            GroundedStateChangedEvent?.Invoke(curGroundedState);
            
            UpdateIdleState();
        }
    }

    #endregion


    #region States

    public void ChangeState(ActionState newState)
    {
        if (newState != curActionState)
        {
            curActionState = newState;

            sceneObject.Log("ActionState State: " + curActionState);
            ActionStateChangedEvent?.Invoke(curActionState);
        }
    }

    public bool TryChangeState(ActionState newState)
    {
        if (newState > curActionState)
        {
            ChangeState(newState);
            return true;
        }

        else if (newState == curActionState)
            return true;

        return false;
    }

    private void UpdateIdleState()
    {        
        if (curGroundedState == GroundedState.Grounded)
            curIdleState = IdleState.GroundIdle;
        else
            curIdleState = IdleState.AirIdle;

        sceneObject.Log("Idle State: " + curIdleState);
        IdleStateChangedEvent?.Invoke(curIdleState);
    }

    #endregion    
}