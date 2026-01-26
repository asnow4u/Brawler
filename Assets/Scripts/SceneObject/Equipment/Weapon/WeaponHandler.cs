using System;
using System.Collections.Generic;
using UnityEngine;


public enum WeaponType { Sword }

public class WeaponHandler : MonoBehaviour
{
    [SerializeField] private Transform grabPoint;

    [SerializeField] private Weapon equippedWeapon;
    [SerializeField] private Weapon secondaryWeapon;

    public Weapon EquippedWeapon => equippedWeapon;

    /// <summary>
    /// Event when weapon is equipped, providing previous and new weapon.
    /// </summary>
    public event Action<Weapon, Weapon> OnWeaponEquippedEvent;

    public void Setup()
    {        
        //All Starting Weapons
        foreach (Weapon weapon in gameObject.GetComponentsInChildren<Weapon>(true))
            AddWeapon(weapon);
    }

    /// <summary>
    /// Add <paramref name="weapon"/> to collection
    /// </summary>
    public void AddWeapon(Weapon weapon)
    {
        weapon.DisableInteraction();       

        if (equippedWeapon == null)
            EquipWeapon(weapon);

        else if (secondaryWeapon == null)
            AddWeaponToInventory(weapon);

        else
            EquipWeapon(weapon);
    }

    /// <summary>
    /// Switch which weapon is currently equipped.
    /// </summary>
    public void ToggleEquippedWeapon()
    {
        if (secondaryWeapon != null)
            EquipWeapon(secondaryWeapon);
    }

    /// <summary>
    /// Equip <paramref name="weapon"/> and remove current weapon if necessary.
    /// </summary>
    private void EquipWeapon(Weapon weapon)
    {
        if (weapon == secondaryWeapon)
            AddWeaponToInventory(equippedWeapon);

        else if (equippedWeapon != null && secondaryWeapon != null && equippedWeapon != weapon && secondaryWeapon != weapon)
            RemoveWeapon(equippedWeapon);
        
        Weapon previousWeapon = equippedWeapon;
        equippedWeapon = weapon;
        equippedWeapon.gameObject.SetActive(true);

        equippedWeapon.transform.parent = grabPoint.transform;
        equippedWeapon.transform.localPosition = Vector3.zero;
        equippedWeapon.transform.localRotation = Quaternion.identity;

        OnWeaponEquippedEvent?.Invoke(previousWeapon, equippedWeapon);
    }
    
    /// <summary>
    /// Adds <paramref name="weapon"/> to the inventory.
    /// </summary>
    private void AddWeaponToInventory(Weapon weapon)
    {
        secondaryWeapon = weapon;
        secondaryWeapon.gameObject.SetActive(false);
        secondaryWeapon.transform.parent = transform;
    }

    /// <summary>
    /// Remove <paramref name="weapon"/> from collection
    /// </summary>
    private void RemoveWeapon(Weapon weapon)
    {
        if (equippedWeapon == weapon)
            equippedWeapon = null;

        if (secondaryWeapon == weapon)
            secondaryWeapon = null;

        weapon.EnableInteraction();
        weapon.gameObject.SetActive(true);
        weapon.transform.parent = null;
    }
}

