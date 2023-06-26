using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IEquipment
{
    public void AddWeapon(Weapon weapon);

    public void RemoveWeapon(Weapon weapon);

    public void SwapWeapon(int index);
}
