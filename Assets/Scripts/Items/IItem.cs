using System;
using System.Collections.Generic;
using UnityEngine;

public interface IItem
{
    public GameObject gameObject { get; }
    public Transform transform { get; }
    public float Mass { get; }

    public void EnableInteraction();
    public void DisableInteraction();
}


public interface IWeapon : IItem
{
    public WeaponData WeaponData { get; }
    public Transform GripPoint { get; }
    public ParticleSystem AttackEffect { get; }

    public IReadOnlyList<EnhancementData> Enhancements { get; }
    public event Action EnhancementsChangedEvent;

    public void AddEnhancement(EnhancementData enhancement);
    public void RemoveEnhancement(EnhancementData enhancement);
}