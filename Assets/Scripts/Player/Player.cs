using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SceneObj
{
    public class Player : SceneObject
    {
        private float numLives;
        private float numDeaths;

        //CurrentEquipment
        public void Start()
        {
            Initialize();
        }

        protected override void Initialize()
        {
            base.Initialize();
            
            GetComponent<PlayerInputHandler>().SetUpHandler(router);
        }

        private void OnDisable()
        {
            GetComponent<PlayerInputHandler>().DisableInputEvents();
        }




        //public float bounceDampener;
        //public float stallTimer;

        //TODO: move this to platforms
        //private void OnCollisionEnter(Collision collision)
        //{
        //    if (collision.gameObject.CompareTag("Platforms"))
        //    {
        //        if (stallTimer > 0) 
        //        {

        //            //F = m(v/t) || t=1
        //            Vector3 bounceForce = Vector3.Reflect(-1 * collision.relativeVelocity, collision.transform.up) * rb.mass;

        //            //Debug.Log("Bounce");
        //            //Debug.Log(bounceForce);

        //            rb.AddForce(bounceForce * bounceDampener, ForceMode.Impulse);
        //        }
        //    }
        //}
    }
}
