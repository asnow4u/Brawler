using System;
using UnityEngine;

public interface IAttackHitBoxHandler : IHitBoxHandler
{
    public void SetWeaponHitBoxs(GameObject weapon);
    public void SetAttackHitData(HitData hitData, AttackHitSenderData attackHitSenderData);

    /// <summary>Stops the current attack from hitting the given scene object. Cleared when the attack ends.</summary>
    public void IgnoreForCurrentAttack(Guid sceneObjectID);
}
