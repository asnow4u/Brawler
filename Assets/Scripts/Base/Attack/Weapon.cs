using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
