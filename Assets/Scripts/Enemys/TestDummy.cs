
using Game.SceneObjects.Animation;
using Game.SceneObjects.Attack;
using Game.SceneObjects.Equipment;
using Game.SceneObjects.Movement;
using UnityEngine;

namespace Game.SceneObjects
{
    public class TestDummy : Enemy
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


        public override bool IsDownAttackActive()
        {
            throw new System.NotImplementedException();
        }

        public override bool IsHorizontalMovementActive()
        {
            throw new System.NotImplementedException();
        }

        public override bool IsLeftAttackActive()
        {
            throw new System.NotImplementedException();
        }

        public override bool IsRightAttackActive()
        {
            throw new System.NotImplementedException();
        }

        public override bool IsUpAttackActive()
        {
            throw new System.NotImplementedException();
        }

        public override bool IsVerticalJumpActive()
        {
            throw new System.NotImplementedException();
        }

        public override void PerformDownAttack()
        {
            throw new System.NotImplementedException();
        }

        public override void PerformMovement(Vector2 movement)
        {
            throw new System.NotImplementedException();
        }

        public override void PerformInteraction()
        {
            throw new System.NotImplementedException();
        }

        public override void PerformLeftAttack()
        {
            throw new System.NotImplementedException();
        }

        public override void PerformRightAttack()
        {
            throw new System.NotImplementedException();
        }

        public override void PerformUpAttack()
        {
            throw new System.NotImplementedException();
        }

        public override void PerformVerticalJump(float jumpStrength)
        {
            throw new System.NotImplementedException();
        }

        public override void StopHorizontalMovement()
        {
            throw new System.NotImplementedException();
        }

        public override void StopJumpMovement()
        {
            throw new System.NotImplementedException();
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
        }
    }
}
