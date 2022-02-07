using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Platform_Move : MonoBehaviour
{
    public float speed;
    public bool vertDir;
    public bool horDir;
    public float stopTime;
    public float travDist;
    private Vector3 direction;
    private float time;
    private float dist;
    
    // Start is called before the first frame update
    void Start()
    {
        //if (IsServer)
        //{
            time = 0;
            dist = travDist;
        //}
    }

    // Update is called once per frame
    void Update()
    {
        //if (IsServer)
        //{

            // check the time to make sure 
            if (time > 0)
            {
                time -= Time.fixedDeltaTime;
            }
            else
            {
                // Resets the direction piece for the if statements
                direction = Vector3.zero;

                // Check horizontal direction
                if (horDir)
                {
                    direction += transform.right;
                }

                // Check vertical direction
                if (vertDir)
                {
                    direction += transform.up;
                }

                // Movement time
                transform.Translate(direction * speed, Space.Self);

                // Lowering that distance
                dist -= Mathf.Abs(speed);

                // Check the distance and enforce the time stoppage
                if (dist < 0)
                {
                    time = stopTime;
                    dist = travDist;

                    speed *= -1;
                }
            }
        //}
    }

    private void OnCollisionEnter(Collision collision)
    {
        //if (IsServer)
        //{
            if (collision.gameObject.tag == "Player")
            {
                collision.collider.transform.SetParent(transform);
            }
        //}
    }

    private void OnCollisionExit(Collision collision)
    {
        //if (IsServer)
        //{
            if (collision.gameObject.tag == "Player")
            {
                collision.collider.transform.SetParent(null);
            }
        //}
    }
}
