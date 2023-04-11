using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Attack;

namespace Equipment
{
    public class Weapon : MonoBehaviour
    {
        private AttackHandler handler;

        private void Start()
        {
            handler = GetComponent<AttackHandler>();
        }
    }
}
