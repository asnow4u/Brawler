using Game.SceneObjects;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using UnityEngine;

public class HitStunCalculator
{
    private struct HitStunPercentData
    {
        public float Damage;
        public float Force;
        public float Percentage;

        public HitStunPercentData(float damage, float force, float percentage)
        {
            Damage = damage;
            Force = force;
            Percentage = percentage;
        }
    }

    private HashSet<HitStunPercentData> hitStunPercentSet = new HashSet<HitStunPercentData>();


    public HitStunCalculator()
    {
        hitStunPercentSet.Add(new HitStunPercentData(0, 700, 1));
        hitStunPercentSet.Add(new HitStunPercentData(10, 710, 0.9f));
        hitStunPercentSet.Add(new HitStunPercentData(20, 780, 0.8f));
        hitStunPercentSet.Add(new HitStunPercentData(30, 970, 0.9f));
        hitStunPercentSet.Add(new HitStunPercentData(40, 1340, 0.6f));
        hitStunPercentSet.Add(new HitStunPercentData(50, 1950, 0.7f));
        hitStunPercentSet.Add(new HitStunPercentData(60, 2860, 0.32f));
        hitStunPercentSet.Add(new HitStunPercentData(70, 4130, 0.2f));
        hitStunPercentSet.Add(new HitStunPercentData(80, 5820, 0.13f));
        hitStunPercentSet.Add(new HitStunPercentData(90, 7990, 0.08f));
        hitStunPercentSet.Add(new HitStunPercentData(100, 10700, 0.05f));
    }


    /// <returns>
    /// The percentage of hitstun time based on the force of the hit
    /// </returns>
    private float LookUpPercentageValueBy(float forceValue)
    {        
        HitStunPercentData lowEnd = hitStunPercentSet.First();
        HitStunPercentData highEnd = hitStunPercentSet.First();

        foreach (HitStunPercentData data in hitStunPercentSet)
        {           
            if (data.Force <= forceValue)
                lowEnd = data;
            else
            {
                highEnd = data;
                break;
            }
        }
        
        if (highEnd.Force == lowEnd.Force)
            throw new System.Exception("Force value not found in hitstun table");

        float forceDiff = highEnd.Force - lowEnd.Force;
        float percentageDiff = highEnd.Percentage - lowEnd.Percentage;
        float inbeteenPercentage = (forceValue - lowEnd.Force) / forceDiff;

        float percentageValue = lowEnd.Percentage + inbeteenPercentage * percentageDiff;
        Debug.Log("Percentage Value: " + percentageValue);

        return percentageValue;
    }


    /// <summary>
    /// Calculate the hitstun time based on the launch force and mass of the
    /// </summary>
    public float CalculateHitStunTime(Vector3 launchForce, float mass)
    {
        float apexTime = ((-launchForce.y / mass) / Physics.gravity.y);

        //NOTE: Hitstun based on percentage of time to apex
        return LookUpPercentageValueBy(launchForce.magnitude) * apexTime;
    }
}
