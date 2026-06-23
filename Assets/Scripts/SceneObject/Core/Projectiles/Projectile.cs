using System;
using UnityEngine;

[RequireComponent(typeof(ProjectileHitBoxHandler))]
[RequireComponent(typeof(ProjectileHurtBoxHandler))]
public class Projectile : SceneObject
{
    private Rigidbody rb;
    private ProjectileHitBoxHandler hitBoxHandler;

    [SerializeField] private float lifetime = 5f;
    private float spawnTime;

    protected override void Awake()
    {
        base.Awake();
        rb = GetComponent<Rigidbody>();
        hitBoxHandler = GetComponent<ProjectileHitBoxHandler>();
    }

    public void Initialize(Vector3 velocity, HitData hitData)
    {
        spawnTime = Time.time;

        rb.linearVelocity = velocity;
        rb.useGravity = false;

        hitBoxHandler.SetHitData(hitData);
    }

    private void Update()
    {
        if (Time.time - spawnTime >= lifetime)
            Destroy(gameObject);
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Environment"))
            Destroy(gameObject);
    }
}
