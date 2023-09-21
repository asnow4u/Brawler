using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Sword", menuName = "ScriptableObject/Sword")]
public class SwordData : ScriptableObject
{
    [SerializeField] private List<AttackData> upTilts;
    [SerializeField] private List<AttackData> upAir;
    [SerializeField] private List<AttackData> downTilts;
    [SerializeField] private List<AttackData> downAir;
    [SerializeField] private List<AttackData> forwardTilts;
    [SerializeField] private List<AttackData> forwardAir;
    [SerializeField] private List<AttackData> backAir;
}
