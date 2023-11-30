using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IWeaponCollection
{
    public void Initialize(SceneObject sceneObj);

    public void AddWeapon(Weapon weapon);

    public void RemoveWeapon(Weapon weapon);

    public void SwapWeaponTo(int index);

    public Weapon GetCurWeapon();

    public void EnableAttackColliders(List<AttackCollider> colliderTags, Action<IDamage> OnHitEvent);

    public void DisableAttackColliders(List<AttackCollider> colliderTags, Action<IDamage> OnHitEvent);
}
