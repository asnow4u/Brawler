using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AttackCollection))]
[RequireComponent(typeof(MovementCollection))]
public class Weapon : MonoBehaviour
{
    public enum Type { Base, Sword, Spear, GreatSword, Hammer };
    public Type weaponType;

    public MovementCollection MovementCollection { get; protected set; }
    public AttackCollection AttackCollection { get; protected set; }   
    public List<IAttackPoint> AttackPoints { get; protected set; }


    private void Start()
    {
        AttackPoints = GetAttackPoints();
        AttackCollection = GetComponent<AttackCollection>();
        MovementCollection = GetComponent<MovementCollection>();
    }


    protected virtual List<IAttackPoint> GetAttackPoints()
    {
        return new List<IAttackPoint>(GetComponentsInChildren<IAttackPoint>());
    }
}

