using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character_Hands : MonoBehaviour
{
    public GameObject rightFist;
    public GameObject leftFist;
    public float force;
    public float punchBrakes;
    private Vector3 direction;
    private Rigidbody rightRB;
    private Rigidbody leftRB;
    private bool rightOut;
    private bool leftOut;

    // Start is called before the first frame update
    void Start()
    {
        direction = new Vector3(1, 0, 0);
        rightRB = rightFist.GetComponent<Rigidbody>();
        leftRB = leftFist.GetComponent<Rigidbody>();
        rightOut = false;
        leftOut = false;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (!rightOut)
            {
                rightRB.AddForce(direction * force);
                rightOut = true;
            }
            else if (!leftOut)
            {
                leftRB.AddForce(direction * force);
                leftOut = true;
            }
        }

        if (rightOut)
        {
            if (rightRB.velocity.magnitude < punchBrakes)
            {
                rightFist.transform.position = Vector3.Lerp(rightFist.transform.position, transform.position, 20f);
            }

        }
        else if (leftOut)
        {
            if (leftRB.velocity.magnitude < punchBrakes)
            {
                leftFist.transform.position = Vector3.Lerp(leftFist.transform.position, transform.position, 20f);
            }
        }

    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "PlayerFist")
        {
            if (rightOut)
            {
                rightOut = false;
            }
            
            if (leftOut)
            {
                leftOut = false;
            }
        }
    }
}
