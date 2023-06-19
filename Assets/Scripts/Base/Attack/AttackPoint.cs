using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackPoint : MonoBehaviour
{
    [SerializeField] private string AnimationName;
    private List<Collider> colliders;       
    [SerializeField] private float damageOutput;
    [SerializeField] private float knockback;
    [SerializeField] private Vector2 forceDirection;

    private bool isRight;

    public void Start()
    {
        colliders = new List<Collider>(GetComponents<Collider>());

        DisableColliders();
    }


    public void EnableColliders(bool isFacingRightDirection)
    {
        foreach (Collider col in colliders)
        {
            isRight = isFacingRightDirection;
            col.enabled = true;
        }       
    }


    public void DisableColliders()
    {
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }
    }

    private void HitTarget(SceneObject target)
    {
        target.AddDamage(damageOutput);

        float xDirection = forceDirection.x * (isRight ? 1 : -1);

        target.ApplyForce(knockback, new Vector2(xDirection, forceDirection.y));
    }


    private void OnTriggerEnter(Collider col)
    {
        SceneObject target = col.GetComponent<SceneObject>();

        if (target != null)
        {
            HitTarget(target);
        }
    }
}
