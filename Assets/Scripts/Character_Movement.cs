using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character_Movement : MonoBehaviour
{
    // Variables
    public float speed_x;
    public float speed_y;
    public int defaultJumpNumber = 1;
    public float ht;
    private bool isGrounded;
    private int jumpsAvailable;
    private bool doubleJumpCheck;
    private Rigidbody rb;

    private GameObject ledgeFist;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        jumpsAvailable = defaultJumpNumber;
        
        ledgeFist = null;
    }

    // Update is called once per frame
    void Update()
    {
        // Movement on x-axis
        float x = Input.GetAxis("Horizontal");

        //Check if hanging from a ledge
        if (ledgeFist != null)
        {
            if (Mathf.Abs(x) > 0)
            {
                ledgeFist.GetComponent<FistController>().StopHanging();
                doubleJumpCheck = true;
                ledgeFist = null;
            }
        }

        Vector3 movement = transform.right * x * speed_x;
        transform.Translate(movement * Time.deltaTime);

        // Movement on y-axis
        if(Input.GetKeyDown(KeyCode.Space))
        {

            //Player is hanging from a ledge
            if (ledgeFist != null)
            {
                ledgeFist.GetComponent<FistController>().StopHanging();

                Jump();

                doubleJumpCheck = true;
                ledgeFist = null;
            }

            else if (isGrounded) 
            {               
                Jump();

                doubleJumpCheck = true;
            }

            else if (doubleJumpCheck)
            {
                rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);

                doubleJumpCheck = false;
                
                Jump();
            }
        }

    }

    void Jump()
    {
        //movement += transform.up * speed_y;
        rb.AddForce(transform.up * speed_y, ForceMode.Impulse);
        isGrounded = false;
    }

    //Get a reference to the fist that is hanging off a wall
    public void UpdateLedgeFist(GameObject fist)
    {
        ledgeFist = fist;
    }


    // Jump collision with ground
    private void OnCollisionEnter(Collision collision)
    {

        if (LayerMask.LayerToName(collision.gameObject.layer) == "Ground")
        {
            if (Physics.Raycast(transform.position, Vector3.down, transform.GetComponent<Collider>().bounds.size.y / 2 + 0.1f, ~LayerMask.NameToLayer("Ground")))
            {
                isGrounded = true;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        Gizmos.DrawRay(transform.position, Vector3.down);
    }
}
