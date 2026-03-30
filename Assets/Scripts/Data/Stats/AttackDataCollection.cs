using UnityEngine;

[CreateAssetMenu(fileName = "AttackCollection", menuName = "ScriptableObjects/SceneObject/Attack/Collection")]
public class AttackDataCollection : ScriptableObject
{
    public AttackData UpTiltData;
    public AttackData UpAirData;
    public AttackData DownTiltData;
    public AttackData DownAirData;
    public AttackData ForwardTiltData;
    public AttackData ForwardAirData;
}
