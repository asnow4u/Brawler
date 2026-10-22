public enum HitResult { Hit, Blocked, Ignored }

/// <summary>Outcome of a hit, returned from the hurtbox to the hitbox that delivered it.</summary>
public struct HitResponse
{
    public HitResult Result;

    //Attacker side - how long the attacker is staggered when the hit is blocked
    public float AttackerStaggerTime;

    public static HitResponse Hit => new HitResponse { Result = HitResult.Hit };
    public static HitResponse Ignored => new HitResponse { Result = HitResult.Ignored };

    public static HitResponse Blocked(float attackerStaggerTime)
    {
        return new HitResponse { Result = HitResult.Blocked, AttackerStaggerTime = attackerStaggerTime };
    }
}
