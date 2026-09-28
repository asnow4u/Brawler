using System;
using System.Collections.Generic;
using UnityEngine;

internal class Weapon : Item, IWeapon
{    
    [SerializeField] private WeaponData weaponData;
    public WeaponData WeaponData => weaponData;

    [SerializeField] private Transform gripPoint;
    public Transform GripPoint => gripPoint;

    [SerializeField] private ParticleSystem attackEffect;
    public ParticleSystem AttackEffect => attackEffect;

    [Header("Enhancements")]
    [SerializeField] private List<EnhancementData> enhancements = new List<EnhancementData>();
    public IReadOnlyList<EnhancementData> Enhancements => enhancements;

    public event Action EnhancementsChangedEvent;

    protected override void Awake()
    {
        if (weaponData == null)
            Debug.LogError("Weapon Data not set for " + name, this);

        if (gripPoint == null)
            Debug.LogError("Weapon Grip Point not set for " + name, this);
    }


    #region Enhancements

    public void AddEnhancement(EnhancementData enhancement)
    {
        if (enhancement == null)
            return;

        enhancements.Add(enhancement);
        EnhancementsChangedEvent?.Invoke();
    }

    public void RemoveEnhancement(EnhancementData enhancement)
    {
        if (enhancements.Remove(enhancement))
            EnhancementsChangedEvent?.Invoke();
    }

    #if UNITY_EDITOR

        private void OnValidate()
        {
            if (Application.isPlaying)
                EnhancementsChangedEvent?.Invoke();
        }

    #endif

    #endregion
}

