using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

//NOTE:
//https://unity.com/roadmap/unity-platform/multiplayer-networking?_ga=2.11311988.1592772763.1644094688-1905360034.1638849034
//There is a Client-side Perdiction feature being worked on for unity netcode
//Currently Unity states that currently that having the phisics run on the server is the best
//"With future prediction support of Netcode, the latency will no longer be an issue which makes this the best choice of a movement model for a game like this." (https://docs-multiplayer.unity3d.com/docs/learn/bitesize-spaceshooter)

public class PlayerMovement : NetworkBehaviour
{
    public float speed_x;
    public float speed_y;

    public bool isGrounded;
    public int jumpsAvailable;
    private bool doubleJumpCheck;

    private Rigidbody rb;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {

        //Client authoritative for movement
        if (isLocalPlayer)
        {

            // Movement on X-Axis
            float x = Input.GetAxis("Horizontal");
            Vector3 movement = transform.right * x * speed_x;

            transform.Translate(movement * Time.deltaTime);

            //TODO: make two rays on each side of the player to prevent landing just on the edge and not getting jump reset
            if (  Physics.Raycast(transform.position + new Vector3((transform.GetComponent<Collider>().bounds.size.x / 2) * (.7f), 0, 0), Vector3.down, transform.GetComponent<Collider>().bounds.size.y / 2 + 0.1f, ~LayerMask.NameToLayer("Ground"))
               || Physics.Raycast(transform.position - new Vector3((transform.GetComponent<Collider>().bounds.size.x / 2) * (.7f), 0, 0), Vector3.down, transform.GetComponent<Collider>().bounds.size.y / 2 + 0.1f, ~LayerMask.NameToLayer("Ground")))
            {
                isGrounded = true;
                doubleJumpCheck = true;
            }
            else
            {
                isGrounded = false;
            }


            //Jump
            if (Input.GetKeyDown(KeyCode.Space))
            {

                //    //Player is hanging from a ledge
                //    if (ledgeFist != null)
                //    {
                //        ledgeFist.GetComponent<FistController>().StopHanging();

                //        Jump();

                //        doubleJumpCheck = true;
                //        ledgeFist = null;
                //    }

                //    else
                if (isGrounded)
                {
   
                    rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
                    rb.AddForce(transform.up * speed_y, ForceMode.Impulse);
                    isGrounded = false;
                }

                else if (doubleJumpCheck)
                {
                    rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
                    rb.AddForce(transform.up * speed_y, ForceMode.Impulse);
                    doubleJumpCheck = false;
                }
            }
        }
    }
}
