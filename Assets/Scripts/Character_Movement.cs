using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class Character_Movement : NetworkBehaviour
{
    //Network Variable
    private NetworkVariable<Vector3> horizontalMovement = new NetworkVariable<Vector3>();

    // Variables
    private Vector3 oldMovement;

    public float speed_x;
    public float speed_y;

    public bool isGrounded;
    public int jumpsAvailable;
    private int jumpCount;
    private bool doubleJumpCheck;
    
    private Rigidbody rb;

    private GameObject ledgeFist;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();

        ledgeFist = null;

        //Test
        isGrounded = true;
    }


    // Update is called once per frame
    void Update()
    {

        if (IsClient && IsOwner) 
        {
            UpdateClient();
        }

        if (IsServer)
        {
            UpdateHorizontalMovement();
        }


        ////Check if hanging from a ledge
        //if (ledgeFist != null)
        //{
        //    if (Mathf.Abs(x) > 0)
        //    {
        //        ledgeFist.GetComponent<FistController>().StopHanging();
        //        doubleJumpCheck = true;
        //        ledgeFist = null;
        //    }
        //}

        //// Movement on y-axis
        //
    }


    private void UpdateClient()
    {
        // Movement on X-Axis
        float x = Input.GetAxis("Horizontal");
        Vector3 movement = transform.right * x * speed_x;

        if (oldMovement != movement)
        {
            oldMovement = movement;
            UpdateClientPositionServerRpc(movement);
        }


        //Check to see if the client is on the ground
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
                doubleJumpCheck = true;
            }

            else if (doubleJumpCheck)
            {
                UpdateClientJumpServerRpc(transform.up * speed_y);
                doubleJumpCheck = false;
            }
        }
    }

    #region RPC Calls

    [ServerRpc]
    public void UpdateClientPositionServerRpc(Vector3 movement)
    {
        horizontalMovement.Value = movement;
    }

    [ServerRpc]
    public void UpdateClientJumpServerRpc(Vector3 force)
    {

        if (force != Vector3.zero)
        {
            rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            rb.AddForce(force, ForceMode.Impulse);
        }
    }

    #endregion


    #region Server Functions
    private void UpdateHorizontalMovement()
    {
        if (horizontalMovement.Value != Vector3.zero)
        {
            transform.Translate(horizontalMovement.Value * Time.deltaTime);
        }
    }

    #endregion


    //Get a reference to the fist that is hanging off a wall
    public void UpdateLedgeFist(GameObject fist)
    {
        ledgeFist = fist;
    }

}
