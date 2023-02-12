using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class Player : MonoBehaviour
{
    private PlayerButtonMap input;
    private AnimatorController animator;

    private float numLives;
    private float numDeaths;
    

    public float totalDamage;
    public float damageMultiplier;
    public float bounceDampener;

    //CurrentEquipment

    public float stallTimer;


    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = gameObject.AddComponent<AnimatorController>();
    }

    private void OnEnable()
    {
        input = new PlayerButtonMap();
        input.Enable();

        SetUpMovementEvents(input);
        SetUpActionEvents(input);
    }

    private void OnDisable()
    {
        input.Disable();
    }



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
