using System;
using System.Collections.Generic;
using UnityEngine;


public enum WeaponType { Sword }

public class WeaponCollection : IDisposable
{
    [SerializeField] private List<Weapon> weapons = new List<Weapon>();
    private Dictionary<WeaponType, WeaponGrabPoint> grabPoints = new Dictionary<WeaponType, WeaponGrabPoint>();

    public WeaponCollection(GameObject go)
    {
        //GrabPoints
        foreach (WeaponGrabPoint grabPoint in go.GetComponentsInChildren<WeaponGrabPoint>())
            AddWeaponGrabPoint(grabPoint);

        //Weapons
        foreach (Weapon weapon in go.GetComponentsInChildren<Weapon>())
            AddWeapon(weapon);
    }


    #region Getter

    /// <summary>
    /// Get <paramref name="weapon"/> from collection by <paramref name="index"/>
    /// </summary>
    public bool TryGetWeaponByIndex(int index, out Weapon weapon)
    {
        weapon = null;

        if (weapons.Count > index)
            weapon = weapons[index];

        return weapon != null;
    }


    /// <summary>
    /// Get <paramref name="grabPoint"/> from collection by <paramref name="type"/>
    /// </summary>
    public bool TryGetGrabPointByWeaponType(WeaponType type, out WeaponGrabPoint grabPoint)
    {
        grabPoint = null;
        
        if (grabPoints.ContainsKey(type))
            grabPoint = grabPoints[type];

        return grabPoint != null;    
    }

    #endregion


    #region Collection

    /// <summary>
    /// Add <paramref name="grabPoint"/> to <see cref="grabPoints"/>
    /// </summary>
    private void AddWeaponGrabPoint(WeaponGrabPoint grabPoint)
    {
        if (!grabPoints.ContainsKey(grabPoint.WeaponType))
            grabPoints.Add(grabPoint.WeaponType, grabPoint);
        else
            Debug.LogWarning("Multiple WeaponGrabPoints for " +  grabPoint.WeaponType + " found");
    }    


    /// <summary>
    /// Add <paramref name="weapon"/> to collection
    /// </summary>
    public void AddWeapon(Weapon weapon)
    {
        try
        {
            if (!weapons.Contains(weapon))
            {
                weapons.Add(weapon);
                SetWeaponToInventory(weapon);
            }
        }

        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }


    /// <summary>
    /// Remove <paramref name="weapon"/> from collection
    /// </summary>
    public void RemoveWeapon(Weapon weapon)
    {
        if (weapons.Contains(weapon))
            weapons.Remove(weapon);
    }

    #endregion


    #region Inventory


    /// <summary>
    /// Set <paramref name="weapon"/> to inventory
    /// </summary>
    private void SetWeaponToInventory(Weapon weapon)
    {        
        SetGrabPointTo(weapon);
        weapon.gameObject.SetActive(false);
    }


    /// <summary>
    /// Set <paramref name="weapon"/> to <see cref="WeaponGrabPoint"/> by <see cref="Weapon.Type"/>
    /// </summary>
    private void SetGrabPointTo(Weapon weapon)
    {
        if (grabPoints.ContainsKey(weapon.Type))
        {
            WeaponGrabPoint grabPoint = grabPoints[weapon.Type];
            weapon.transform.parent = grabPoint.transform;
            weapon.transform.localPosition = Vector3.zero;
            weapon.transform.localRotation = Quaternion.identity;
        }

        else
            throw new NullReferenceException("Weapon grabpoint not found");
    }

    #endregion


    public void Dispose()
    {
        grabPoints.Clear();
        weapons.Clear();
    }
}

