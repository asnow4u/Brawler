using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Game.SceneObjects.Collision
{
    public class CollisionHandler : SceneObjectHandler
    {
        [SerializeField] DamageCollider baseDamageCollider; //TODO: This will become more than a single collider in the future.        

        private const float INVICIBILITY_TIME_AFTER_HIT = 0.2f;
        private const float MAX_COLLISION_RESOLVE_TIME = 0.1f;

        public Collider Collider => GetComponent<Collider>();

        public override void Setup()
        {
            if (baseDamageCollider == null)
                throw new MissingReferenceException("Base Damage Collider is not assigned in the Collision Handler.");
        }

        #region Events

        public override void RegisterToEvents()
        {
            baseDamageCollider.OnHit += HandleCollision;

            if (sceneObject.EquipmentHandler != null)
                sceneObject.EquipmentHandler.WeaponHandler.OnWeaponEquippedEvent += OnWeaponEquipped;
        }


        public override void UnregisterToEvents()
        {
            baseDamageCollider.OnHit -= HandleCollision;

            if (sceneObject.EquipmentHandler != null)
                sceneObject.EquipmentHandler.WeaponHandler.OnWeaponEquippedEvent -= OnWeaponEquipped;
        }

        private void OnWeaponEquipped(Weapon previousWeapon, Weapon currentWeapon)
        {
            if (previousWeapon != null)
            {
                foreach (DamageCollider collider in previousWeapon.DamageColliders)
                    collider.OnHit -= HandleCollision;
            }

            if (currentWeapon != null)
            {
                foreach (DamageCollider collider in currentWeapon.DamageColliders)
                    collider.OnHit += HandleCollision;
            }
        }

        #endregion


        private async void HandleCollision(DamageCollisionHitData data)
        {
            SceneObject targetSO = data.TargetData.SceneObject;

            if (targetSO.DamageHandler.CheckForImmunity(sceneObject.UniqueId))
                return;            

            //TODO: Calculate resolveTime based on damage and totalDamage
            float resolveTime = MAX_COLLISION_RESOLVE_TIME;

            //Apply Immunity (Prevent bounce back hits if sceneObject hits wall immediatly)
            sceneObject.DamageHandler.SetImmunity(targetSO.UniqueId, INVICIBILITY_TIME_AFTER_HIT);
            //Apply Immunity to target to prevent multi hits from single attack
            targetSO.DamageHandler.SetImmunity(sceneObject.UniqueId, INVICIBILITY_TIME_AFTER_HIT);

            //Pause animations
            sceneObject.Freeze();
            targetSO.Freeze();

            await Task.Delay(TimeSpan.FromSeconds(resolveTime));

            sceneObject.UnFreeze();
            targetSO.UnFreeze();

            UIFactory.Instance.SpawnDamageBubble(data.TargetData.ContactPoint, data.SourceData.Damage);            
            targetSO.DamageHandler.HitByCollision(data.SourceData);
        }


        #region Directional Collision

        /// <summary>
        /// Try to detect if a collider exists
        /// </summary>
        public bool TryDetectCollision(Direction direction, float dist, LayerMask mask, out Collider collidingCollider)
        {
            collidingCollider = null;

            switch (direction)
            {
                case Direction.Left:
                    collidingCollider = LeftSideCollisionDetection(dist, mask);
                    break;

                case Direction.Right:
                    collidingCollider = RightSideCollisionDetection(dist, mask);
                    break;

                case Direction.Up:
                    collidingCollider = UpSideCollisionDetection(dist, mask);
                    break;

                case Direction.Down:
                    collidingCollider = DownSideCollisionDetection(dist, mask);
                    break;
            }

            return collidingCollider != null;
        }


        /// <summary>
        /// Check right side for any collisions
        /// </summary>
        private Collider RightSideCollisionDetection(float dist, LayerMask mask)
        {
            Vector3 point1 = Collider.bounds.center + Vector3.up * Collider.bounds.extents.y;
            Vector3 point2 = Collider.bounds.center + Vector3.down * Collider.bounds.extents.y;
            float spaceBetweenRays = (point1.y - point2.y) / 10;

            for (int i = 0; i < 10; i++)
            {
                Vector3 origin = point1 + Vector3.down * spaceBetweenRays * i;

                if (Physics.Raycast(origin, Vector3.right, out RaycastHit hit, Collider.bounds.extents.x + dist, mask))
                {
                    return hit.collider;
                }
            }

            return null;
        }


        /// <summary>
        /// Check left side for any collisions
        /// </summary>
        private Collider LeftSideCollisionDetection(float dist, LayerMask mask)
        {
            Vector3 point1 = Collider.bounds.center + Vector3.up * Collider.bounds.extents.y;
            Vector3 point2 = Collider.bounds.center + Vector3.down * Collider.bounds.extents.y;
            float spaceBetweenRays = (point1.y - point2.y) / 10;

            for (int i = 0; i < 10; i++)
            {
                Vector3 origin = point1 + Vector3.down * spaceBetweenRays * i;

                if (Physics.Raycast(origin, Vector3.left, out RaycastHit hit, Collider.bounds.extents.x + dist, mask))
                {
                    return hit.collider;
                }
            }

            return null;
        }


        /// <summary>
        /// Check up for any collisions
        /// </summary>
        private Collider UpSideCollisionDetection(float dist, LayerMask mask)
        {
            Vector3 point1 = Collider.bounds.center + Vector3.right * Collider.bounds.extents.x;
            Vector3 point2 = Collider.bounds.center + Vector3.left * Collider.bounds.extents.x;
            float spaceBetweenRays = (point1.x - point2.x) / 10;

            for (int i = 0; i < 10; i++)
            {
                Vector3 origin = point1 + Vector3.left * spaceBetweenRays * i;

                if (Physics.Raycast(origin, Vector3.up, out RaycastHit hit, Collider.bounds.extents.y + dist, mask))
                {
                    return hit.collider;
                }
            }

            return null;
        }


        /// <summary>
        /// Check down for any collisions
        /// </summary>
        private Collider DownSideCollisionDetection(float dist, LayerMask mask)
        {
            Vector3 point1 = Collider.bounds.center + Vector3.right * Collider.bounds.extents.x;
            Vector3 point2 = Collider.bounds.center + Vector3.left * Collider.bounds.extents.x;
            float spaceBetweenRays = (point1.x - point2.x) / 10;

            for (int i = 0; i < 10; i++)
            {
                Vector3 origin = point1 + Vector3.left * spaceBetweenRays * i;

                if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, Collider.bounds.extents.y + dist, mask))
                {
                    return hit.collider;
                }
            }

            return null;
        }

        #endregion
    }
}
