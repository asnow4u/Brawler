using Game.SceneObjects.ActionStates;
using Game.SceneObjects.Attack;
using Game.SceneObjects.Movement;
using System;
using UnityEngine;

namespace Game.SceneObjects
{
    public class SceneObjectLogger
    {
        SceneObject sceneObject;

        public SceneObjectLogger(SceneObject sceneObject)
        {
            this.sceneObject = sceneObject;
            SetupEventListeners();
        }


        #region Events

        private void SetupEventListeners()
        {
            //Ground state
            sceneObject.GroundedStateChangedEvent += OnGroundStateChanged;

            //Climb state
            sceneObject.ClimbStateChangedEvent += OnClimbStateChanged;

            //Action State
            sceneObject.ActionStateHandler.ActionStateChangedEvent += OnActionStateChanged;

            //Movement
            sceneObject.MovementInputHandler.MoveStateChangedEvent += OnMoveStateChanged;
            sceneObject.MovementInputHandler.MovementCollectionChangedEvent += OnMoveCollectionChanged;

            //Attack
            sceneObject.AttackInputHandler.AttackStateChangedEvent += OnAttackStateChanged;

            //Animation
            sceneObject.AnimationHandler.AnimationStartedEvent += OnAnimationStarted;
            sceneObject.AnimationHandler.AnimationEndedEvent += OnAnimationEnded;
        }


        /// <summary>
        /// Log ground state change
        /// </summary>
        /// <param name="groundedState"></param>
        private void OnGroundStateChanged(GroundedState groundState)
        {
            Log(sceneObject.ObjectType + ": " + sceneObject.gameObject.name + ": GroundState: " + groundState);
        }


        /// <summary>
        /// Log climb state change
        /// </summary>
        /// <param name="climbState"></param>
        private void OnClimbStateChanged(ClimbState prevCLimbState, ClimbState climbState)
        {
            Log(sceneObject.ObjectType + ": " + sceneObject.gameObject.name + ": ClimbState: " + climbState);
        }


        /// <summary>
        /// Log action state change
        /// </summary>
        /// <param name="state"></param>    
        private void OnActionStateChanged(ActionState actionState)
        {
            Log(sceneObject.ObjectType + ": " + sceneObject.gameObject.name + ": ActionState: " + actionState);
        }


        /// <summary>
        /// Log move collection change
        /// </summary>
        /// <param name="collection"></param>
        private void OnMoveCollectionChanged(MovementCollection collection)
        {
            Log(sceneObject.ObjectType + ": " + sceneObject.gameObject.name + ": Move Collection Changed");
        }


        /// <summary>
        /// Log move state change
        /// </summary>
        /// <param name="type"></param>
        private void OnMoveStateChanged(MovementType moveState)
        {
            Log(sceneObject.ObjectType + ": " + sceneObject.gameObject.name + ": MoveState: " + moveState);
        }


        /// <summary>
        /// Log attack state change
        /// </summary>
        /// <param name="type"></param>
        private void OnAttackStateChanged(AttackType prevAttackState, AttackType currentAttackState)
        {
            Log(sceneObject.ObjectType + ": " + sceneObject.gameObject.name + ": AttackState: " + currentAttackState + ", from " + prevAttackState);
        }


        /// <summary>
        /// Log animation start
        /// </summary>
        /// <param name="clip"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void OnAnimationStarted(AnimationClip clip)
        {
            Log(sceneObject.ObjectType + ": " + sceneObject.gameObject.name + ": Animation Started: " + clip.name);
        }


        /// <summary>
        /// Log animation end
        /// </summary>
        /// <param name="clip"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void OnAnimationEnded(AnimationClip clip)
        {
            Log(sceneObject.ObjectType + ": " + sceneObject.gameObject.name + ": Animation Ended: " + clip.name);
        }

        #endregion


        #region Logs

        /// <summary>
        /// Log a debugLog to the console information about a sceneObject
        /// </summary>
        public void Log(string log)
        {
            Debug.Log(log, sceneObject);
        }


        /// <summary>
        /// Log a debugLog to the console information about a sceneObject
        /// </summary>
        public void LogWarning(string log)
        {
            Debug.Log(log, sceneObject);
        }


        /// <summary>
        /// Log a debugLog to the console information about a sceneObject
        /// </summary>
        public void LogError(string log)
        {
            Debug.LogError(log, sceneObject);
        }

        #endregion
    }
}
