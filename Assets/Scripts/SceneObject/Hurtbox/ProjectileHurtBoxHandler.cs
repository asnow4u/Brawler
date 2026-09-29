using System;
using UnityEngine;

public class ProjectileHurtBoxHandler : HurtBoxHandler
{
    /// <summary>Records the attacker as the only entry in LastHitBy, resets damage taken, then applies knockback from this hit alone.</summary>
    protected override void OnHit(HitData hitData)
    {
        if (hitData == null)
            return;

        if (hitData is SceneObjectHitData sceneObjectHitData)
        {
            lastHitBy.Clear();
            lastHitBy.Add(sceneObjectHitData.SceneObjectAttackerID);
        }

        damageTaken = 0f;

        base.OnHit(hitData);
    }
}
