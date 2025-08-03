using Game.SceneObjects;
using UnityEngine;

public class Damager : MonoBehaviour
{
    [SerializeField] private float launchAngle = 45f;
    [SerializeField] private float damage = 5;

    private DamageCollider damageCollider;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        damageCollider = GetComponent<DamageCollider>();
        damageCollider.Enable(HandleCollision);
    }


    private void HandleCollision(ITakeDamage hitTarget, Collider col)
    {        
        hitTarget.HitByAttack(1, col.ClosestPoint(col.transform.position), damage, launchAngle);
    }
}
