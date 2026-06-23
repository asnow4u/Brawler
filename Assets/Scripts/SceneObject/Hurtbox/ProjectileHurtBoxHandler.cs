using System;
using UnityEngine;

public class ProjectileHurtBoxHandler : HurtBoxHandler
{
    protected override void OnHit(HitData hitData)
    {
        if (hitData == null)
            return;

        Vector3 vel = rb.linearVelocity;
        rb.linearVelocity = new Vector3(-vel.x, vel.y, vel.z);
    }
}
