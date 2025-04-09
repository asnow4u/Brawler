using System;
using UnityEngine;

namespace Game.SceneObjects.Movement
{
    [CreateAssetMenu(fileName = "MovementCollection", menuName = "ScriptableObjects/Movement/Collection")]

    public class MovementCollection : ScriptableObject
    {
        public MoveData MoveData;
        public AirMoveData AirMoveData;
        public JumpData JumpData;
        public AirJumpData AirJumpData;
        public LandData LandData;
        public WallLeanData WallLeanData;
        public WallSlideData WallSlideData;
        public WallJumpData WallJumpData;


        /// <summary>
        /// Attempt to get <paramref name="requestedMovement"/> by <paramref name="movementType"/>
        /// </summary>
        public bool TryGetMovementByType(MovementType movementType, out MovementData requestedMovement)
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

                case MovementType.Landing:
                    if (LandData != null)
                    {
                        requestedMovement = LandData;
                        return true;
                    }
                    break;

                //case MovementType.WallLean:
                //    if (WallLeanData != null)
                //    {
                //        requestedMovement = WallLeanData;
                //        return true;
                //    }
                //    break;

                //case MovementType.WallSlide:
                //    if (WallSlideData != null)
                //    {
                //        requestedMovement = WallSlideData;
                //        return true;
                //    }
                //    break;

                //case MovementType.WallJump:
                //    if (WallJumpData != null)
                //    {
                //        requestedMovement = WallJumpData;
                //        return true;
                //    }
                //    break;

            }

            requestedMovement = null;
            return false;
        }


        /// <summary>
        /// Attempt to get <paramref name="movement"/> by <paramref name="animation"/>
        /// </summary>
        public bool TryGetMovementFromAnimation(AnimationClip animation, out MovementData movement)
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

        /// <returns>
        /// Grounded max velocity in collection
        /// </returns>
        /// <exception cref="NullReferenceException"></exception>
        public float GetGroundedMaxXVelocity()
        {
            if (MoveData != null)
                return MoveData.GroundedMaxXVelocity;

            throw new NullReferenceException("MoveData is not set");
        }


        /// <summary>
        /// Grounded acceleration in collection
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        public float GetGroundedXAcceleration()
        {
            if (MoveData != null)
                return MoveData.GroundedXAcceleration;

            throw new NullReferenceException("MoveData is not set");
        }


        /// <returns>
        /// Grounded deceleration in collection
        /// </returns>
        /// <exception cref="NullReferenceException"></exception>
        public float GetGroundedXDeceleration()
        {
            if (MoveData != null)
                return MoveData.GroundedXDeceleration;

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
        /// Aeiral max X velocity in collection
        /// </returns>
        /// <exception cref="NullReferenceException"></exception>
        public float GetAerialMaxXVelocity()
        {
            if (AirMoveData != null)
                return AirMoveData.AerialMaxXVelocity;

            throw new NullReferenceException("AirMoveData is not set");
        }


        /// <returns>
        /// Aeiral max Y velocity in collection
        /// </returns>
        /// <exception cref="NullReferenceException"></exception>
        public float GetAerialMaxYVelocity()
        {
            if (AirMoveData != null)
                return AirMoveData.AerialMaxYVelocity;
            
            throw new NullReferenceException("AirMoveData is not set");
        }



        /// <returns>
        /// Aeiral X acceleration in collection
        /// </returns>
        /// <exception cref="NullReferenceException"></exception>
        public float GetAerialXAcceleration()
        {
            if (AirMoveData != null)
                return AirMoveData.AerialXAcceleration;

            throw new NullReferenceException("AirMoveData is not set");
        }


        /// <returns>
        /// Aeiral X decceleration in collection
        /// </returns>
        /// <exception cref="NullReferenceException"></exception>
        public float GetAerialXDeceleration()
        {
            if (AirMoveData != null)
                return AirMoveData.AerialXDeceleration;

            throw new NullReferenceException("AirMoveData is not set");
        }


        /// <returns>
        /// Aeiral Y acceleration in collection
        /// </returns>
        /// <exception cref="NullReferenceException"></exception>
        public float GetAerialYAcceleration()
        {
            if (AirMoveData != null)
                return AirMoveData.AerialYAcceleration;
            
            throw new NullReferenceException("AirMoveData is not set");
        }


        /// <returns>
        /// Aeiral Y decceleration in collection
        /// </returns>
        /// <exception cref="NullReferenceException"></exception>
        public float GetAerialYDeceleration()
        {
            if (AirMoveData != null)
                return AirMoveData.AerialYDeceleration;

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


        /// <summary>
        /// Attempt to get the velocity for wall jumping if in collection
        /// </summary>
        public bool TryGetWallJumpVelocity(out float jumpVelocity)
        {
            if (WallJumpData != null)
            {
                jumpVelocity = WallJumpData.JumpVelocity;
                return true;
            }

            jumpVelocity = 0;
            return false;
        }

        #endregion     
    }
}
