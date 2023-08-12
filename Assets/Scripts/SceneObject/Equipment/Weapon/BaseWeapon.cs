using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseWeapon : Weapon
{
    [SerializeField] private List<GameObject> attackPointGOs = new List<GameObject>();

    protected override List<IAttackPoint> GetAttackPoints()
    {
        List<IAttackPoint> attackPoints = new List<IAttackPoint>();

        foreach (GameObject pointGo in attackPointGOs)
        {
            if (pointGo.TryGetComponent(out IAttackPoint point))
            {
                attackPoints.Add(point);
            }
        }

        return attackPoints;
    }
}
