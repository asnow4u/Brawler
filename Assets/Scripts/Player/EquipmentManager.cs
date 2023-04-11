using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Equipment
{
    public class EquipmentManager : MonoBehaviour
    {
        private WeaponCollection weaponCollection;

        public void SetUp()
        {
            weaponCollection = new WeaponCollection();
        }
    
    }
}
