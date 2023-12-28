using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAttackPoint
{
    public AttackColliderType GetColliderType();
    public void RegisterToHitEvent(Action<IDamage> callback);
    public void UnRegisterToHitEvent(Action<IDamage> callback);
    public void EnableColliders();
    public void DisableColliders();
}
