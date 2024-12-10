using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquipmentHandler : SceneObjectHandler
{
    public Weapon CurWeapon { get; private set; }


    private WeaponCollection weaponCollection;

    public event Action<Weapon> OnWeaponEquipped;


    #region Initialize

    public override void Setup()
    {
        base.Setup();
        
        SetupWeaponsCollection();
    }

    public override void RegisterToEvents()
    { }

    public override void UnregisterToEvents()
    { }

    /// <summary>
    /// Initialize <see cref="WeaponCollection"/> if it exists
    /// </summary>
    private void SetupWeaponsCollection()
    {
        weaponCollection = GetComponentInChildren<WeaponCollection>();

        if (weaponCollection != null)
        {
            weaponCollection.Initialize();
            EquipWeaponByIndex(0);
        }
    }

    #endregion


    #region Weapon

    /// <summary>
    /// Equip <see cref="Weapon"/> from <see cref="weaponCollection"/> given <paramref name="index"/>
    /// </summary>
    public void EquipWeaponByIndex(int index)
    {
        if (weaponCollection != null && weaponCollection.TryGetWeaponByIndex(index, out Weapon weapon))
        {
            //TODO: equip weapon
            CurWeapon = weapon;
            OnWeaponEquipped?.Invoke(weapon);
        }
    }

    #endregion




}
