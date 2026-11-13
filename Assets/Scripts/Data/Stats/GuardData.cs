using UnityEngine;

[CreateAssetMenu(fileName = "GuardData", menuName = "ScriptableObjects/SceneObject/Guard")]
public class GuardData : ScriptableObject
{
    [Header("Animation")]
    public AnimationData GuardAnimation;
    public AnimationData CounterAnimation;

    [Header("Timing")]
    [Tooltip("Seconds from raising guard until hits are blocked.")]
    public float RaiseTime = 0.06f;
    [Tooltip("Seconds the guard stays up while held. When it runs out the object returns to idle.")]
    public float GuardDuration = 1.5f;
    [Tooltip("Seconds after the guard ends before it can be raised again.")]
    public float Cooldown = 5f;
    [Tooltip("Seconds from releasing guard until the object can act. Jump and attack skip it.")]
    public float DropTime = 0.05f;
    [Tooltip("Seconds the guard is locked after blocking a hit.")]
    public float BlockstunTime = 0.15f;

    [Header("Ground Movement")]
    [Tooltip("Max horizontal speed while walking with guard up.")]
    public float GuardWalkSpeed = 2f;

    [Header("Air Movement")]
    [Tooltip("Max horizontal speed while drifting with guard up in the air.")]
    public float AirDriftSpeed = 2f;
    [Tooltip("Multiplier on gravity while guarding in the air.")]
    public float AirGravityScale = 0.35f;
    [Tooltip("Fall speed the guard slows toward in the air.")]
    public float AirMaxFallSpeed = 3f;
    [Tooltip("Velocity removed per second while above the air drift speed, above the air fall speed, or rising.")]
    public float AirSlowdownRate = 15f;

    [Header("Pushback")]
    [Tooltip("Speed pushing the guarding object along the blocked attack's launch angle, before damage is added. Horizontal only on the ground.")]
    public float PushbackBase = 2f;
    [Tooltip("Pushback speed added per point of blocked damage.")]
    public float PushbackPerDamage = 0.2f;

    [Header("Attacker")]
    [Tooltip("Seconds of stagger applied to the attacker per point of blocked damage.")]
    public float AttackerStaggerPerDamage = 0.03f;

    [Header("Counter")]
    [Tooltip("Seconds after the latest blocked hit in which releasing guard performs the counter.")]
    public float CounterWindow = 0.2f;
    [Tooltip("Seconds the counter lasts. The object is held in place and cannot be hit during the counter.")]
    public float CounterDuration = 0.25f;

    [Header("Counter Blast")]
    [Tooltip("Radius the blast grows to.")]
    public float BlastRadius = 2.5f;
    [Tooltip("Seconds for the blast to grow to full radius. The blast hits while growing.")]
    public float BlastExpandTime = 0.12f;
    public float CounterDamage = 3f;
    [Tooltip("Knockback at 0 damage.")]
    public float CounterBaseForce = 1500f;
    [Range(0, 1)]
    public float CounterInfluence = 0.2f;
    [Tooltip("Launch angle for targets on the right. Mirrored for targets on the left.")]
    public float CounterLaunchAngle = 30f;
    public float CounterHitStunTime = 0.2f;
    [Tooltip("Seconds the guarding object's animation freezes when the blast connects.")]
    public float CounterHitPauseTime = 0.06f;
}
