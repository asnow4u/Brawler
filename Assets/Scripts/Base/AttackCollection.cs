using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Attack
{
    public class AttackCollection : MonoBehaviour
    {
        [SerializeField] private List<Attack> attacks;


        [System.Serializable]
        public class Attack
        {
            public AttackType.Type type;
            public AnimationClip animationClip;
            public List<AttackPoint> points;

            public void EnableColliders()
            {
                foreach (AttackPoint point in points)
                {
                    point.EnableColliders();
                }
            }

            public void DisableColliders()
            {
                foreach (AttackPoint point in points)
                {
                    point.DisableColliders();
                }
            }
        }


        public bool GetAttackByType(AttackType.Type attackType, out Attack requestedAttack)
        {
            requestedAttack = null;

            foreach (Attack attack in attacks)
            {
                if (attack.type == attackType)
                {
                    requestedAttack = attack;
                    return true;
                }
            }

            return false;
        }
    }
}
