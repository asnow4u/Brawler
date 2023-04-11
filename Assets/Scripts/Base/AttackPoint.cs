using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Attack
{
    public class AttackPoint : MonoBehaviour
    {
        [SerializeField] private List<Collider> colliders;
        [SerializeField] private float damageOutput;
        [SerializeField] private float knockback;
        [SerializeField] private Vector3 forceDirection;

        public void Start()
        {
            colliders = new List<Collider>(GetComponents<Collider>());
        }


        public void EnableColliders()
        {
            foreach (Collider col in colliders)
            {
                col.enabled = true;
            }       
        }


        public void DisableColliders()
        {
            foreach (Collider col in colliders)
            {
                col.enabled = false;
            }
        }

        private void HitTarget(SceneObject target)
        {
            target.AddDamage(damageOutput);
            target.ApplyForce(GetComponent<Rigidbody>().mass, knockback, forceDirection);
        }


        private void OnTriggerEnter(Collider col)
        {
            SceneObject target = col.GetComponent<SceneObject>();

            if (target != null)
            {
                HitTarget(target);
            }
        }
    }
}
