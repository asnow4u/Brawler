using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.SceneObjects.Movement
{
    [CreateAssetMenu(fileName = "MovementInputCollection", menuName = "ScriptableObjects/Movement/InputCollection")]

    public class MovementInputCollection : ScriptableObject
    {
        public MoveInputData MoveData;
        public AirMoveInputData AirMoveData;
        public ClimbInputData ClimbData;
        public JumpInputData JumpData;
        public AirJumpInputData AirJumpData;
        public WallLeanInputData WallLeanData;


        /// <summary>
        /// Attempt to get <paramref name="requestedMovement"/> by <paramref name="movementType"/>
        /// </summary>
        public bool TryGetMovementByType(MovementType movementType, out MovementInputData requestedMovement)
        {
            switch (movementType)
            {
                case MovementType.Move:
                    if (MoveData != null)
                    {
                        requestedMovement = MoveData;
                        return true;
                    }
                    break;

                case MovementType.AirMove:
                    if (AirMoveData != null)
                    {
                        requestedMovement = AirMoveData;
                        return true;
                    }
                    break;

                case MovementType.Climb:
                    if (ClimbData != null)
                    {
                        requestedMovement = ClimbData;
                        return true;
                    }
                    break;

                case MovementType.Jump:
                    if (JumpData != null)
                    {
                        requestedMovement = JumpData;
                        return true;
                    }
                    break;

                case MovementType.AirJump:
                    if (AirJumpData != null)
                    {
                        requestedMovement = AirJumpData;
                        return true;
                    }
                    break;

                case MovementType.WallLean:
                    if (WallLeanData != null)
                    {
                        requestedMovement = WallLeanData;
                        return true;
                    }
                    break;
            }

            requestedMovement = null;
            return false;
        }

        /// <summary>
        /// Attempt to get <paramref name="movement"/> by <paramref name="animation"/>
        /// </summary>
        public bool TryGetMovementFromAnimation(AnimationClip animation, out MovementInputData movement)
        {
            foreach (MovementType type in Enum.GetValues(typeof(MovementType)))
            {
                if (TryGetMovementByType(type, out movement))
                {
                    if (movement.Animation == animation)
                        return true;
                }
            }

            movement = null;
            return false;
        }


        #region Grounded Movement

        /// <summary>
        /// Grounded acceleration in collection
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        public float GetGroundedXAcceleration(float ratio)
        {            
            if (MoveData != null)
                return Mathf.Lerp(MoveData.GroundedXMaxAcceleration, MoveData.GroundedXMinAcceleration, Mathf.Clamp01(ratio));

            throw new NullReferenceException("MoveData is not set");
        }

        /// <returns>
        /// Grounded attack deceleration in collection
        /// </returns>
        /// <exception cref="NullReferenceException"></exception>
        public float GetGroundedAttackDecleration()
        {
            if (MoveData != null)
                return MoveData.GroundedAttackXDecleration;

            throw new NullReferenceException("MoveData is not set");
        }

        #endregion

        #region Air Movement        

        /// <returns>
        /// Aeiral X acceleration in collection
        /// </returns>
        /// <exception cref="NullReferenceException"></exception>
        public float GetAerialXAcceleration(float ratio)
        {
            if (AirMoveData != null)
                return Mathf.Lerp(AirMoveData.AerialXMaxAcceleration, AirMoveData.AerialXMinAcceleration, Mathf.Clamp01(ratio));

            throw new NullReferenceException("AirMoveData is not set");
        }

        /// <returns>
        /// Aeiral Y acceleration in collection
        /// </returns>
        /// <exception cref="NullReferenceException"></exception>
        public float GetAerialYAcceleration(float ratio)
        {
            if (AirMoveData != null)
                return Mathf.Lerp(AirMoveData.AerialYMaxAcceleration, AirMoveData.AerialYMinAcceleration, Mathf.Clamp01(ratio));

            throw new NullReferenceException("AirMoveData is not set");
        }

        #endregion


        #region Jump

        /// <summary>
        /// Attempt to get jump velocity if in collection
        /// </summary>
        public bool TryGetMinJumpVelocity(out float minJumpVelocity)
        {
            if (JumpData != null)
            {
                minJumpVelocity = JumpData.MinJumpVelocity;
                return true;
            }

            minJumpVelocity = 0;
            return false;
        }


        /// <summary>
        /// Attempt to get jump velocity if in collection
        /// </summary>
        public bool TryGetMaxJumpVelocity(out float maxJumpVelocity)
        {
            if (JumpData != null)
            {
                maxJumpVelocity = JumpData.MaxJumpVelocity;
                return true;
            }

            maxJumpVelocity = 0;
            return false;
        }

        #endregion
    }
}
