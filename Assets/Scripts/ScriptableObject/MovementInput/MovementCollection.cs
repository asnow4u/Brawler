using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.SceneObjects.Movement
{
    [CreateAssetMenu(fileName = "MovementInputCollection", menuName = "ScriptableObjects/Movement/InputCollection")]

    public class MovementCollection : ScriptableObject
    {
        [SerializeField] private MoveInputData moveInput;
        [SerializeField] private AirMoveInputData airMoveInput;
        [SerializeField] private JumpInputData jumpInput;
        [SerializeField] private AirJumpInputData airJumpInput;
        [SerializeField] private ClimbMoveInputData climbMoveInput;
        [SerializeField] private VaultInputData valueInput;
        [SerializeField] private WallLeanInputData wallLeanInput;
        [SerializeField] private HeavyLandingInputData heavyLandingInput;

        public List<MovementInputData> MovementData => new List<MovementInputData>() 
        { 
            moveInput,
            airJumpInput,
            jumpInput,
            airMoveInput,
            climbMoveInput,
            valueInput,
            wallLeanInput,
            heavyLandingInput
        };

        /// <returns>
        /// A movementData of type T if found in collection, null otherwise
        /// </returns>
        public T GetMovementData<T>() where T : MovementInputData
        {
            foreach (var data in MovementData)
            {
                if (data.GetType() ==  typeof(T))
                    return (T)data;
            }
            return null;
        }

        /// <summary>
        /// Attempt to get <paramref name="movement"/> by <paramref name="animation"/>
        /// </summary>
        public bool TryGetMovementFromAnimation(AnimationClip animation, out MovementInputData movement)
        {
            foreach (MovementInputData moveData in MovementData)
            {
                if (moveData.Animation == animation)
                {
                    movement = moveData;
                    return true;
                }
            }

            movement = null;
            return false;
        }

        /// <returns>
        /// The index of the given movement input in the collection
        /// </returns>
        public int GetIndex(MovementInputData movement)
        {
            return MovementData.IndexOf(movement);
        }

        #region Grounded Movement

        /// <summary>
        /// Grounded acceleration in collection
        /// </summary>
        /// <returns></returns>
        /// <exception cref="MissingReferenceException"></exception>
        public float GetGroundedXAcceleration(float ratio)
        {            
            MoveInputData moveData = GetMovementData<MoveInputData>();
            if (moveData != null)
                return Mathf.Lerp(moveData.GroundedXMaxAcceleration, moveData.GroundedXMinAcceleration, Mathf.Clamp01(ratio));

            throw new MissingReferenceException("MoveData is not set");
        }

        /// <returns>
        /// Grounded attack deceleration in collection
        /// </returns>
        /// <exception cref="MissingReferenceException"></exception>
        public float GetGroundedAttackDecleration()
        {
            MoveInputData moveData = GetMovementData<MoveInputData>();
            if (moveData != null)
                return moveData.GroundedAttackXDecleration;

            throw new MissingReferenceException("MoveData is not set");
        }

        #endregion


        #region Air Movement        

        /// <returns>
        /// Aeiral X acceleration in collection
        /// </returns>
        /// <exception cref="MissingReferenceException"></exception>
        public float GetAerialXAcceleration(float ratio)
        {
            AirMoveInputData airMoveData = GetMovementData<AirMoveInputData>();
            if (airMoveData != null)
                return Mathf.Lerp(airMoveData.AerialXMaxAcceleration, airMoveData.AerialXMinAcceleration, Mathf.Clamp01(ratio));

            throw new MissingReferenceException("AirMoveData is not set");
        }

        /// <returns>
        /// Aeiral Y acceleration in collection
        /// </returns>
        /// <exception cref="MissingReferenceException"></exception>
        public float GetAerialYAcceleration(float ratio)
        {
            AirMoveInputData airMoveData = GetMovementData<AirMoveInputData>();
            if (airMoveData != null)
                return Mathf.Lerp(airMoveData.AerialYMaxAcceleration, airMoveData.AerialYMinAcceleration, Mathf.Clamp01(ratio));

            throw new MissingReferenceException("AirMoveData is not set");
        }

        #endregion   
    }
}
