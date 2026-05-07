using UnityEngine;
using System;

[CreateAssetMenu(fileName = "MovementCollection", menuName = "ScriptableObjects/SceneObject/Movement/Collection")]
public class MovementDataCollection : ScriptableObject
{
    public MoveData MoveData;
    public AirMoveData AirMoveData;
    public JumpData JumpData;
    public AirJumpData AirJumpData;
    public WallJumpData WallJumpData;
    public ClimbMoveData ClimbMoveData;
    public LedgeClimbData LedgeClimbData;
    public WallLeanData WallLeanData;
    public WallSlideData WallSlideData;

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
        if (WallJumpData != null) RegisterToChangeEvents(WallJumpData);
        if (ClimbMoveData != null) RegisterToChangeEvents(ClimbMoveData);
        if (LedgeClimbData != null) RegisterToChangeEvents(LedgeClimbData);
        if (WallLeanData != null) RegisterToChangeEvents(WallLeanData);
        if (WallSlideData != null) RegisterToChangeEvents(WallSlideData);
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
        if (WallJumpData != null) UnregisterFromChangeEvents(WallJumpData);
        if (ClimbMoveData != null) UnregisterFromChangeEvents(ClimbMoveData);
        if (LedgeClimbData != null) UnregisterFromChangeEvents(LedgeClimbData);
        if (WallLeanData != null) UnregisterFromChangeEvents(WallLeanData);
        if (WallSlideData != null) UnregisterFromChangeEvents(WallSlideData);
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
