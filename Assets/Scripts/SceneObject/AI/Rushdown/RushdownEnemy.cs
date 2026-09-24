using UnityEngine;

/// <summary>
/// Rushes the player once engaged, stops in range, and swings on a cooldown. Attacks only while
/// grounded, choosing the slot from the player's position relative to the enemy.
/// </summary>
internal class RushdownEnemy : Enemy
{
    [Header("Melee")]
    [Tooltip("Distance at which the enemy stops and attacks, in world units.")]
    [SerializeField] private float attackRange = 1.75f;
    [Tooltip("Height difference above which an up or down attack is used instead of a forward one, in world units.")]
    [SerializeField] private float verticalSlotThreshold = 1f;
    [SerializeField] private Cooldown attackCooldown = new Cooldown(1.2f, 0.5f);


    protected override void Start()
    {
        base.Start();
        attackCooldown.Stagger();
    }


    #region Behavior

    protected override void OnEngaged(Vector3 playerCenter)
    {
        if (TryMeleeAttack(playerCenter))
        {
            StopMoving();
            return;
        }

        MoveTo(LastKnownPlayerPosition);
    }

    /// <summary>Attacks when in range and ready. True while the enemy should hold position.</summary>
    private bool TryMeleeAttack(Vector3 playerCenter)
    {
        if (IsAttacking)
            return true;

        if (!IsGrounded || !IsWithinOfSelf(playerCenter, attackRange))
            return false;

        if (!attackCooldown.IsReady)
            return true;

        PressAttack(AttackDirectionTo(playerCenter, verticalSlotThreshold));
        attackCooldown.Trigger();

        return true;
    }

    #endregion


    #region Gizmos

    protected override void DrawArchetypeGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(Bounds.center, attackRange);
    }

    #endregion
}
