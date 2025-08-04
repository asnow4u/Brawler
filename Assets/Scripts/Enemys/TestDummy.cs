
using Game.SceneObjects.Animation;
using Game.SceneObjects.Attack;
using Game.SceneObjects.Equipment;
using Game.SceneObjects.Movement;
using UnityEngine;

namespace Game.SceneObjects
{
    public class TestDummy : Enemy, IMovementInput, IAttackInput
    {
        protected override void GetHandlers()
        {
            base.GetHandlers();

            EquipmentHandler = GetComponent<EquipmentHandler>();
            InteractionHandler = GetComponent<InteractionHandler>();
            MovementInputHandler = GetComponent<MovementInputHandler>();
            AttackInputHandler = GetComponent<AttackInputHandler>();
            AnimationHandler = GetComponent<AnimationHandler>();
        }

        public bool IsHorizontalMovementActive()
        {
            throw new System.NotImplementedException();
        }

        public bool IsVerticalJumpActive()
        {
            throw new System.NotImplementedException();
        }

        public void PerformMovement(Vector2 movement)
        {
            throw new System.NotImplementedException();
        }

        public void PerformVerticalJump(float jumpStrength)
        {
            throw new System.NotImplementedException();
        }

        public void StopHorizontalMovement()
        {
            throw new System.NotImplementedException();
        }

        public void StopJumpMovement()
        {
            throw new System.NotImplementedException();
        }

        public void PerformUpAttack()
        {
            throw new System.NotImplementedException();
        }

        public void PerformDownAttack()
        {
            throw new System.NotImplementedException();
        }

        public void PerformLeftAttack()
        {
            throw new System.NotImplementedException();
        }

        public void PerformRightAttack()
        {
            throw new System.NotImplementedException();
        }

        public bool IsUpAttackActive()
        {
            throw new System.NotImplementedException();
        }

        public bool IsDownAttackActive()
        {
            throw new System.NotImplementedException();
        }

        public bool IsLeftAttackActive()
        {
            throw new System.NotImplementedException();
        }

        public bool IsRightAttackActive()
        {
            throw new System.NotImplementedException();
        }
    }
}
