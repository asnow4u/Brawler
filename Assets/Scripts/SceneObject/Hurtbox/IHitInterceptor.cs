/// <summary>A component that can claim an incoming hit before the hurtbox handler applies it.</summary>
public interface IHitInterceptor
{
    /// <summary>Returns true when the hit is claimed. The response is then returned to the attacker and the hit is not applied.</summary>
    public bool TryIntercept(HitData hitData, out HitResponse response);
}
