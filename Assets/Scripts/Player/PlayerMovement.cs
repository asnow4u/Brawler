using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Speed")]
    [SerializeField] private float speedX;
    [SerializeField] private float speedY;
    [Range(0, 20)]
    [SerializeField] private float maxVelocityX;
    [Range(0, 20)]
    [SerializeField] private float maxVelocityY;

    [Header("Jump")]
    [SerializeField] private float jumpForce;
    [SerializeField] int jumpsAvailable;


    public bool isGrounded;

    private Rigidbody rb;

    private float xAxis;
    private float yAxis;

    private int additionalJumpsPerformed;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }


    // Update is called once per frame
    void FixedUpdate()
    {
        ////TODO: make two rays on each side of the player to prevent landing just on the edge and not getting jump reset
        if (Physics.Raycast(transform.position, Vector3.down, transform.GetComponent<Collider>().bounds.size.y / 2 + 0.1f, ~LayerMask.NameToLayer("Ground")))
        {
            isGrounded = true;
            additionalJumpsPerformed = 0;
        }
        else
        {
            isGrounded = false;
        }


        if (isGrounded)
        {
            UpdateGroundMovement();
        }

        else
        {
            UpdateAirMovement();
        }        
    }

    private void UpdateGroundMovement()
    {
        //Horizontal Movement
        if (rb.velocity.x < maxVelocityX && rb.velocity.x > -maxVelocityX)
        {
            rb.AddForce(transform.right * xAxis * speedX);
        }


        //TODO: note, depending on sign of xAxis and direction character is facing might need to flip around
    }

    private void UpdateAirMovement()
    {
        if (rb.velocity.x < maxVelocityX && rb.velocity.x > -maxVelocityX)
        {
            rb.AddForce(transform.right * xAxis * speedX);
        }
    }


    public void UpdateHorizontalMovementSpeed(float horValue)
    {
        xAxis = horValue;
    }

    public void UpdateVerticalMovementSpeed(float verValue)
    {
        yAxis = verValue;
    }


    public void Jump()
    {
        if (isGrounded)
        {
            rb.AddForce(new Vector3(0, 1, 0) * jumpForce, ForceMode.Impulse);
        }

        else if (additionalJumpsPerformed < jumpsAvailable)
        {
            rb.AddForce(new Vector3(0, 1, 0) * jumpForce, ForceMode.Impulse);
            additionalJumpsPerformed++;
        }
    }
}
