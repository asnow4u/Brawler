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
    [Tooltip("Seconds from dropping guard until the object can act.")]
    public float DropTime = 0.1f;
    [Tooltip("Seconds the guard is locked after blocking a hit.")]
    public float BlockstunTime = 0.15f;

    [Header("Stagger Bar")]
    [Tooltip("Blocked damage needed to break the guard.")]
    public float StaggerBarMax = 50f;
    [Tooltip("Bar drained per second while not guarding.")]
    public float BarDecayRate = 10f;
    [Tooltip("Bar filled per second while the guard is up.")]
    public float HoldFillRate = 3f;
    [Tooltip("Bar drained per second during a guard break. The break lasts until the bar is empty.")]
    public float BreakDrainRate = 50f;

    [Header("Movement")]
    [Tooltip("Max horizontal speed while walking with guard up.")]
    public float GuardWalkSpeed = 2f;
    [Tooltip("Velocity pushing the guarding object away from a blocked hit, before damage is added.")]
    public float PushbackBase = 2f;
    [Tooltip("Pushback velocity added per point of blocked damage.")]
    public float PushbackPerDamage = 0.2f;

    [Header("Attacker")]
    [Tooltip("Seconds of stagger applied to the attacker per point of blocked damage.")]
    public float AttackerStaggerPerDamage = 0.03f;

    [Header("Counter")]
    [Tooltip("Seconds after the latest blocked hit in which releasing guard performs the counter.")]
    public float CounterWindow = 0.2f;
    [Tooltip("Seconds the counter lasts. The object cannot be hit during the counter.")]
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
