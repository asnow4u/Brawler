using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IWeaponCollection
{
    public void Initialize(SceneObject sceneObj);

    public Weapon GetCurWeapon();

    public void AddWeapon(Weapon weapon);

    public void RemoveWeapon(Weapon weapon);

    public void SwapWeaponTo(int index);


    public event Action<Weapon> WeaponChangedEvent;
}
