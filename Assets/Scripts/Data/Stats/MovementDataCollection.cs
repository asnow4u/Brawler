using UnityEngine;

[CreateAssetMenu(fileName = "MovementCollection", menuName = "ScriptableObjects/SceneObject/Movement/Collection")]
public class MovementDataCollection : ScriptableObject
{
    public MoveData MoveData;
    public AirMoveData AirMoveData;
    public JumpData JumpData;
    public AirJumpData AirJumpData;
    public ClimbMoveData ClimbMoveData;
    public VaultData VaultData;
    public WallLeanData WallLeanData;
}
