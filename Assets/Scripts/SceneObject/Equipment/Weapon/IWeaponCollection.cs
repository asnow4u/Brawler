using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IWeaponCollection
{    
    public void AddWeapon(Weapon weapon);

    public void RemoveWeapon(Weapon weapon);

    public void SwapWeaponTo(int index);

    public Weapon GetCurWeapon();

    public void EnableAttackColliders(List<string> colliderTags, Action<IDamage> OnHitEvent);

    public void DisableAttackColliders(List<string> colliderTags, Action<IDamage> OnHitEvent);
}
