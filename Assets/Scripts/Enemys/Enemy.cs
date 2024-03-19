using UnityEngine;

public enum EnemyState { Idle, Alert, Attack }

public abstract class Enemy : SceneObject
{
    protected EnemyState enemyState;

    protected override void Initialize()
    {
        base.Initialize();

        ObjectType = SceneObjectType.Enemy;

        SetState(EnemyState.Idle);        
    }


    private void SetState(EnemyState state)
    {
        enemyState = state;
    }


    protected override void FixedUpdate()
    {
        base.FixedUpdate();
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
