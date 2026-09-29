using System;
using UnityEngine;

/// <summary>
/// Base for spawned objects that fly and hit. Holds its own hit stats. Flies at the velocity it is
/// launched with, pulled down by the given gravity, and is removed after its lifetime, on contact
/// with the environment, or when it hits something.
///
/// When launched along a trajectory, aims at a target in range if hitting it needs no more than the
/// trajectory's maximum correction. When hit, flies back at whoever it came from if they are in
/// range on the side it was hit toward; otherwise flies its trajectory's forward angle toward that
/// side.
/// </summary>
[RequireComponent(typeof(ProjectileHitBoxHandler))]
[RequireComponent(typeof(ProjectileHurtBoxHandler))]
public class Projectile : SceneObject
{
    private ProjectileHitBoxHandler hitBoxHandler;
    private IHurtBoxHandler hurtBoxHandler;

    [Header("Hit")]
    [SerializeField] private float baseForce = 750f;
    [Range(0, 1)]
    [SerializeField] private float influence = 0.2f;
    [SerializeField] private float damage = 6f;
    [SerializeField] private float stunTime = 0.15f;
    [SerializeField] private int effectIndex = 0;

    [Header("Deflection")]
    [Tooltip("Slowest speed after being hit, in world units per second.")]
    [SerializeField] private float minDeflectSpeed = 8f;
    [Tooltip("Fastest speed after being hit, in world units per second.")]
    [SerializeField] private float maxDeflectSpeed = 30f;

    private TrajectoryData trajectory;
    private Guid ownerID;

    private float gravity;
    private float lifetime;
    private float spawnTime;
    private bool launched;


    protected override void Awake()
    {
        base.Awake();

        hitBoxHandler = GetComponent<ProjectileHitBoxHandler>();
        hurtBoxHandler = GetComponent<IHurtBoxHandler>();

        hurtBoxHandler.OnHitEvent += OnHitTaken;
    }

    protected override void OnDestroy()
    {
        hurtBoxHandler.OnHitEvent -= OnHitTaken;

        base.OnDestroy();
    }


    #region Launch

    /// <summary>
    /// Launches along a trajectory at an angle in degrees, 0 being right and 90 up, adjusted toward a
    /// target within the trajectory's assist settings.
    /// </summary>
    public void Launch(Guid ownerID, float launchAngle, TrajectoryData trajectory)
    {
        this.trajectory = trajectory;

        float trajectoryGravity = trajectory is LobTrajectoryData lob ? lob.Gravity : 0f;
        float angle = AssistedLaunchAngle(launchAngle, trajectory, trajectoryGravity, ownerID);

        Launch(ownerID, AngleToDirection(angle) * trajectory.Speed, trajectoryGravity, trajectory.Lifetime);
    }

    /// <summary>Launches at a velocity. Hits ignore the owner.</summary>
    public void Launch(Guid ownerID, Vector3 velocity, float gravity, float lifetime)
    {
        this.ownerID = ownerID;
        this.gravity = gravity;
        this.lifetime = lifetime;
        spawnTime = Time.time;
        launched = true;

        rb.useGravity = false;
        rb.linearVelocity = velocity;

        hitBoxHandler.Initialize(ownerID, new HitData(baseForce, influence, 0f, damage, stunTime, transform.position, effectIndex));
    }

    /// <summary>
    /// The angle that hits the target needing the smallest correction, when that correction is within
    /// the trajectory's maximum. Otherwise the intended angle.
    /// </summary>
    private float AssistedLaunchAngle(float intendedAngle, TrajectoryData trajectory, float gravity, Guid ownerID)
    {
        float bestAngle = intendedAngle;
        float bestCorrection = trajectory.AssistMaxCorrection;

        foreach (SceneObject candidate in FindObjectsByType<SceneObject>(FindObjectsSortMode.None))
        {
            if (!IsAssistCandidate(candidate, ownerID, trajectory.AssistRange))
                continue;

            if (!TrySolveAngles(candidate.Bounds.center, trajectory.Speed, gravity, out float lowAngle, out float highAngle))
                continue;

            float angle = ClosestAngle(intendedAngle, lowAngle, highAngle);
            float correction = Mathf.Abs(Mathf.DeltaAngle(intendedAngle, angle));

            if (correction > bestCorrection)
                continue;

            bestCorrection = correction;
            bestAngle = angle;
        }

        return bestAngle;
    }

    /// <summary>Not this projectile, its owner, or another projectile, and within range.</summary>
    private bool IsAssistCandidate(SceneObject candidate, Guid ownerID, float range)
    {
        if (candidate == this || candidate.UniqueID == ownerID || candidate is Projectile)
            return false;

        return Vector3.Distance(candidate.Bounds.center, transform.position) <= range;
    }

    #endregion


    #region Deflection

    /// <summary>
    /// Flies at a speed from the knockback, clamped to the deflect range, in the deflect direction.
    /// Restarts its lifetime and takes the attacker as its owner.
    /// </summary>
    private void OnHitTaken(KnockBackHitData knockBackHitData)
    {
        Vector3 knockBack = knockBackHitData.KnockBackVelocity;
        float speed = Mathf.Clamp(knockBack.magnitude, minDeflectSpeed, maxDeflectSpeed);

        Guid[] hitBy = hurtBoxHandler.LastHitBy;
        Guid batterID = hitBy.Length > 0 ? hitBy[hitBy.Length - 1] : Guid.Empty;

        rb.linearVelocity = DeflectDirection(knockBack, speed, batterID) * speed;
        spawnTime = Time.time;

        if (batterID == Guid.Empty)
            return;

        ownerID = batterID;
        hitBoxHandler.TransferOwnership(ownerID);
    }

    /// <summary>
    /// Toward the previous owner when valid, otherwise the trajectory's forward angle toward the hit
    /// side, otherwise the knockback direction.
    /// </summary>
    private Vector3 DeflectDirection(Vector3 knockBack, float speed, Guid batterID)
    {
        float hitSide = Mathf.Abs(knockBack.x) > 0.01f ? Mathf.Sign(knockBack.x) : 0f;

        if (ownerID != batterID && TryFindSceneObject(ownerID, out SceneObject sender) && IsValidReturnTarget(sender, hitSide))
            return AimAt(sender.Bounds.center, speed);

        if (trajectory != null && hitSide != 0f)
            return AngleToDirection(MirrorToSide(trajectory.ForwardTiltAngle, hitSide));

        if (knockBack.sqrMagnitude > 0f)
            return knockBack.normalized;

        return -rb.linearVelocity.normalized;
    }

    /// <summary>Within the trajectory's assist range and on the side the projectile was hit toward.</summary>
    private bool IsValidReturnTarget(SceneObject target, float hitSide)
    {
        if (trajectory == null)
            return false;

        Vector3 toTarget = target.Bounds.center - transform.position;

        if (toTarget.magnitude > trajectory.AssistRange)
            return false;

        return hitSide == 0f || Mathf.Sign(toTarget.x) == hitSide;
    }

    /// <summary>Direction of the low arc that reaches the target, or 45 degrees toward it when out of reach.</summary>
    private Vector3 AimAt(Vector3 target, float speed)
    {
        if (TrySolveAngles(target, speed, gravity, out float lowAngle, out _))
            return AngleToDirection(lowAngle);

        float side = target.x >= transform.position.x ? 1f : -1f;

        return AngleToDirection(MirrorToSide(45f, side));
    }

    #endregion


    #region Aim Math

    /// <summary>
    /// Launch angles in degrees that reach a target at a speed under a gravity. The same angle twice
    /// when there is no gravity. False when the target is out of reach.
    /// </summary>
    private bool TrySolveAngles(Vector3 target, float speed, float gravity, out float lowAngle, out float highAngle)
    {
        Vector3 toTarget = target - transform.position;
        float dx = Mathf.Abs(toTarget.x);
        float side = toTarget.x >= 0f ? 1f : -1f;

        if (gravity <= 0f || dx < 0.01f)
        {
            lowAngle = highAngle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
            return true;
        }

        float speedSquared = speed * speed;
        float discriminant = speedSquared * speedSquared - gravity * (gravity * dx * dx + 2f * toTarget.y * speedSquared);

        if (discriminant < 0f)
        {
            lowAngle = highAngle = 0f;
            return false;
        }

        float root = Mathf.Sqrt(discriminant);
        float low = Mathf.Atan((speedSquared - root) / (gravity * dx)) * Mathf.Rad2Deg;
        float high = Mathf.Atan((speedSquared + root) / (gravity * dx)) * Mathf.Rad2Deg;

        lowAngle = MirrorToSide(low, side);
        highAngle = MirrorToSide(high, side);
        return true;
    }

    private static float ClosestAngle(float intendedAngle, float a, float b)
    {
        return Mathf.Abs(Mathf.DeltaAngle(intendedAngle, a)) <= Mathf.Abs(Mathf.DeltaAngle(intendedAngle, b)) ? a : b;
    }

    /// <summary>An angle authored facing right, mirrored when the side is negative.</summary>
    private static float MirrorToSide(float angle, float side)
    {
        return side >= 0f ? angle : 180f - angle;
    }

    /// <summary>Unit direction for an angle in degrees, 0 being right and 90 up.</summary>
    private static Vector3 AngleToDirection(float angle)
    {
        float radians = angle * Mathf.Deg2Rad;

        return new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f);
    }

    private static bool TryFindSceneObject(Guid id, out SceneObject sceneObject)
    {
        sceneObject = null;

        if (id == Guid.Empty)
            return false;

        foreach (SceneObject candidate in FindObjectsByType<SceneObject>(FindObjectsSortMode.None))
        {
            if (candidate.UniqueID != id)
                continue;

            sceneObject = candidate;
            return true;
        }

        return false;
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
