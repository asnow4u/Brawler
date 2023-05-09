using SceneObj.Router;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SceneObj.Animation;
using SceneObj.Movement;
using SceneObj.Attack;
using System;

namespace SceneObj
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(AnimationHandler))]   
    public abstract class SceneObject : MonoBehaviour, IDamage
    {
        public ActionRouter router;
        public bool isGrounded;
        private Rigidbody rb;

        protected float damageTaken;


        protected virtual void Initialize()
        {        
            rb = GetComponent<Rigidbody>();
            router = new ActionRouter(this, GetComponent<MovementHandler>(), GetComponent<AttackHandler>(), GetComponentInChildren<AnimationHandler>());              
        }


        //TODO: make two rays on each side to prevent landing just on the edge and not getting jump reset
        protected void FixedUpdate()
        {
            if (Physics.Raycast(GetComponent<Collider>().bounds.center, Vector3.down, transform.GetComponent<Collider>().bounds.size.y / 2 + 0.1f, ~LayerMask.NameToLayer("Ground")))
            {
                if (rb.velocity.y < 0 && !isGrounded)
                {
                    isGrounded = true;
                    router.OnLand(rb.velocity.y);
                }
            }
            else
            {
                isGrounded = false;
            }
        }

        public bool IsFacingRightDirection()
        {
            float angleRightDiff = Vector3.Angle(transform.right, Vector3.right);
            float angleLeftDiff = Vector3.Angle(transform.right, Vector3.left);

            if (angleRightDiff < angleLeftDiff)
            {
                return true;
            }

            return false;
        }      


        public void ResetDamaget()
        {
            damageTaken = 0;
        }

        public void AddDamage(float percent)
        {
            damageTaken += percent;
        }   

        public void RemoveDamage(float percent)
        {
            damageTaken -= percent;
        }   

        public void ApplyForce(float basePower, Vector2 direction)
        {

            //Grab here in case change to mass
            Rigidbody rb = GetComponent<Rigidbody>();

            float force = rb.mass * (basePower * damageTaken);

            rb.AddForce(new Vector3(direction.x, direction.y, 0) * force, ForceMode.Impulse);

            //TODO: Set drag amount to 0.1 ish
            //Drag should reset when ground is touched or immobile is finished
        }
    }
}
