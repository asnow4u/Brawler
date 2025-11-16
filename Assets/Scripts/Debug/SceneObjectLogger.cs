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

        private void SetupEventListeners()
        {
            //Ground state
            sceneObject.GroundedStateChangedEvent += OnGroundStateChanged;

            //Action State
            sceneObject.ActionStateHandler.ActionStateChangedEvent += OnActionStateChanged;

            //Movement
            if (sceneObject.MovementInputHandler != null)
            {
                sceneObject.ClimbStateChangedEvent += OnClimbStateChanged;
                sceneObject.MovementInputHandler.InputDataChangedEvent += OnMoveInputChanged;
                sceneObject.MovementInputHandler.CollectionChangedEvent += OnMoveCollectionChanged;
            }

            //Attack
            if (sceneObject.AttackInputHandler != null)
            {
                sceneObject.AttackInputHandler.AttackStateChangedEvent += OnAttackStateChanged;
                sceneObject.AttackInputHandler.CollectionChangedEvent += OnAttackCollectionChanged;
            }

            //Animation
            if (sceneObject.AnimationHandler != null)
            {
                sceneObject.AnimationHandler.AnimationStartedEvent += OnAnimationStarted;
                sceneObject.AnimationHandler.AnimationEndedEvent += OnAnimationEnded;
            }
        }


        /// <summary>
        /// Log ground state change
        /// </summary>
        private void OnGroundStateChanged(GroundedState groundState)
        {
            Log("GroundState: " + groundState);
        }


        /// <summary>
        /// Log climb state change
        /// </summary>
        private void OnClimbStateChanged(ClimbState prevCLimbState, ClimbState climbState)
        {
            Log("ClimbState: " + climbState);
        }


        /// <summary>
        /// Log action state change
        /// </summary>
        private void OnActionStateChanged(ActionState actionState)
        {
            Log("ActionState: " + actionState);
        }


        /// <summary>
        /// Log move collection change
        /// </summary>
        private void OnMoveCollectionChanged(MovementCollection collection)
        {
            Log("Move Collection Changed");
        }


        /// <summary>
        /// Log move state change
        /// </summary>
        private void OnMoveInputChanged(MovementInputData inputData)
        {
            if (inputData == null)
                Log("MoveState: Null");
            else
                Log("MoveState: " + inputData.Type);
        }


        /// <summary>
        /// Log attack state change
        /// </summary>
        private void OnAttackStateChanged(AttackType prevAttackState, AttackType currentAttackState)
        {
            Log("AttackState: " + currentAttackState + ", from " + prevAttackState);
        }

        /// <summary>
        /// Log change of Atttack Collection
        /// </summary>
        private void OnAttackCollectionChanged(AttackCollection collection)
        {
            Log("Attack Collection Changed");
        }

        /// <summary>
        /// Log animation start
        /// </summary>
        private void OnAnimationStarted(AnimationClip clip)
        {
            Log("Animation Started: " + clip.name);
        }


        /// <summary>
        /// Log animation end
        /// </summary>
        private void OnAnimationEnded(AnimationClip clip)
        {
            Log("Animation Ended: " + clip.name);
        }

        /// <summary>
        /// Log a debugLog to the console information about a sceneObject
        /// </summary>
        public void Log(string log)
        {
            Debug.Log($"({sceneObject.ObjectType}) {sceneObject.gameObject.name}: {log}", sceneObject);
        }
    }
}
