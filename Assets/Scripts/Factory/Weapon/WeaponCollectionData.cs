using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Weapon", menuName = "ScriptableObject/Weapon")]
public class WeaponCollectionData : ScriptableObject
{
    public List<AttackData> UpTiltAttacks;
    public List<AttackData> UpAirAttacks;
    public List<AttackData> DownTiltAttacks;
    public List<AttackData> DownAirAttacks;
    public List<AttackData> ForwardTiltAttacks;
    public List<AttackData> ForwardAirAttacks;
    public List<AttackData> DashAttacks;

    private int RandomNum(int max)
    {
        return Random.Range(0, max);
    }

    public AttackData GetRandomUpTiltAttack()
    {
        return UpTiltAttacks[RandomNum(UpTiltAttacks.Count)];
    }

    public AttackData GetRandomUpAirAttack()
    {
        return UpAirAttacks[RandomNum(UpAirAttacks.Count)];
    }

    public AttackData GetRandomDownTiltAttack()
    {
        return DownTiltAttacks[RandomNum(DownTiltAttacks.Count)];
    }

    public AttackData GetRandomDownAirAttack()
    {
        return DownAirAttacks[RandomNum(DownAirAttacks.Count)];
    }

    public AttackData GetRandomForwardTiltAttack()
    {
        return ForwardTiltAttacks[RandomNum(ForwardTiltAttacks.Count)];
    }

    public AttackData GetRandomForwardAirAttack()
    {
        return ForwardAirAttacks[RandomNum(ForwardAirAttacks.Count)];
    }

    public AttackData GetRandomDashAttack()
    {
        return DashAttacks[RandomNum(DashAttacks.Count)];
    }
}
