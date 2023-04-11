using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : SceneObject
{
    private float numLives;
    private float numDeaths;

    //CurrentEquipment

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        GetComponent<InputHandler>().SetUpInputEvents();
    }

    private void OnEnable()
    {
        //movement = GetComponent<PlayerMovement>();
        //movement.SetUpMovementEvents(input);

    }

    private void OnDisable()
    {
        GetComponent<InputHandler>().DisableInputEvents();
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
