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
}