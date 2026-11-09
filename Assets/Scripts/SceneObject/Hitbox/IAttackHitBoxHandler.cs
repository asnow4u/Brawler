using System;
using System.Collections.Generic;
using UnityEngine;

public interface IAttackHitBoxHandler : IHitBoxHandler
{
    /// <summary>The current weapon hitboxes. Empty when no weapon is set.</summary>
    public IReadOnlyList<IHitBox> HitBoxs { get; }

    /// <summary>Raised after the weapon hitboxes are replaced.</summary>
    public event Action HitBoxsChangedEvent;

    public void SetWeaponHitBoxs(GameObject weapon);
    public void SetAttackHitData(HitData hitData, AttackHitSenderData attackHitSenderData);

    /// <summary>Stops the current attack from hitting the given scene object. Cleared when the attack ends.</summary>
    public void IgnoreForCurrentAttack(Guid sceneObjectID);
}
