using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chainsaw : Item, IItemInterface
{
    public bool isGrounded;
    public bool left;
    public bool right;
    public float speed;
    public float timeInterval;
    private Vector3 movement;


    // Start is called before the first frame update
    void Start()
    {
        base.Start();
    }

    // Update is called once per frame
    void Update()
    {
        base.Update();
    }

    private void FixedUpdate()
    {
       // if (IsServer)
       // {
            if (isGrounded)
            {
                //Check to see if the client is on the ground
                if (!Physics.Raycast(transform.position, -transform.up, transform.GetComponent<Collider>().bounds.size.y / 2 + 0.1f, ~LayerMask.NameToLayer("Ground")))
                {
                    transform.SetParent(null);
                    transform.rotation = Quaternion.AngleAxis(-90, new Vector3(0, 0, 1));
                    
                }

                if (right)
                {
                    movement = transform.right * speed;
                }
                else if (left)
                {
                    movement = -transform.right * speed;
                }
                else
                {
                    Debug.LogError("oh no it broke");
                }
                transform.Translate(movement * Time.fixedDeltaTime);
            }
       // }
    }


    public void UseItem()
    {
        Debug.Log("ACTIVATED");
        Debug.Log(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        //if (IsServer)
       // {
            if (collision.gameObject.tag == "Platforms")
            {
                isGrounded = true;
                GetComponent<Rigidbody>().useGravity = false;
                transform.SetParent(collision.collider.transform);
            }
       // }
    }
}
