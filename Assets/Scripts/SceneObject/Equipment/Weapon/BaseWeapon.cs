using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseWeapon : Weapon
{
    [SerializeField] private List<GameObject> attackPoints = new List<GameObject>();

    protected override List<IAttackPoint> GetAttackPoints()
    {
        List<IAttackPoint> attackPoints = new List<IAttackPoint>();

        foreach (GameObject pointGo in this.attackPoints)
        {
            if (pointGo.TryGetComponent(out IAttackPoint point))
            {
                attackPoints.Add(point);
            }
        }

        return attackPoints;
    }
}
