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

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        jumpsAvailable = defaultJumpNumber;
    }

    // Jump collision with ground
    private void OnCollisionEnter(Collision collision)
    {
        
        if(LayerMask.LayerToName(collision.gameObject.layer) == "Ground")
        {
            if (Physics.Raycast(transform.position, Vector3.down, transform.GetComponent<CapsuleCollider>().height/2 + 0.1f, ~LayerMask.NameToLayer("Ground")))
            {
                isGrounded = true;
                Debug.Log("We have collided.");
            }

            
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Movement on x-axis
        float x = Input.GetAxis("Horizontal");
        Vector3 movement = transform.right * x * speed_x;

        // Movement on y-axis
        if(Input.GetKeyDown(KeyCode.Space))
        {
            if (isGrounded) 
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
        
        
        
        transform.Translate(movement * Time.deltaTime);
    }

    void Jump()
    {
        //movement += transform.up * speed_y;
        rb.AddForce(transform.up * speed_y, ForceMode.Impulse);
        isGrounded = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        Gizmos.DrawRay(transform.position, Vector3.down);
    }
}
