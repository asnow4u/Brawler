using System;
using UnityEngine;

namespace Game.SceneObjects.ActionStates
{
    public enum ActionState 
    { 
        Idle, Moving, Attacking, HitStun
    };


    public class ActionStateHandler : SceneObjectHandler
    {
        [SerializeField] private ActionState curActionState;
        public ActionState CurActionState => curActionState;

        public event Action<ActionState> ActionStateChangedEvent;

        #region Initialize

        public override void Setup()
        {
            curActionState = ActionState.Idle;
        }


        public override void RegisterToEvents()
        { }


        public override void UnregisterToEvents()
        { }

        #endregion


        #region Action State

        /// <summary>
        /// Change the action state without consideration for priority
        /// </summary>
        /// <param name="newState"></param>
        public void ChangeState(ActionState newState)
        {
            if (newState != curActionState)
            {
                curActionState = newState;
                ActionStateChangedEvent?.Invoke(curActionState);
            }
        }


        /// <summary>
        /// Atempt to change action state based on if newState takes more priority
        /// </summary>
        /// <param name="newState"></param>
        /// <returns></returns>
        public bool TryChangeState(ActionState newState)
        {
            if (newState > curActionState)
            {
                ChangeState(newState);
                return true;
            }

            else if (newState == curActionState)
                return true;

            return false;
        }


        /// <summary>
        /// Reset State to Idle
        /// </summary>
        private void ResetState()
        {
            curActionState = ActionState.Idle;

            ActionStateChangedEvent?.Invoke(curActionState);
        }

        #endregion
    }
}