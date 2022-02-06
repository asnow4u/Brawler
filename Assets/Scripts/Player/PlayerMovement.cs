using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


//NOTE:
//https://unity.com/roadmap/unity-platform/multiplayer-networking?_ga=2.11311988.1592772763.1644094688-1905360034.1638849034
//There is a Client-side Perdiction feature being worked on for unity netcode
//Currently Unity states that currently that having the phisics run on the server is the best
//"With future prediction support of Netcode, the latency will no longer be an issue which makes this the best choice of a movement model for a game like this." (https://docs-multiplayer.unity3d.com/docs/learn/bitesize-spaceshooter)

public class PlayerMovement : NetworkBehaviour
{
    private NetworkVariable<Vector3> horizontalMovement = new NetworkVariable<Vector3>();

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
        if (IsServer)
            ServerUpdate();
        

        if (IsClient && IsOwner)
            ClientUpdate();
        
    }


    private void ClientUpdate()
    {
        // Movement on X-Axis
        float x = Input.GetAxis("Horizontal");
        Vector3 movement = transform.right * x * speed_x;


        Debug.Log("Movement Update Sent: " + movement);
        UpdateClientPositionServerRpc(movement);

        if (!IsHost)
            transform.Translate(movement * Time.deltaTime);


        //TODO: make two rays on each side of the player to prevent landing just on the edge and not getting jump reset
        if (Physics.Raycast(transform.position, Vector3.down, transform.GetComponent<Collider>().bounds.size.y / 2 + 0.1f, ~LayerMask.NameToLayer("Ground")))
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

                UpdateClientJumpServerRpc(transform.up * speed_y);
                isGrounded = false;
                
                if (!IsHost) 
                {
                    rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
                    rb.AddForce(transform.up * speed_y, ForceMode.Impulse);
                }
            }

            else if (doubleJumpCheck)
            {
                UpdateClientJumpServerRpc(transform.up * speed_y);
                doubleJumpCheck = false;
                
                if (!IsHost)
                {
                    rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
                    rb.AddForce(transform.up * speed_y, ForceMode.Impulse);
                }
            }
        }
    }


    private void ServerUpdate()
    {
        if (horizontalMovement.Value != Vector3.zero)
        {
            transform.Translate(horizontalMovement.Value * Time.deltaTime);
            UpdateClientPostionClientRPC(transform.position);
        }
    }


    [ServerRpc]
    private void UpdateClientPositionServerRpc(Vector3 movement)
    {
        Debug.Log("Position Update Recieved: " + movement);
        horizontalMovement.Value = movement;
    }


    [ServerRpc]
    private void UpdateClientJumpServerRpc(Vector3 force)
    {

        if (force != Vector3.zero)
        {
            rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            rb.AddForce(force, ForceMode.Impulse);
        }
    }


    [ClientRpc]
    private void UpdateClientPostionClientRPC(Vector3 pos)
    {
        Debug.Log("Reset Client Pos to Server");
        transform.position = pos;
    }
}
