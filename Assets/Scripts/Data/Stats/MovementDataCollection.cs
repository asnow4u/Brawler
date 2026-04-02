using UnityEngine;
using System;

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

    public event Action OnChangedEvent;

#if UNITY_EDITOR
    //NOTE: This is nessisary to allow for editor updating of data during playmode
    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (MoveData != null) RegisterToChangeEvents(MoveData);
        if (AirMoveData != null) RegisterToChangeEvents(AirMoveData);
        if (JumpData != null) RegisterToChangeEvents(JumpData);
        if (AirJumpData != null) RegisterToChangeEvents(AirJumpData);
        if (ClimbMoveData != null) RegisterToChangeEvents(ClimbMoveData);
        if (VaultData != null) RegisterToChangeEvents(VaultData);
        if (WallLeanData != null) RegisterToChangeEvents(WallLeanData);
    }

    private void RegisterToChangeEvents(BaseMovementData data)
    {
        data.OnChangedEvent += MovementDataChanged;
    }

    private void Unsubscribe()
    {
        if (MoveData != null) UnregisterFromChangeEvents(MoveData);
        if (AirMoveData != null) UnregisterFromChangeEvents(AirMoveData);
        if (JumpData != null) UnregisterFromChangeEvents(JumpData);
        if (AirJumpData != null) UnregisterFromChangeEvents(AirJumpData);
        if (ClimbMoveData != null) UnregisterFromChangeEvents(ClimbMoveData);
        if (VaultData != null) UnregisterFromChangeEvents(VaultData);
        if (WallLeanData != null) UnregisterFromChangeEvents(WallLeanData);
    }

    private void UnregisterFromChangeEvents(BaseMovementData data)
    {
        data.OnChangedEvent -= MovementDataChanged;
    }

    private void MovementDataChanged()
    {
        OnChangedEvent?.Invoke();
    }

    private void OnValidate()
    {
        MovementDataChanged();

        Unsubscribe();
        Subscribe();
    }

#endif
}
