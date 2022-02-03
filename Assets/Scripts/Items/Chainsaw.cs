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
    private bool reposition;


    // Start is called before the first frame update
    void Start()
    {
        base.Start();
    }

    // Update is called once per frame
    //void Update()
    //{
    //    base.Update();
    //}

    private void FixedUpdate()
    {
       // if (IsServer)
       // {
            if (isGrounded)
            {
                if (!reposition)
                {
                    
                    //Check to see if the client is on the ground
                    if (!Physics.Raycast(transform.position, -transform.up, transform.GetComponent<Collider>().bounds.size.y / 2 + 0.1f, ~LayerMask.NameToLayer("Ground")))
                    {
                        transform.SetParent(null);
                        //transform.rotation = Quaternion.AngleAxis(-90, new Vector3(0, 0, 1));
                        transform.Rotate(0, 0, -90, relativeTo: Space.Self);
                        reposition = true;
                    }

                }
                else
                {
                    //Check to see if the client is on the ground
                    if (Physics.Raycast(transform.position, -transform.up, transform.GetComponent<Collider>().bounds.size.y / 2 + 0.1f, ~LayerMask.NameToLayer("Ground")))
                    {
                        reposition = false;
                    }
                }

                if (right)
                {
                    movement = Vector3.right * speed;
                }
                else if (left)
                {
                    movement = -Vector3.right * speed;
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

    private void OnTriggerEnter(Collider col)
    {
        //if (IsServer)
       // {
            if (col.gameObject.tag == "Platforms")
            {
                isGrounded = true;
                GetComponent<Rigidbody>().useGravity = false;
                transform.SetParent(col.transform);
            }
       // }
    }
}
