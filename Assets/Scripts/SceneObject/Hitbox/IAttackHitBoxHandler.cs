using UnityEngine;

public interface IAttackHitBoxHandler : IHitBoxHandler
{
    public void SetWeaponHitBoxs(GameObject weapon);
    public void SetAttackHitData(HitData hitData, AttackHitSenderData attackHitSenderData);
}
