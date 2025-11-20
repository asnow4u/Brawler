using System;
using System.Linq;
using UnityEngine;

namespace Game.SceneObjects.Equipment
{
    public class EquipmentHandler : SceneObjectHandler
    {
        public WeaponHandler WeaponHandler;

        #region Initialize

        public override void Setup()
        {
            SetupWeaponsCollection();
        }

        public override void RegisterToEvents()
        {
            //throw new NotImplementedException();
        }


        public override void UnregisterToEvents()
        {
            //throw new NotImplementedException();
        }

        /// <summary>
        /// Initialize <see cref="global::WeaponHandler"/> if it exists
        /// </summary>
        private void SetupWeaponsCollection()
        {
            WeaponHandler = GetComponentInChildren<WeaponHandler>();

            if (WeaponHandler != null)
                WeaponHandler.Setup();
        }

        #endregion


        public float GetEquipmentMass()
        {
            float accumulatedMass = 0f;

            if (WeaponHandler.EquippedWeapon != null)
                accumulatedMass += WeaponHandler.EquippedWeapon.Mass;

            return accumulatedMass;
        }
    }
}
