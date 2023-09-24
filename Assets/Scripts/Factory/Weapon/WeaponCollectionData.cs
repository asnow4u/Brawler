using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Weapon", menuName = "ScriptableObject/Weapon")]
public class WeaponCollectionData : ScriptableObject
{
    public List<AttackData> UpTilts;
    public List<AttackData> UpAirs;
    public List<AttackData> DownTilts;
    public List<AttackData> DownAirs;
    public List<AttackData> ForwardTilts;
    public List<AttackData> ForwardAirs;
    public List<AttackData> BackAirs;

    private int RandomNum(int max)
    {
        return Random.Range(0, max);
    }

    public AttackData GetRandomUpTilt()
    {
        return UpTilts[RandomNum(UpTilts.Count)];
    }

    public AttackData GetRandomUpAir()
    {
        return UpAirs[RandomNum(UpAirs.Count)];
    }

    public AttackData GetRandomDownTilt()
    {
        return DownTilts[RandomNum(DownTilts.Count)];
    }

    public AttackData GetRandomDownAir()
    {
        return DownAirs[RandomNum(DownAirs.Count)];
    }

    public AttackData GetRandomForwardTilt()
    {
        return ForwardTilts[RandomNum(ForwardTilts.Count)];
    }

    public AttackData GetRandomForwardAir()
    {
        return ForwardAirs[RandomNum(ForwardAirs.Count)];
    }

    public AttackData GetRandomBackAir()
    {
        return BackAirs[RandomNum(BackAirs.Count)];
    }
}
