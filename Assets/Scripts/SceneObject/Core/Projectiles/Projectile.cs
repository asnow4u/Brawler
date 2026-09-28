using System;
using UnityEngine;

/// <summary>
/// Base for spawned objects that fly and hit. Holds its own hit stats. Flies at the velocity it is
/// launched with, pulled down by the given gravity, and is removed after its lifetime, on contact
/// with the environment, or when it hits something.
/// </summary>
[RequireComponent(typeof(ProjectileHitBoxHandler))]
[RequireComponent(typeof(ProjectileHurtBoxHandler))]
public class Projectile : SceneObject
{
    private ProjectileHitBoxHandler hitBoxHandler;

    [Header("Hit")]
    [SerializeField] private float baseForce = 750f;
    [Range(0, 1)]
    [SerializeField] private float influence = 0.2f;
    [SerializeField] private float damage = 6f;
    [SerializeField] private float stunTime = 0.15f;
    [SerializeField] private int effectIndex = 0;

    private float gravity;
    private float lifetime;
    private float spawnTime;
    private bool launched;


    protected override void Awake()
    {
        base.Awake();

        hitBoxHandler = GetComponent<ProjectileHitBoxHandler>();
    }


    #region Launch

    /// <summary>Launches along a trajectory at an angle in degrees, 0 being right and 90 up.</summary>
    public void Launch(Guid ownerID, float launchAngle, TrajectoryData trajectory)
    {
        float radians = launchAngle * Mathf.Deg2Rad;
        Vector3 velocity = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f) * trajectory.Speed;

        float trajectoryGravity = trajectory is LobTrajectoryData lob ? lob.Gravity : 0f;

        Launch(ownerID, velocity, trajectoryGravity, trajectory.Lifetime);
    }

    /// <summary>Launches at a velocity. Hits ignore the owner.</summary>
    public void Launch(Guid ownerID, Vector3 velocity, float gravity, float lifetime)
    {
        this.gravity = gravity;
        this.lifetime = lifetime;
        spawnTime = Time.time;
        launched = true;

        rb.useGravity = false;
        rb.linearVelocity = velocity;

        hitBoxHandler.Initialize(ownerID, new HitData(baseForce, influence, 0f, damage, stunTime, transform.position, effectIndex));
    }

    #endregion


    #region Flight

    private void FixedUpdate()
    {
        if (gravity > 0f)
            rb.linearVelocity += Vector3.down * gravity * Time.fixedDeltaTime;
    }

    private void Update()
    {
        if (launched && Time.time - spawnTime >= lifetime)
            Destroy(gameObject);
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Environment"))
            Destroy(gameObject);
    }

    #endregion
}
