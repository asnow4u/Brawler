using System;
using System.Collections.Generic;
using UnityEngine;

internal class WeaponHandler : MonoBehaviour
{
    [SerializeField] private Transform grabPoint;

    [SerializeField] private IWeapon equippedWeapon;
    [SerializeField] private IWeapon secondaryWeapon;

    public IWeapon EquippedWeapon => equippedWeapon;

    public event Action<IWeapon> OnWeaponEquippedEvent;


    private void Awake()
    {
        if (grabPoint == null)
            Debug.LogError("Weapon GrabPoint is Null", gameObject);
    }

    public void Initialize()
    {
        SetupStartingWeapons();        
    }

    private void SetupStartingWeapons()
    {
        IWeapon[] startingWeapons = GetComponentsInChildren<IWeapon>();

        if (startingWeapons == null || startingWeapons.Length == 0)
            return;

        if (startingWeapons.Length == 1)
            EquipWeapon(startingWeapons[0]);

        else if (startingWeapons.Length == 2)
        {
            AddWeaponToInventory(startingWeapons[1]);
            EquipWeapon(startingWeapons[0]);
        }

        else if (startingWeapons.Length > 2)
        {
            Debug.LogError("Too many starting weapons found in Weapon Inventory", gameObject);
            return;
        }
    }

    public void HandleWeaponPickup(IWeapon weapon)
    {        
        if (equippedWeapon != null && secondaryWeapon != null)
            DropEquippedWeapon();
        
        EquipWeapon(weapon);
    }
    
    private void EquipWeapon(IWeapon weapon)
    {
        if (weapon == secondaryWeapon)
            AddWeaponToInventory(equippedWeapon);
        
        equippedWeapon = weapon;
        equippedWeapon.gameObject.SetActive(true);

        Quaternion rotationOffset = grabPoint.rotation * Quaternion.Inverse(weapon.GripPoint.rotation);
        equippedWeapon.transform.rotation = rotationOffset * equippedWeapon.transform.rotation;

        Vector3 positionOffset = grabPoint.position - weapon.GripPoint.position;
        equippedWeapon.transform.position += positionOffset;

        equippedWeapon.transform.SetParent(grabPoint.transform, true);

        OnWeaponEquippedEvent?.Invoke(equippedWeapon);
    }

    private void AddWeaponToInventory(IWeapon weapon)
    {
        secondaryWeapon = weapon;
        secondaryWeapon.gameObject.SetActive(false);
        secondaryWeapon.transform.parent = transform;
    }

    private void DropEquippedWeapon()
    {
        throw new NotImplementedException();
    }
    
    public void ToggleEquippedWeapon()
    {
        if (secondaryWeapon != null)
            EquipWeapon(secondaryWeapon);
    }
}

