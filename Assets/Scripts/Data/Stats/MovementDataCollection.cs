using UnityEngine;
using System;

[CreateAssetMenu(fileName = "MovementCollection", menuName = "ScriptableObjects/SceneObject/Movement/Collection")]
public class MovementDataCollection : ScriptableObject
{
    public MoveData MoveData;
    public AirMoveData AirMoveData;
    public GroundedJumpData GroundedJumpData;
    public AirJumpData AirJumpData;
    public WallJumpData WallJumpData;
    public ClimbMoveData ClimbMoveData;
    public LedgeClimbData LedgeClimbData;
    public WallLeanData WallLeanData;
    public WallSlideData WallSlideData;

    #region Editor Updating

    public event Action OnChangedEvent;

    #if UNITY_EDITOR

        private void OnValidate()
        {
            if (Application.isPlaying)
                OnChangedEvent?.Invoke();
        }

    #endif

    #endregion
}
