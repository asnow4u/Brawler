using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : SceneObject
{
    private PlayerButtonMap input;
    private PlayerMovement movement;
    private PlayerAction action;

    private float numLives;
    private float numDeaths;

    //CurrentEquipment



    private void OnEnable()
    {
        input = new PlayerButtonMap();
        input.Enable();

        movement = GetComponent<PlayerMovement>();
        movement.SetUpMovementEvents(input);

        action = GetComponent<PlayerAction>();
        action.SetUpActionEvents(input);

        action.SetUpAttackAction(currentWeapons);
    }

    private void OnDisable()
    {
        input.Disable();
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
