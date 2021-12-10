using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character_Controls : MonoBehaviour
{
    // Variables
    public float speed_x = 2;
    public float speed_y = 5;
    public float ht;
    private bool isGrounded;
    private Rigidbody rb;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Jump collision with ground
    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.tag == "Platforms")
        {
            isGrounded = true;
            Debug.Log("Van Halen");
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Movement on x-axis
        float x = Input.GetAxis("Horizontal");
        Vector3 movement = transform.right * x * speed_x;

        // Movement on y-axis
        if(Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            //movement += transform.up * speed_y;
            rb.AddForce(transform.up * speed_y, ForceMode.Impulse);
            isGrounded = false;
        }
        
        
        
        transform.Translate(movement * Time.deltaTime);
    }
}
